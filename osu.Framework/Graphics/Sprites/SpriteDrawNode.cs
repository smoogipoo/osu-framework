// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Extensions.MatrixExtensions;
using osuTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Utils;

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

        protected RectangleF ConservativeScreenSpaceAABB;

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
                ConservativeScreenSpaceAABB = Source.ConservativeScreenSpaceAABB;
        }

        protected virtual void Blit(IRenderer renderer)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            renderer.DrawQuad(Texture, ScreenSpaceDrawQuad, DrawColourInfo.Colour, null, null,
                new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height),
                null, TextureCoords);
        }

        protected virtual void BlitOpaqueInterior(IRenderer renderer)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            Quad drawQuad = Quad.FromRectangle(DrawRectangle) * DrawInfo.Matrix;

            if (Math.Abs(DrawInfo.Rotation) != 0)
            {
                Matrix3 mat = DrawInfo.Matrix;
                MatrixExtensions.RotateFromLeft(ref mat, -DrawInfo.Rotation);

                Vector2 inscribedSize = MathUtils.LargestInscribedRectangle(DrawRectangle.Size, DrawInfo.Rotation);
                RectangleF inscribed = (Quad.FromRectangle(new RectangleF(DrawRectangle.Location, inscribedSize)) * mat).AABBFloat;

                drawQuad = inscribed.Offset(drawQuad.Centre - inscribed.Centre);
            }

            RectangleF drawRect = drawQuad.AABBFloat;

            if (renderer.CurrentConservativeScreenSpaceRectangle is RectangleF rect)
            {
                drawRect = RectangleF.FromLTRB(
                    Math.Max(drawRect.Left, rect.Left),
                    Math.Max(drawRect.Top, rect.Top),
                    Math.Min(drawRect.Right, rect.Right),
                    Math.Min(drawRect.Bottom, rect.Bottom));
            }

            renderer.DrawQuad(Texture, drawRect, DrawColourInfo.Colour, textureCoords: TextureCoords);
        }

        public override void Draw(IRenderer renderer)
        {
            base.Draw(renderer);

            if (Texture?.Available != true)
                return;

            BindTextureShader(renderer);

            Blit(renderer);

            UnbindTextureShader(renderer);
        }

        protected override void DrawOpaqueInterior(IRenderer renderer)
        {
            base.DrawOpaqueInterior(renderer);

            if (Texture?.Available != true)
                return;

            BindTextureShader(renderer);

            BlitOpaqueInterior(renderer);

            UnbindTextureShader(renderer);
        }

        protected internal override bool CanDrawOpaqueInterior => Texture?.Available == true && Texture.Opacity == Opacity.Opaque && hasOpaqueInterior;
    }
}
