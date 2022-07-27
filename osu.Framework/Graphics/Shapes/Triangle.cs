// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osuTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Platform;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// Represents a sprite that is drawn in a triangle shape, instead of a rectangle shape.
    /// </summary>
    public class Triangle : Sprite
    {
        public Triangle()
        {
            // Texture is late bound but would otherwise set an initial (1, 1) size for relative size.
            Size = Vector2.One;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            Texture ??= host.Renderer.WhiteTexture;
        }

        public override RectangleF BoundingBox => toTriangle(ToParentSpace(LayoutRectangle)).AABBFloat;

        private static Primitives.Triangle toTriangle(Quad q) => new Primitives.Triangle(
            (q.TopLeft + q.TopRight) / 2,
            q.BottomLeft,
            q.BottomRight);

        public override bool Contains(Vector2 screenSpacePos) => toTriangle(ScreenSpaceDrawQuad).Contains(screenSpacePos);

        protected override DrawNode CreateDrawNode() => new TriangleDrawNode(this);

        private class TriangleDrawNode : SpriteDrawNode
        {
            public TriangleDrawNode(Triangle source)
                : base(source)
            {
            }

            protected override void Blit(IRenderer renderer)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                renderer.DrawTriangle(Texture, toTriangle(ScreenSpaceDrawQuad), DrawColourInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), TextureCoords);
            }

            protected override void BlitOpaqueInterior(IRenderer renderer)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                var triangle = toTriangle(ConservativeScreenSpaceDrawQuad);

                if (renderer.IsMaskingActive)
                    renderer.DrawClipped(ref triangle, Texture, DrawColourInfo.Colour);
                else
                    renderer.DrawTriangle(Texture, triangle, DrawColourInfo.Colour);
            }
        }
    }
}
