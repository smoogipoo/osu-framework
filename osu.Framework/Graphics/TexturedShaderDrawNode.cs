// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics
{
    public abstract class TexturedShaderDrawNode : DrawNode
    {
        protected IShader Shader => RequiresRoundedShader ? RoundedTextureShader : TextureShader;

        protected IShader TextureShader { get; private set; }
        protected IShader RoundedTextureShader { get; private set; }

        protected new ITexturedShaderDrawable Source => (ITexturedShaderDrawable)base.Source;

        protected TexturedShaderDrawNode(ITexturedShaderDrawable source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            TextureShader = Source.TextureShader;
            RoundedTextureShader = Source.RoundedTextureShader;
        }

        public override void Draw(IRenderer renderer)
        {
            base.Draw(renderer);

            RequiresRoundedShader = renderer.IsMaskingActive;
        }

        protected override void DrawOpaqueInterior(IRenderer renderer)
        {
            base.DrawOpaqueInterior(renderer);

            RequiresRoundedShader = renderer.IsMaskingActive;
        }

        protected virtual bool RequiresRoundedShader { get; private set; }
    }
}
