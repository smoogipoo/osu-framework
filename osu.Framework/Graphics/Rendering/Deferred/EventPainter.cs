// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal struct EventPainter
    {
        private readonly DeferredRenderer deferredRenderer;
        private readonly IRenderer baseRenderer;

        private IDeferredVertexBatch? currentDrawBatch;
        private int drawCount;

        public EventPainter(DeferredRenderer deferredRenderer, IRenderer baseRenderer)
        {
            this.deferredRenderer = deferredRenderer;
            this.baseRenderer = baseRenderer;
            currentDrawBatch = null;
            drawCount = 0;
        }

        public void ProcessEvents(EventListReader reader)
        {
            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddVertexToBatch:
                        AddVertexToBatchEvent e = reader.Current<AddVertexToBatchEvent>();
                        e.VertexBatch.Resolve<IDeferredVertexBatch>(deferredRenderer).Write(e.Memory, ((VeldridRenderer)baseRenderer).Commands);
                        break;
                }
            }

            reader.Reset();

            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddVertexToBatch:
                        ProcessEvent(reader.Current<AddVertexToBatchEvent>());
                        break;

                    case RenderEventType.BindFrameBuffer:
                        ProcessEvent(reader.Current<BindFrameBufferEvent>());
                        break;

                    case RenderEventType.BindShader:
                        ProcessEvent(reader.Current<BindShaderEvent>());
                        break;

                    case RenderEventType.BindTexture:
                        ProcessEvent(reader.Current<BindTextureEvent>());
                        break;

                    case RenderEventType.BindUniformBlock:
                        ProcessEvent(reader.Current<BindUniformBlockEvent>());
                        break;

                    case RenderEventType.Clear:
                        ProcessEvent(reader.Current<ClearEvent>());
                        break;

                    case RenderEventType.DrawVertexBatch:
                        ProcessEvent(reader.Current<DrawVertexBatchEvent>());
                        break;

                    case RenderEventType.PopDepthInfo:
                        ProcessEvent(reader.Current<PopDepthInfoEvent>());
                        break;

                    case RenderEventType.PopMaskingInfo:
                        ProcessEvent(reader.Current<PopMaskingInfoEvent>());
                        break;

                    case RenderEventType.PopProjectionMatrix:
                        ProcessEvent(reader.Current<PopProjectionMatrixEvent>());
                        break;

                    case RenderEventType.PopQuadBatch:
                        ProcessEvent(reader.Current<PopQuadBatchEvent>());
                        break;

                    case RenderEventType.PopScissor:
                        ProcessEvent(reader.Current<PopScissorEvent>());
                        break;

                    case RenderEventType.PopScissorOffset:
                        ProcessEvent(reader.Current<PopScissorOffsetEvent>());
                        break;

                    case RenderEventType.PopScissorState:
                        ProcessEvent(reader.Current<PopScissorStateEvent>());
                        break;

                    case RenderEventType.PopStencilInfo:
                        ProcessEvent(reader.Current<PopStencilInfoEvent>());
                        break;

                    case RenderEventType.PopViewport:
                        ProcessEvent(reader.Current<PopViewportEvent>());
                        break;

                    case RenderEventType.PushDepthInfo:
                        ProcessEvent(reader.Current<PushDepthInfoEvent>());
                        break;

                    case RenderEventType.PushMaskingInfo:
                        ProcessEvent(reader.Current<PushMaskingInfoEvent>());
                        break;

                    case RenderEventType.PushProjectionMatrix:
                        ProcessEvent(reader.Current<PushProjectionMatrixEvent>());
                        break;

                    case RenderEventType.PushQuadBatch:
                        ProcessEvent(reader.Current<PushQuadBatchEvent>());
                        break;

                    case RenderEventType.PushScissor:
                        ProcessEvent(reader.Current<PushScissorEvent>());
                        break;

                    case RenderEventType.PushScissorOffset:
                        ProcessEvent(reader.Current<PushScissorOffsetEvent>());
                        break;

                    case RenderEventType.PushScissorState:
                        ProcessEvent(reader.Current<PushScissorStateEvent>());
                        break;

                    case RenderEventType.PushStencilInfo:
                        ProcessEvent(reader.Current<PushStencilInfoEvent>());
                        break;

                    case RenderEventType.PushViewport:
                        ProcessEvent(reader.Current<PushViewportEvent>());
                        break;

                    case RenderEventType.SetBlend:
                        ProcessEvent(reader.Current<SetBlendEvent>());
                        break;

                    case RenderEventType.SetBlendMask:
                        ProcessEvent(reader.Current<SetBlendMaskEvent>());
                        break;

                    case RenderEventType.SetUniformBufferData:
                        ProcessEvent(reader.Current<SetUniformBufferDataEvent>());
                        break;

                    case RenderEventType.UnbindFrameBuffer:
                        ProcessEvent(reader.Current<UnbindFrameBufferEvent>());
                        break;

                    case RenderEventType.UnbindShader:
                        ProcessEvent(reader.Current<UnbindShaderEvent>());
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            FlushCurrentBatch(FlushBatchSource.FinishFrame);
        }

        public void ProcessEvent(AddVertexToBatchEvent e)
        {
            IDeferredVertexBatch batch = e.VertexBatch.Resolve<IDeferredVertexBatch>(deferredRenderer);

            if (currentDrawBatch != null && batch != currentDrawBatch)
                FlushCurrentBatch(FlushBatchSource.BindBuffer);

            currentDrawBatch = batch;
            drawCount++;
        }

        public void ProcessEvent(BindFrameBufferEvent e)
        {
            e.FrameBuffer.Resolve<DeferredFrameBuffer>(deferredRenderer).Resource.Bind();
        }

        public void ProcessEvent(BindShaderEvent e)
        {
            e.Shader.Resolve<DeferredShader>(deferredRenderer).Resource.Bind();
        }

        public void ProcessEvent(BindTextureEvent e)
        {
            baseRenderer.BindTexture(e.Texture.Resolve<Texture>(deferredRenderer), e.Unit, e.WrapModeS, e.WrapModeT);
        }

        public void ProcessEvent(BindUniformBlockEvent e)
        {
            e.Shader.Resolve<DeferredShader>(deferredRenderer).Resource.BindUniformBlock(
                e.Name.Resolve<string>(deferredRenderer),
                e.Buffer.Resolve<IDeferredUniformBuffer>(deferredRenderer).GetBuffer());
        }

        public void ProcessEvent(ClearEvent e)
        {
            baseRenderer.Clear(e.Info);
        }

        public void ProcessEvent(DrawVertexBatchEvent e)
        {
            baseRenderer.FlushCurrentBatch(null);
        }

        public void ProcessEvent(PopDepthInfoEvent e)
        {
            baseRenderer.PopDepthInfo();
        }

        public void ProcessEvent(PopMaskingInfoEvent e)
        {
            baseRenderer.PopMaskingInfo();
        }

        public void ProcessEvent(PopProjectionMatrixEvent e)
        {
            baseRenderer.PopProjectionMatrix();
        }

        public void ProcessEvent(PopQuadBatchEvent e)
        {
            baseRenderer.PopQuadBatch();
        }

        public void ProcessEvent(PopScissorEvent e)
        {
            baseRenderer.PopScissor();
        }

        public void ProcessEvent(PopScissorOffsetEvent e)
        {
            baseRenderer.PopScissorOffset();
        }

        public void ProcessEvent(PopScissorStateEvent e)
        {
            baseRenderer.PopScissorState();
        }

        public void ProcessEvent(PopStencilInfoEvent e)
        {
            baseRenderer.PopStencilInfo();
        }

        public void ProcessEvent(PopViewportEvent e)
        {
            baseRenderer.PopViewport();
        }

        public void ProcessEvent(PushDepthInfoEvent e)
        {
            baseRenderer.PushDepthInfo(e.Info);
        }

        public void ProcessEvent(PushMaskingInfoEvent e)
        {
            baseRenderer.PushMaskingInfo(e.Info);
        }

        public void ProcessEvent(PushProjectionMatrixEvent e)
        {
            baseRenderer.PushProjectionMatrix(e.Matrix);
        }

        public void ProcessEvent(PushQuadBatchEvent e)
        {
            baseRenderer.PushQuadBatch(e.VertexBatch.Resolve<IVertexBatch<TexturedVertex2D>>(deferredRenderer));
        }

        public void ProcessEvent(PushScissorEvent e)
        {
            baseRenderer.PushScissor(e.Scissor);
        }

        public void ProcessEvent(PushScissorOffsetEvent e)
        {
            baseRenderer.PushScissorOffset(e.Offset);
        }

        public void ProcessEvent(PushScissorStateEvent e)
        {
            baseRenderer.PushScissorState(e.Enabled);
        }

        public void ProcessEvent(PushStencilInfoEvent e)
        {
            baseRenderer.PushStencilInfo(e.Info);
        }

        public void ProcessEvent(PushViewportEvent e)
        {
            baseRenderer.PushViewport(e.Viewport);
        }

        public void ProcessEvent(SetBlendEvent e)
        {
            baseRenderer.SetBlend(e.Parameters);
        }

        public void ProcessEvent(SetBlendMaskEvent e)
        {
            baseRenderer.SetBlendMask(e.Mask);
        }

        public void ProcessEvent(SetUniformBufferDataEvent e)
        {
            e.Buffer.Resolve<IDeferredUniformBuffer>(deferredRenderer).SetDataFromBuffer(e.Data.GetBuffer(deferredRenderer));
        }

        public void ProcessEvent(UnbindFrameBufferEvent e)
        {
            e.FrameBuffer.Resolve<DeferredFrameBuffer>(deferredRenderer).Resource.Unbind();
        }

        public void ProcessEvent(UnbindShaderEvent e)
        {
            e.Shader.Resolve<DeferredShader>(deferredRenderer).Resource.Unbind();
        }

        public void FlushCurrentBatch(FlushBatchSource? source)
        {
            if (currentDrawBatch == null)
                return;

            // Prevent re-entrancy
            IDeferredVertexBatch batch = currentDrawBatch;
            currentDrawBatch = null;

            batch.Draw((VeldridRenderer)baseRenderer, drawCount);

            drawCount = 0;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
        }
    }
}
