// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Draw node containing all necessary information to draw a <see cref="Sprite"/>.
    /// </summary>
    public class SpriteDrawNode : TexturedShaderDrawNode
    {
        protected Texture Texture { get; private set; }
        protected Quad ScreenSpaceDrawQuad { get; private set; }

        protected RectangleF DrawRectangle { get; private set; }
        protected Vector2 InflationAmount { get; private set; }

        protected RectangleF TextureCoords { get; private set; }

        protected new Sprite Source => (Sprite)base.Source;

        protected Quad ConservativeScreenSpaceDrawQuad;
        private bool hasOpaqueInterior;

        public SpriteDrawNode(Sprite source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            Texture = Source.Texture;
            ScreenSpaceDrawQuad = Source.ScreenSpaceDrawQuad;
            DrawRectangle = Source.DrawRectangle;
            InflationAmount = Source.InflationAmount;

            TextureCoords = Source.DrawRectangle.RelativeIn(Source.DrawTextureRectangle);
            if (Texture != null)
                TextureCoords *= new Vector2(Texture.DisplayWidth, Texture.DisplayHeight);

            hasOpaqueInterior = DrawColourInfo.Colour.MinAlpha == 1
                                && DrawColourInfo.Blending == BlendingParameters.Mixture
                                && DrawColourInfo.Colour.HasSingleColour;

            if (CanDrawOpaqueInterior)
                ConservativeScreenSpaceDrawQuad = Source.ConservativeScreenSpaceDrawQuad;
        }

        protected virtual void Blit(QuadBatch<TexturedVertex2D> batch)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            Texture.TextureGL.Bind();

            batch.DrawGroup(ref BatchUsage, this, static (ref VertexBatchUsage<TexturedVertex2D> group, SpriteDrawNode node) =>
            {
                node.DrawQuad(node.Texture, node.ScreenSpaceDrawQuad, node.DrawColourInfo.Colour, ref group, null,
                    new Vector2(node.InflationAmount.X / node.DrawRectangle.Width, node.InflationAmount.Y / node.DrawRectangle.Height),
                    null, node.TextureCoords);
            });
        }

        protected virtual void BlitOpaqueInterior(QuadBatch<TexturedVertex2D> batch)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            Texture.TextureGL.Bind();

            batch.DrawGroup(ref OpaqueInteriorBatchUsage, this, static (ref VertexBatchUsage<TexturedVertex2D> group, SpriteDrawNode node) =>
            {
                if (GLWrapper.IsMaskingActive)
                    node.DrawClipped(ref node.ConservativeScreenSpaceDrawQuad, node.Texture, node.DrawColourInfo.Colour, ref group);
                else
                    node.DrawQuad(node.Texture, node.ConservativeScreenSpaceDrawQuad, node.DrawColourInfo.Colour, ref group, textureCoords: node.TextureCoords);
            });
        }

        public override void Draw(in DrawState drawState)
        {
            base.Draw(drawState);

            if (Texture?.Available != true)
                return;

            Shader.Bind();

            Blit(drawState.QuadBatch);

            Shader.Unbind();
        }

        protected override bool RequiresRoundedShader => base.RequiresRoundedShader || InflationAmount != Vector2.Zero;

        protected override void DrawOpaqueInterior(in DrawState drawState)
        {
            base.DrawOpaqueInterior(drawState);

            if (Texture?.Available != true)
                return;

            TextureShader.Bind();

            BlitOpaqueInterior(drawState.QuadBatch);

            TextureShader.Unbind();
        }

        protected internal override bool CanDrawOpaqueInterior => Texture?.Available == true && Texture.Opacity == Opacity.Opaque && hasOpaqueInterior;
    }
}
