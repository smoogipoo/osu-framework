// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Threading;
using osuTK.Graphics.ES30;
using Veldrid;
using Veldrid.SPIRV;
using static osu.Framework.Threading.ScheduledDelegate;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    internal class GLShader : IShader
    {
        private readonly GLRenderer renderer;
        private readonly string name;
        private readonly IUniformBuffer<GlobalUniformData> globalUniformBuffer;
        private readonly GLShaderPart[] parts;

        private readonly ScheduledDelegate shaderCompileDelegate;

        internal readonly Dictionary<string, IUniform> Uniforms = new Dictionary<string, IUniform>();

        IReadOnlyDictionary<string, IUniform> IShader.Uniforms => Uniforms;

        private readonly Dictionary<string, GLUniformBlock> uniformBlocks = new Dictionary<string, GLUniformBlock>();
        private readonly List<Uniform<int>> textureUniforms = new List<Uniform<int>>();

        /// <summary>
        /// Holds all <see cref="uniformBlocks"/> values for faster access than iterating on <see cref="Dictionary{TKey,TValue}.Values"/>.
        /// </summary>
        private readonly List<GLUniformBlock> uniformBlocksValues = new List<GLUniformBlock>();

        public bool IsLoaded { get; private set; }

        public bool IsBound { get; private set; }

        private int programID = -1;

        internal GLShader(GLRenderer renderer, string name, GLShaderPart[] parts, IUniformBuffer<GlobalUniformData> globalUniformBuffer)
        {
            this.renderer = renderer;
            this.name = name;
            this.globalUniformBuffer = globalUniformBuffer;
            this.parts = parts.Where(p => p != null).ToArray();

            renderer.ScheduleExpensiveOperation(shaderCompileDelegate = new ScheduledDelegate(compile));
        }

        private void compile()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (IsLoaded)
                throw new InvalidOperationException("Attempting to compile an already-compiled shader.");

            if (parts.Length == 0)
                return;

            programID = CreateProgram();

            if (!CompileInternal())
                throw new ProgramLinkingFailedException(name, GetProgramLog());

            IsLoaded = true;

            BindUniformBlock("g_GlobalUniforms", globalUniformBuffer);
        }

        internal void EnsureShaderCompiled()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not compile a disposed shader.");

            if (shaderCompileDelegate.State == RunState.Waiting)
                shaderCompileDelegate.RunTask();
        }

        public void Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed shader.");

            if (IsBound)
                return;

            EnsureShaderCompiled();

            renderer.BindShader(this);

            for (int i = 0; i < textureUniforms.Count; i++)
                textureUniforms[i].Update();

            foreach (var block in uniformBlocksValues)
                block?.Bind();

            IsBound = true;
        }

        public void Unbind()
        {
            if (!IsBound)
                return;

            renderer.UnbindShader(this);

            IsBound = false;
        }

        public Uniform<T> GetUniform<T>(string name)
            where T : unmanaged, IEquatable<T>
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not retrieve uniforms from a disposed shader.");

            EnsureShaderCompiled();

            return (Uniform<T>)Uniforms[name];
        }

        public void BindUniformBlock(string blockName, IUniformBuffer buffer) => uniformBlocks[blockName].Assign(buffer);

        private protected virtual bool CompileInternal()
        {
            GLShaderPart vertexPart = parts.Single(p => p.Type == ShaderType.VertexShader);
            GLShaderPart fragmentPart = parts.Single(p => p.Type == ShaderType.FragmentShader);

            VertexFragmentCompilationResult crossCompileResult;

            try
            {
                // Shaders are in "Vulkan GLSL" format. They need to be cross-compiled to GLSL.
                crossCompileResult = SpirvCompilation.CompileVertexFragment(
                    Encoding.UTF8.GetBytes(vertexPart.GetRawText()),
                    Encoding.UTF8.GetBytes(fragmentPart.GetRawText()),
                    renderer.IsEmbedded ? CrossCompileTarget.ESSL : CrossCompileTarget.GLSL);
            }
            catch (Exception e)
            {
                throw new ProgramLinkingFailedException(name, e.ToString());
            }

            vertexPart.Compile(crossCompileResult.VertexShader);
            fragmentPart.Compile(crossCompileResult.FragmentShader);

            foreach (GLShaderPart p in parts)
                GL.AttachShader(this, p);

            GL.LinkProgram(this);
            GL.GetProgram(this, GetProgramParameterName.LinkStatus, out int linkResult);

            foreach (var part in parts)
                GL.DetachShader(this, part);

            int blockBindingIndex = 0;
            int textureIndex = 0;

            for (int set = 0; set < crossCompileResult.Reflection.ResourceLayouts.Length; set++)
            {
                ResourceLayoutDescription layout = crossCompileResult.Reflection.ResourceLayouts[set];

                if (layout.Elements.Length == 0)
                    continue;

                if (layout.Elements.Any(e => e.Kind == ResourceKind.TextureReadOnly || e.Kind == ResourceKind.TextureReadWrite))
                    textureUniforms.Add(new Uniform<int>(renderer, this, layout.Elements[0].Name, GL.GetUniformLocation(this, layout.Elements[0].Name)) { Value = textureIndex++ });
                else if (layout.Elements[0].Kind == ResourceKind.UniformBuffer)
                {
                    var block = new GLUniformBlock(renderer, this, GL.GetUniformBlockIndex(this, layout.Elements[0].Name), blockBindingIndex++);
                    uniformBlocks[layout.Elements[0].Name] = block;
                    uniformBlocksValues.Add(block);
                }
            }

            return linkResult == 1;
        }

        private protected virtual string GetProgramLog() => GL.GetProgramInfoLog(this);

        private protected virtual int CreateProgram() => GL.CreateProgram();

        private protected virtual void DeleteProgram(int id) => GL.DeleteProgram(id);

        public override string ToString() => $@"{name} Shader (Compiled: {programID != -1})";

        public static implicit operator int(GLShader shader) => shader.programID;

        #region IDisposable Support

        protected internal bool IsDisposed { get; private set; }

        ~GLShader()
        {
            renderer.ScheduleDisposal(s => s.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;

                shaderCompileDelegate?.Cancel();

                if (programID != -1)
                    DeleteProgram(this);
            }
        }

        #endregion

        public class PartCompilationFailedException : Exception
        {
            public PartCompilationFailedException(string partName, string log)
                : base($"A {typeof(GLShaderPart)} failed to compile: {partName}:\n{log.Trim()}")
            {
            }
        }

        public class ProgramLinkingFailedException : Exception
        {
            public ProgramLinkingFailedException(string programName, string log)
                : base($"A {typeof(GLShader)} failed to link: {programName}:\n{log.Trim()}")
            {
            }
        }
    }
}
