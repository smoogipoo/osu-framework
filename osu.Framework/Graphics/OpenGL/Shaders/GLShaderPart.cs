﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Shaders
{
    internal class GLShaderPart : IShaderPart
    {
        public static readonly Regex SHADER_INPUT_PATTERN = new Regex(@"^\s*layout\s*\(\s*location\s*=\s*(-?\d+)\s*\)\s*in\s+((?:(?:lowp|mediump|highp)\s+)?\w+)\s+(\w+)\s*;", RegexOptions.Multiline);
        private static readonly Regex last_input_pattern = new Regex(@"^\s*layout\s*\(\s*location\s*=\s*-1\s*\)\s+in", RegexOptions.Multiline);
        private static readonly Regex uniform_pattern = new Regex(@"^(\s*layout\s*\(.*)set\s*=\s*(-?\d)(.*\)\s*(?:(?:readonly\s*)?buffer|uniform))", RegexOptions.Multiline);
        private static readonly Regex include_pattern = new Regex(@"^\s*#\s*include\s+[""<](.*)["">]");

        internal bool Compiled { get; private set; }

        public readonly string Name;
        public readonly ShaderType Type;

        private readonly GLRenderer renderer;
        private readonly List<string> shaderCodes = new List<string>();
        private readonly IShaderStore store;

        private int partID = -1;

        public GLShaderPart(GLRenderer renderer, string name, byte[]? data, ShaderType type, IShaderStore store)
        {
            this.renderer = renderer;
            this.store = store;

            Name = name;
            Type = type;

            // Load the shader files.
            shaderCodes.Add(loadFile(data, true));

            int lastInputIndex = 0;

            // Parse all shader inputs to find the last input index.
            foreach (string code in shaderCodes)
            {
                foreach (Match m in SHADER_INPUT_PATTERN.Matches(code))
                    lastInputIndex = Math.Max(lastInputIndex, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
            }

            // Update the location of the m_BackbufferDrawDepth input to be placed after all other inputs.
            for (int i = 0; i < shaderCodes.Count; i++)
                shaderCodes[i] = last_input_pattern.Replace(shaderCodes[i], match => $"layout(location = {++lastInputIndex}) in");

            // Find the minimum uniform/buffer binding set across all shader codes. This will be a negative number (see sh_GlobalUniforms.h).
            int minSet = 0;

            foreach (string code in shaderCodes)
            {
                minSet = Math.Min(minSet, uniform_pattern.Matches(code)
                                                         .Where(m => m.Success)
                                                         .Select(m => int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture))
                                                         .DefaultIfEmpty(0).Min());
            }

            // Increment the binding set of all uniform blocks such that the minimum index is 0.
            // The difference in implementation here (compared to above) is intentional, as uniform blocks must be consistent between the shader stages, so they can't be easily appended.
            for (int i = 0; i < shaderCodes.Count; i++)
            {
                shaderCodes[i] = uniform_pattern.Replace(shaderCodes[i],
                    match => $"{match.Groups[1].Value}set = {int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) + Math.Abs(minSet)}{match.Groups[3].Value}");
            }
        }

        private string loadFile(byte[]? bytes, bool mainFile)
        {
            if (bytes == null)
                return string.Empty;

            using (MemoryStream ms = new MemoryStream(bytes))
            using (StreamReader sr = new StreamReader(ms))
            {
                string code = string.Empty;

                while (sr.Peek() != -1)
                {
                    string? line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        code += line + '\n';
                        continue;
                    }

                    if (line.StartsWith("#version", StringComparison.Ordinal)) // the version directive has to appear before anything else in the shader
                    {
                        shaderCodes.Insert(0, line + '\n');
                        continue;
                    }

                    if (line.StartsWith("#extension", StringComparison.Ordinal))
                    {
                        shaderCodes.Add(line + '\n');
                        continue;
                    }

                    Match includeMatch = include_pattern.Match(line);

                    if (includeMatch.Success)
                    {
                        string includeName = includeMatch.Groups[1].Value.Trim();

                        //#if DEBUG
                        //                        byte[] rawData = null;
                        //                        if (File.Exists(includeName))
                        //                            rawData = File.ReadAllBytes(includeName);
                        //#endif
                        code += loadFile(store.GetRawData(includeName), false) + '\n';
                    }
                    else
                        code += line + '\n';
                }

                if (mainFile)
                {
                    string internalIncludes = loadFile(store.GetRawData("Internal/sh_Compatibility.h"), false) + "\n";

                    internalIncludes += loadFile(store.GetRawData("Internal/sh_GlobalUniforms.h"), false) + "\n";
                    internalIncludes += loadFile(store.GetRawData("Internal/sh_MaskingInfo.h"), false) + "\n";

                    if (renderer.UseStructuredBuffers)
                        internalIncludes += loadFile(store.GetRawData("Internal/sh_MaskingBuffer_SSBO.h"), false) + "\n";
                    else
                        internalIncludes += loadFile(store.GetRawData("Internal/sh_MaskingBuffer_UBO.h"), false) + "\n";

                    if (Type == ShaderType.VertexShader)
                        internalIncludes += loadFile(store.GetRawData("Internal/sh_Vertex_Input.h"), false) + "\n";

                    code = internalIncludes + code;

                    if (Type == ShaderType.VertexShader)
                    {
                        string backbufferCode = loadFile(store.GetRawData("Internal/sh_Vertex_Output.h"), false);

                        if (!string.IsNullOrEmpty(backbufferCode))
                        {
                            const string real_main_name = "__internal_real_main";

                            backbufferCode = backbufferCode.Replace("{{ real_main }}", real_main_name);
                            code = Regex.Replace(code, @"void main\((.*)\)", $"void {real_main_name}()") + backbufferCode + '\n';
                        }
                    }
                }

                return code;
            }
        }

        public string GetRawText() => string.Join('\n', shaderCodes);

        public void Compile(string crossCompileOutput)
        {
            if (Compiled)
                return;

            partID = GL.CreateShader(Type);

            GL.ShaderSource(this, crossCompileOutput);
            GL.CompileShader(this);
            GL.GetShader(this, ShaderParameter.CompileStatus, out int compileResult);

            Compiled = compileResult == 1;

            if (!Compiled)
                throw new GLShader.PartCompilationFailedException(Name, GL.GetShaderInfoLog(this));
        }

        public static implicit operator int(GLShaderPart program) => program.partID;

        #region IDisposable Support

        protected internal bool IsDisposed { get; private set; }

        ~GLShaderPart()
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

                if (partID != -1)
                    GL.DeleteShader(this);
            }
        }

        #endregion
    }
}
