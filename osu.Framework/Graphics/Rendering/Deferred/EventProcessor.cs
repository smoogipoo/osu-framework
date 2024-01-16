// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Graphics.Veldrid.Textures;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventProcessor
    {
        private readonly DeferredRenderer deferredRenderer;
        private readonly GraphicsPipeline pipeline;

        public EventProcessor(DeferredRenderer deferredRenderer, GraphicsPipeline pipeline)
        {
            this.deferredRenderer = deferredRenderer;
            this.pipeline = pipeline;
        }

        public void ProcessEvents(EventListReader reader)
        {
            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddPrimitiveToBatch:
                    {
                        AddPrimitiveToBatchEvent e = reader.Current<AddPrimitiveToBatchEvent>();
                        IDeferredVertexBatch batch = e.VertexBatch.Dereference<IDeferredVertexBatch>(deferredRenderer);
                        batch.WritePrimitive(e.Memory, pipeline.Commands);
                        break;
                    }

                    case RenderEventType.SetUniformBufferData:
                    {
                        SetUniformBufferDataEvent e = reader.Current<SetUniformBufferDataEvent>();
                        IDeferredUniformBuffer buffer = e.Buffer.Dereference<IDeferredUniformBuffer>(deferredRenderer);
                        buffer.Write(e.Memory, pipeline.Commands);
                        break;
                    }
                }
            }

            reader.Reset();

            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddPrimitiveToBatch:
                        processEvent(reader.Current<AddPrimitiveToBatchEvent>());
                        break;

                    case RenderEventType.SetFrameBuffer:
                        processEvent(reader.Current<SetFrameBufferEvent>());
                        break;

                    case RenderEventType.UnsetFrameBuffer:
                        processEvent(reader.Current<UnsetFrameBufferEvent>());
                        break;

                    case RenderEventType.SetShader:
                        processEvent(reader.Current<SetShaderEvent>());
                        break;

                    case RenderEventType.SetTexture:
                        processEvent(reader.Current<SetTextureEvent>());
                        break;

                    case RenderEventType.BindUniformBlock:
                        processEvent(reader.Current<BindUniformBlockEvent>());
                        break;

                    case RenderEventType.Clear:
                        processEvent(reader.Current<ClearEvent>());
                        break;

                    case RenderEventType.SetDepthInfo:
                        processEvent(reader.Current<SetDepthInfoEvent>());
                        break;

                    case RenderEventType.SetScissor:
                        processEvent(reader.Current<SetScissorEvent>());
                        break;

                    case RenderEventType.SetScissorState:
                        processEvent(reader.Current<SetScissorStateEvent>());
                        break;

                    case RenderEventType.SetStencilInfo:
                        processEvent(reader.Current<SetStencilInfoEvent>());
                        break;

                    case RenderEventType.SetViewport:
                        processEvent(reader.Current<SetViewportEvent>());
                        break;

                    case RenderEventType.SetBlend:
                        processEvent(reader.Current<SetBlendEvent>());
                        break;

                    case RenderEventType.SetBlendMask:
                        processEvent(reader.Current<SetBlendMaskEvent>());
                        break;

                    case RenderEventType.SetUniformBufferData:
                        processEvent(reader.Current<SetUniformBufferDataEvent>());
                        break;

                    case RenderEventType.Flush:
                        processEvent(reader.Current<FlushEvent>());
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void processEvent(AddPrimitiveToBatchEvent e)
        {
        }

        private void processEvent(SetFrameBufferEvent e) => pipeline.SetFrameBuffer(e.FrameBuffer.Dereference<DeferredFrameBuffer>(deferredRenderer).Resource);

        private void processEvent(UnsetFrameBufferEvent e) => pipeline.SetFrameBuffer(null);

        private void processEvent(SetShaderEvent e) => pipeline.SetShader(e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource);

        private void processEvent(SetTextureEvent e) => pipeline.AttachTexture(e.Unit, e.Texture.Dereference<VeldridTexture>(deferredRenderer));

        private void processEvent(BindUniformBlockEvent e)
        {
            e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource.BindUniformBlock(
                e.Name.Dereference<string>(deferredRenderer),
                e.Buffer.Dereference<IUniformBuffer>(deferredRenderer));
        }

        private void processEvent(ClearEvent e) => pipeline.Clear(e.Info);

        private void processEvent(SetDepthInfoEvent e) => pipeline.SetDepthInfo(e.Info);

        private void processEvent(SetScissorEvent e) => pipeline.SetScissor(e.Scissor);

        private void processEvent(SetScissorStateEvent e) => pipeline.SetScissorState(e.Enabled);

        private void processEvent(SetStencilInfoEvent e) => pipeline.SetStencilInfo(e.Info);

        private void processEvent(SetViewportEvent e) => pipeline.SetViewport(e.Viewport);

        private void processEvent(SetBlendEvent e) => pipeline.SetBlend(e.Parameters);

        private void processEvent(SetBlendMaskEvent e) => pipeline.SetBlendMask(e.Mask);

        private void processEvent(SetUniformBufferDataEvent e) => e.Buffer.Dereference<IDeferredUniformBuffer>(deferredRenderer).MoveNext();

        private void processEvent(FlushEvent e) => e.VertexBatch.Dereference<IDeferredVertexBatch>(deferredRenderer).Draw(pipeline, e.VertexCount);
    }
}
