﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Markdig.Syntax.Inlines;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface.Markdown.Renderers.Inlines
{
    public class LinkRenderer : ObjectRenderer<LinkInline>
    {
        protected MarkdownRenderer Renderer { get; private set; }

        protected override void Write(MarkdownRenderer renderer, LinkInline obj)
        {
            Renderer = renderer;

            if (obj.IsImage)
                renderer.Write(new DelayedLoadWrapper(new AsyncSprite(obj.GetDynamicUrl?.Invoke() ?? obj.Url)));
            else
            {
                var subRenderer = new MarkdownRenderer();
                var document = (FillFlowContainer)subRenderer.Render(null);
                subRenderer.WriteChildren(obj);

                document.AutoSizeAxes = Axes.Both;

                renderer.Write(CreateLink(obj.GetDynamicUrl?.Invoke() ?? obj.Url, document));
            }
        }

        protected virtual Drawable CreateLink(string url, Drawable text) => new DrawableLink(text);

        private class DrawableLink : CompositeDrawable
        {
            public DrawableLink(Drawable text)
            {
                AutoSizeAxes = Axes.Both;
                Colour = Color4.LightBlue;

                InternalChild = text;
            }

            protected override bool OnHover(InputState state)
            {
                Colour = Color4.Pink;
                return true;
            }

            protected override void OnHoverLost(InputState state)
            {
                Colour = Color4.LightBlue;
            }
        }

        private class AsyncSprite : Sprite
        {
            private readonly string url;

            public AsyncSprite(string url)
            {
                this.url = url;
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Texture = textures.Get(url);
            }
        }
    }
}
