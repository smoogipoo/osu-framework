// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Textures;
using osu.Framework.Statistics;
using osu.Framework.Threading;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public struct DeferredPainter(DeferredRenderer deferredRenderer, IRenderer baseRenderer)
    {
        private IDeferredVertexBatch? currentDrawBatch;
        private int? drawStartIndex;
        private int drawEndIndex;

        public void ProcessEvents(EventListReader reader)
        {
            while (reader.ReadType(out RenderEventType type))
            {
                switch (type)
                {
                    case RenderEventType.AddVertexToBatch:
                        ProcessEvent(reader.Read<AddVertexToBatchEvent>());
                        break;

                    case RenderEventType.BindFrameBuffer:
                        ProcessEvent(reader.Read<BindFrameBufferEvent>());
                        break;

                    case RenderEventType.BindShader:
                        ProcessEvent(reader.Read<BindShaderEvent>());
                        break;

                    case RenderEventType.BindTexture:
                        ProcessEvent(reader.Read<BindTextureEvent>());
                        break;

                    case RenderEventType.BindUniformBlock:
                        ProcessEvent(reader.Read<BindUniformBlockEvent>());
                        break;

                    case RenderEventType.Clear:
                        ProcessEvent(reader.Read<ClearEvent>());
                        break;

                    case RenderEventType.Disposal:
                        ProcessEvent(reader.Read<DisposalEvent>());
                        break;

                    case RenderEventType.DrawVertexBatch:
                        ProcessEvent(reader.Read<DrawVertexBatchEvent>());
                        break;

                    case RenderEventType.ExpensiveOperation:
                        ProcessEvent(reader.Read<ExpensiveOperationEvent>());
                        break;

                    case RenderEventType.PopDepthInfo:
                        ProcessEvent(reader.Read<PopDepthInfoEvent>());
                        break;

                    case RenderEventType.PopMaskingInfo:
                        ProcessEvent(reader.Read<PopMaskingInfoEvent>());
                        break;

                    case RenderEventType.PopProjectionMatrix:
                        ProcessEvent(reader.Read<PopProjectionMatrixEvent>());
                        break;

                    case RenderEventType.PopQuadBatch:
                        ProcessEvent(reader.Read<PopQuadBatchEvent>());
                        break;

                    case RenderEventType.PopScissor:
                        ProcessEvent(reader.Read<PopScissorEvent>());
                        break;

                    case RenderEventType.PopScissorOffset:
                        ProcessEvent(reader.Read<PopScissorOffsetEvent>());
                        break;

                    case RenderEventType.PopScissorState:
                        ProcessEvent(reader.Read<PopScissorStateEvent>());
                        break;

                    case RenderEventType.PopStencilInfo:
                        ProcessEvent(reader.Read<PopStencilInfoEvent>());
                        break;

                    case RenderEventType.PopViewport:
                        ProcessEvent(reader.Read<PopViewportEvent>());
                        break;

                    case RenderEventType.PushDepthInfo:
                        ProcessEvent(reader.Read<PushDepthInfoEvent>());
                        break;

                    case RenderEventType.PushMaskingInfo:
                        ProcessEvent(reader.Read<PushMaskingInfoEvent>());
                        break;

                    case RenderEventType.PushProjectionMatrix:
                        ProcessEvent(reader.Read<PushProjectionMatrixEvent>());
                        break;

                    case RenderEventType.PushQuadBatch:
                        ProcessEvent(reader.Read<PushQuadBatchEvent>());
                        break;

                    case RenderEventType.PushScissor:
                        ProcessEvent(reader.Read<PushScissorEvent>());
                        break;

                    case RenderEventType.PushScissorOffset:
                        ProcessEvent(reader.Read<PushScissorOffsetEvent>());
                        break;

                    case RenderEventType.PushScissorState:
                        ProcessEvent(reader.Read<PushScissorStateEvent>());
                        break;

                    case RenderEventType.PushStencilInfo:
                        ProcessEvent(reader.Read<PushStencilInfoEvent>());
                        break;

                    case RenderEventType.PushViewport:
                        ProcessEvent(reader.Read<PushViewportEvent>());
                        break;

                    case RenderEventType.SetBlend:
                        ProcessEvent(reader.Read<SetBlendEvent>());
                        break;

                    case RenderEventType.SetBlendMask:
                        ProcessEvent(reader.Read<SetBlendMaskEvent>());
                        break;

                    case RenderEventType.SetUniformBufferData:
                        ProcessEvent(reader.Read<SetUniformBufferDataEvent>());
                        break;

                    case RenderEventType.UnbindFrameBuffer:
                        ProcessEvent(reader.Read<UnbindFrameBufferEvent>());
                        break;

                    case RenderEventType.UnbindShader:
                        ProcessEvent(reader.Read<UnbindShaderEvent>());
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void ProcessEvent(AddVertexToBatchEvent e)
        {
            IDeferredVertexBatch batch = e.VertexBatch.Resolve<IDeferredVertexBatch>(deferredRenderer);

            if (currentDrawBatch != null && batch != currentDrawBatch)
                FlushCurrentBatch(FlushBatchSource.BindBuffer);

            drawStartIndex ??= e.Index;
            drawEndIndex = e.Index;
            currentDrawBatch = batch;
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
            e.Shader.Resolve<DeferredShader>(deferredRenderer).Resource.BindUniformBlock(e.Name.Resolve<string>(deferredRenderer), e.Buffer.Resolve<IUniformBuffer>(deferredRenderer));
        }

        public void ProcessEvent(ClearEvent e)
        {
            baseRenderer.Clear(e.Info);
        }

        public void ProcessEvent(DisposalEvent e)
        {
            baseRenderer.ScheduleDisposal(e.DisposalAction.Resolve<Action<object>>(deferredRenderer), e.Target.Resolve<object>(deferredRenderer));
        }

        public void ProcessEvent(DrawVertexBatchEvent e)
        {
            baseRenderer.FlushCurrentBatch(null);
        }

        public void ProcessEvent(ExpensiveOperationEvent e)
        {
            baseRenderer.ScheduleExpensiveOperation(e.Operation.Resolve<ScheduledDelegate>(deferredRenderer));
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

        public void Finish()
        {
            FlushCurrentBatch(FlushBatchSource.FinishFrame);
        }

        public void FlushCurrentBatch(FlushBatchSource? source)
        {
            if (currentDrawBatch == null)
                return;

            // Prevent re-entrancy
            IDeferredVertexBatch batch = currentDrawBatch;
            currentDrawBatch = null;

            Debug.Assert(drawStartIndex != null);
            batch.Draw(drawStartIndex.Value, drawEndIndex);

            drawStartIndex = null;
            drawEndIndex = 0;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
        }
    }
}
