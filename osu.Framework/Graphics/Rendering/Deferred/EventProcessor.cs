// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Graphics.Veldrid.Textures;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventProcessor
    {
        private readonly DeferredRenderer deferredRenderer;
        private readonly GraphicsPipeline pipeline;
        private readonly VertexManager vertexManager;
        private readonly UniformBufferManager uniformBufferManager;

        public EventProcessor(DeferredRenderer deferredRenderer, GraphicsPipeline pipeline, VertexManager vertexManager, UniformBufferManager uniformBufferManager)
        {
            this.deferredRenderer = deferredRenderer;
            this.pipeline = pipeline;
            this.vertexManager = vertexManager;
            this.uniformBufferManager = uniformBufferManager;
        }

        public void ProcessEvents(EventListReader reader)
        {
            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddPrimitiveToBatch:
                    {
                        ref AddPrimitiveToBatchEvent e = ref reader.Current<AddPrimitiveToBatchEvent>();
                        IDeferredVertexBatch batch = e.VertexBatch.Dereference<IDeferredVertexBatch>(deferredRenderer);
                        batch.Write(e.Memory);
                        break;
                    }

                    case RenderEventType.SetUniformBufferData:
                    {
                        ref SetUniformBufferDataEvent e = ref reader.Current<SetUniformBufferDataEvent>();
                        IDeferredUniformBuffer buffer = e.Buffer.Dereference<IDeferredUniformBuffer>(deferredRenderer);
                        buffer.Write(e.Memory);
                        break;
                    }

                    case RenderEventType.SetShaderStorageBufferObjectData:
                    {
                        ref SetShaderStorageBufferObjectDataEvent e = ref reader.Current<SetShaderStorageBufferObjectDataEvent>();
                        IDeferredShaderStorageBufferObject buffer = e.Buffer.Dereference<IDeferredShaderStorageBufferObject>(deferredRenderer);
                        buffer.Write(e.Index, e.Memory);
                        break;
                    }
                }
            }

            vertexManager.Commit();
            uniformBufferManager.Commit();

            reader.Reset();

            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddPrimitiveToBatch:
                        processEvent(ref reader.Current<AddPrimitiveToBatchEvent>());
                        break;

                    case RenderEventType.SetFrameBuffer:
                        processEvent(ref reader.Current<SetFrameBufferEvent>());
                        break;

                    case RenderEventType.UnsetFrameBuffer:
                        processEvent(ref reader.Current<UnsetFrameBufferEvent>());
                        break;

                    case RenderEventType.SetShader:
                        processEvent(ref reader.Current<SetShaderEvent>());
                        break;

                    case RenderEventType.SetTexture:
                        processEvent(ref reader.Current<SetTextureEvent>());
                        break;

                    case RenderEventType.BindUniformBlock:
                        processEvent(ref reader.Current<BindUniformBlockEvent>());
                        break;

                    case RenderEventType.Clear:
                        processEvent(ref reader.Current<ClearEvent>());
                        break;

                    case RenderEventType.SetDepthInfo:
                        processEvent(ref reader.Current<SetDepthInfoEvent>());
                        break;

                    case RenderEventType.SetScissor:
                        processEvent(ref reader.Current<SetScissorEvent>());
                        break;

                    case RenderEventType.SetScissorState:
                        processEvent(ref reader.Current<SetScissorStateEvent>());
                        break;

                    case RenderEventType.SetStencilInfo:
                        processEvent(ref reader.Current<SetStencilInfoEvent>());
                        break;

                    case RenderEventType.SetViewport:
                        processEvent(ref reader.Current<SetViewportEvent>());
                        break;

                    case RenderEventType.SetBlend:
                        processEvent(ref reader.Current<SetBlendEvent>());
                        break;

                    case RenderEventType.SetBlendMask:
                        processEvent(ref reader.Current<SetBlendMaskEvent>());
                        break;

                    case RenderEventType.SetUniformBufferData:
                        processEvent(ref reader.Current<SetUniformBufferDataEvent>());
                        break;

                    case RenderEventType.SetShaderStorageBufferObjectData:
                        break;

                    case RenderEventType.Flush:
                        processEvent(ref reader.Current<FlushEvent>());
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void processEvent(ref AddPrimitiveToBatchEvent e)
        {
        }

        private void processEvent(ref SetFrameBufferEvent e) => pipeline.SetFrameBuffer(e.FrameBuffer.Dereference<DeferredFrameBuffer>(deferredRenderer).Resource);

        private void processEvent(ref UnsetFrameBufferEvent e) => pipeline.SetFrameBuffer(null);

        private void processEvent(ref SetShaderEvent e) => pipeline.SetShader(e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource);

        private void processEvent(ref SetTextureEvent e) => pipeline.AttachTexture(e.Unit, e.Texture.Dereference<VeldridTexture>(deferredRenderer));

        private void processEvent(ref BindUniformBlockEvent e)
        {
            e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource.BindUniformBlock(
                e.Name.Dereference<string>(deferredRenderer),
                e.Buffer.Dereference<IUniformBuffer>(deferredRenderer));
        }

        private void processEvent(ref ClearEvent e) => pipeline.Clear(e.Info);

        private void processEvent(ref SetDepthInfoEvent e) => pipeline.SetDepthInfo(e.Info);

        private void processEvent(ref SetScissorEvent e) => pipeline.SetScissor(e.Scissor);

        private void processEvent(ref SetScissorStateEvent e) => pipeline.SetScissorState(e.Enabled);

        private void processEvent(ref SetStencilInfoEvent e) => pipeline.SetStencilInfo(e.Info);

        private void processEvent(ref SetViewportEvent e) => pipeline.SetViewport(e.Viewport);

        private void processEvent(ref SetBlendEvent e) => pipeline.SetBlend(e.Parameters);

        private void processEvent(ref SetBlendMaskEvent e) => pipeline.SetBlendMask(e.Mask);

        private void processEvent(ref SetUniformBufferDataEvent e) => e.Buffer.Dereference<IDeferredUniformBuffer>(deferredRenderer).MoveNext();

        private void processEvent(ref FlushEvent e) => e.VertexBatch.Dereference<IDeferredVertexBatch>(deferredRenderer).Draw(pipeline, e.VertexCount);
    }
}
