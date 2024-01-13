// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Statistics;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventProcessor
    {
        private ref GraphicsPipelineDescription pipeline => ref baseRenderer.GetPipeline();
        private CommandList commands => baseRenderer.Commands;

        private readonly GraphicsStateStack<StencilInfo> stencilStack;
        private readonly GraphicsStateStack<RectangleI> viewportStack;
        private readonly GraphicsStateStack<RectangleI> scissorStack;
        private readonly GraphicsStateStack<DepthInfo> depthStack;
        private readonly GraphicsStateStack<bool> scissorStateStack;

        private readonly DeferredRenderer deferredRenderer;
        private readonly VeldridRenderer baseRenderer;

        private IDeferredVertexBatch? currentDrawBatch;
        private int drawCount;

        public EventProcessor(DeferredRenderer deferredRenderer, IRenderer baseRenderer)
        {
            this.deferredRenderer = deferredRenderer;
            this.baseRenderer = (VeldridRenderer)baseRenderer;

            stencilStack = new GraphicsStateStack<StencilInfo>(setStencil);
            viewportStack = new GraphicsStateStack<RectangleI>(setViewport);
            scissorStack = new GraphicsStateStack<RectangleI>(setScissor);
            depthStack = new GraphicsStateStack<DepthInfo>(setDepth);
            scissorStateStack = new GraphicsStateStack<bool>(setScissorState);

            baseRenderer.OnFlush += flushCurrentBatch;
        }

        public void ProcessEvents(EventListReader reader)
        {
            stencilStack.Clear();
            viewportStack.Clear();
            scissorStack.Clear();
            depthStack.Clear();
            scissorStateStack.Clear();
            currentDrawBatch = null;
            drawCount = 0;

            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddVertexToBatch:
                        AddVertexToBatchEvent e = reader.Current<AddVertexToBatchEvent>();
                        e.VertexBatch.Resolve<IDeferredVertexBatch>(deferredRenderer).Write(e.Memory, commands);
                        break;
                }
            }

            reader.Reset();

            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.AddVertexToBatch:
                        processEvent(reader.Current<AddVertexToBatchEvent>());
                        break;

                    case RenderEventType.BindFrameBuffer:
                        processEvent(reader.Current<BindFrameBufferEvent>());
                        break;

                    case RenderEventType.BindShader:
                        processEvent(reader.Current<BindShaderEvent>());
                        break;

                    case RenderEventType.BindTexture:
                        processEvent(reader.Current<BindTextureEvent>());
                        break;

                    case RenderEventType.BindUniformBlock:
                        processEvent(reader.Current<BindUniformBlockEvent>());
                        break;

                    case RenderEventType.Clear:
                        processEvent(reader.Current<ClearEvent>());
                        break;

                    case RenderEventType.DrawVertexBatch:
                        processEvent(reader.Current<DrawVertexBatchEvent>());
                        break;

                    case RenderEventType.PopDepthInfo:
                        processEvent(reader.Current<PopDepthInfoEvent>());
                        break;

                    case RenderEventType.PopMaskingInfo:
                        processEvent(reader.Current<PopMaskingInfoEvent>());
                        break;

                    case RenderEventType.PopProjectionMatrix:
                        processEvent(reader.Current<PopProjectionMatrixEvent>());
                        break;

                    case RenderEventType.PopQuadBatch:
                        processEvent(reader.Current<PopQuadBatchEvent>());
                        break;

                    case RenderEventType.PopScissor:
                        processEvent(reader.Current<PopScissorEvent>());
                        break;

                    case RenderEventType.PopScissorOffset:
                        processEvent(reader.Current<PopScissorOffsetEvent>());
                        break;

                    case RenderEventType.PopScissorState:
                        processEvent(reader.Current<PopScissorStateEvent>());
                        break;

                    case RenderEventType.PopStencilInfo:
                        processEvent(reader.Current<PopStencilInfoEvent>());
                        break;

                    case RenderEventType.PopViewport:
                        processEvent(reader.Current<PopViewportEvent>());
                        break;

                    case RenderEventType.PushDepthInfo:
                        processEvent(reader.Current<PushDepthInfoEvent>());
                        break;

                    case RenderEventType.PushMaskingInfo:
                        processEvent(reader.Current<PushMaskingInfoEvent>());
                        break;

                    case RenderEventType.PushProjectionMatrix:
                        processEvent(reader.Current<PushProjectionMatrixEvent>());
                        break;

                    case RenderEventType.PushQuadBatch:
                        processEvent(reader.Current<PushQuadBatchEvent>());
                        break;

                    case RenderEventType.PushScissor:
                        processEvent(reader.Current<PushScissorEvent>());
                        break;

                    case RenderEventType.PushScissorOffset:
                        processEvent(reader.Current<PushScissorOffsetEvent>());
                        break;

                    case RenderEventType.PushScissorState:
                        processEvent(reader.Current<PushScissorStateEvent>());
                        break;

                    case RenderEventType.PushStencilInfo:
                        processEvent(reader.Current<PushStencilInfoEvent>());
                        break;

                    case RenderEventType.PushViewport:
                        processEvent(reader.Current<PushViewportEvent>());
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

                    case RenderEventType.UnbindFrameBuffer:
                        processEvent(reader.Current<UnbindFrameBufferEvent>());
                        break;

                    case RenderEventType.UnbindShader:
                        processEvent(reader.Current<UnbindShaderEvent>());
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            flushCurrentBatch(FlushBatchSource.FinishFrame);
        }

        private void processEvent(AddVertexToBatchEvent e)
        {
            IDeferredVertexBatch batch = e.VertexBatch.Resolve<IDeferredVertexBatch>(deferredRenderer);

            if (currentDrawBatch != null && batch != currentDrawBatch)
                flushCurrentBatch(FlushBatchSource.BindBuffer);

            currentDrawBatch = batch;
            drawCount++;
        }

        private void processEvent(BindFrameBufferEvent e)
        {
            e.FrameBuffer.Resolve<DeferredFrameBuffer>(deferredRenderer).Resource.Bind();
        }

        private void processEvent(BindShaderEvent e)
        {
            e.Shader.Resolve<DeferredShader>(deferredRenderer).Resource.Bind();
        }

        private void processEvent(BindTextureEvent e)
        {
            baseRenderer.BindTexture(e.Texture.Resolve<Texture>(deferredRenderer), e.Unit, e.WrapModeS, e.WrapModeT);
        }

        private void processEvent(BindUniformBlockEvent e)
        {
            e.Shader.Resolve<DeferredShader>(deferredRenderer).Resource.BindUniformBlock(
                e.Name.Resolve<string>(deferredRenderer),
                e.Buffer.Resolve<IDeferredUniformBuffer>(deferredRenderer).GetBuffer());
        }

        private void processEvent(ClearEvent e)
        {
            commands.ClearColorTarget(0, e.Info.Colour.ToRgbaFloat());

            if (pipeline.Outputs.DepthAttachment != null)
                commands.ClearDepthStencil((float)e.Info.Depth, (byte)e.Info.Stencil);
        }

        private void processEvent(DrawVertexBatchEvent e)
        {
            baseRenderer.FlushCurrentBatch(null);
        }

        private void processEvent(PushDepthInfoEvent e) => depthStack.Push(e.Info);

        private void processEvent(PopDepthInfoEvent e) => depthStack.Pop();

        private void setDepth(ref DepthInfo depth)
        {
            pipeline.DepthStencilState.DepthTestEnabled = depth.DepthTest;
            pipeline.DepthStencilState.DepthWriteEnabled = depth.WriteDepth;
            pipeline.DepthStencilState.DepthComparison = depth.Function.ToComparisonKind();
        }

        private void processEvent(PushMaskingInfoEvent e)
        {
            baseRenderer.PushMaskingInfo(e.Info);
        }

        private void processEvent(PopMaskingInfoEvent e)
        {
            baseRenderer.PopMaskingInfo();
        }

        private void processEvent(PushProjectionMatrixEvent e)
        {
            baseRenderer.PushProjectionMatrix(e.Matrix);
        }

        private void processEvent(PopProjectionMatrixEvent e)
        {
            baseRenderer.PopProjectionMatrix();
        }

        private void processEvent(PushQuadBatchEvent e)
        {
            // Todo: This method is only used in recursion.
            baseRenderer.PushQuadBatch(e.VertexBatch.Resolve<IVertexBatch<TexturedVertex2D>>(deferredRenderer));
        }

        private void processEvent(PopQuadBatchEvent e)
        {
            // Todo: This method is only used in recursion.
            baseRenderer.PopQuadBatch();
        }

        private void processEvent(PushScissorEvent e) => scissorStack.Push(e.Scissor);

        private void processEvent(PopScissorEvent e) => scissorStack.Pop();

        private void setScissor(ref RectangleI scissor)
        {
            commands.SetScissorRect(0, (uint)scissor.X, (uint)scissor.Y, (uint)scissor.Width, (uint)scissor.Height);
        }

        private void processEvent(PushScissorOffsetEvent e)
        {
            // Todo: Pretty sure this event is broken (or only used in recursion).
            baseRenderer.PushScissorOffset(e.Offset);
        }

        private void processEvent(PopScissorOffsetEvent e)
        {
            // Todo: Pretty sure this event is broken (or only used in recursion).
            baseRenderer.PopScissorOffset();
        }

        private void processEvent(PushScissorStateEvent e) => scissorStateStack.Push(e.Enabled);

        private void processEvent(PopScissorStateEvent e) => scissorStateStack.Pop();

        private void setScissorState(ref bool enabled)
        {
            pipeline.RasterizerState.ScissorTestEnabled = enabled;
        }

        private void processEvent(PushStencilInfoEvent e) => stencilStack.Push(e.Info);

        private void processEvent(PopStencilInfoEvent e) => stencilStack.Pop();

        private void setStencil(ref StencilInfo stencil)
        {
            pipeline.DepthStencilState.StencilTestEnabled = stencil.StencilTest;
            pipeline.DepthStencilState.StencilReference = (uint)stencil.TestValue;
            pipeline.DepthStencilState.StencilReadMask = pipeline.DepthStencilState.StencilWriteMask = (byte)stencil.Mask;
            pipeline.DepthStencilState.StencilBack.Pass = pipeline.DepthStencilState.StencilFront.Pass = stencil.TestPassedOperation.ToStencilOperation();
            pipeline.DepthStencilState.StencilBack.Fail = pipeline.DepthStencilState.StencilFront.Fail = stencil.StencilTestFailOperation.ToStencilOperation();
            pipeline.DepthStencilState.StencilBack.DepthFail = pipeline.DepthStencilState.StencilFront.DepthFail = stencil.DepthTestFailOperation.ToStencilOperation();
            pipeline.DepthStencilState.StencilBack.Comparison = pipeline.DepthStencilState.StencilFront.Comparison = stencil.TestFunction.ToComparisonKind();
        }

        private void processEvent(PushViewportEvent e) => viewportStack.Push(e.Viewport);

        private void processEvent(PopViewportEvent e) => viewportStack.Pop();

        private void setViewport(ref RectangleI viewport)
        {
            commands.SetViewport(0, new Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height, 0, 1));
        }

        private void processEvent(SetBlendEvent e)
        {
            pipeline.BlendState.AttachmentStates[0].BlendEnabled = !e.Parameters.IsDisabled;
            pipeline.BlendState.AttachmentStates[0].SourceColorFactor = e.Parameters.Source.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].SourceAlphaFactor = e.Parameters.SourceAlpha.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].DestinationColorFactor = e.Parameters.Destination.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].DestinationAlphaFactor = e.Parameters.DestinationAlpha.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].ColorFunction = e.Parameters.RGBEquation.ToBlendFunction();
            pipeline.BlendState.AttachmentStates[0].AlphaFunction = e.Parameters.AlphaEquation.ToBlendFunction();
        }

        private void processEvent(SetBlendMaskEvent e)
        {
            pipeline.BlendState.AttachmentStates[0].ColorWriteMask = e.Mask.ToColorWriteMask();
        }

        private void processEvent(SetUniformBufferDataEvent e)
        {
            e.Buffer.Resolve<IDeferredUniformBuffer>(deferredRenderer).SetDataFromBuffer(e.Data.GetBuffer(deferredRenderer));
        }

        private void processEvent(UnbindFrameBufferEvent e)
        {
            e.FrameBuffer.Resolve<DeferredFrameBuffer>(deferredRenderer).Resource.Unbind();
        }

        private void processEvent(UnbindShaderEvent e)
        {
            e.Shader.Resolve<DeferredShader>(deferredRenderer).Resource.Unbind();
        }

        private void flushCurrentBatch(FlushBatchSource? source)
        {
            if (currentDrawBatch == null)
                return;

            // Prevent re-entrancy
            IDeferredVertexBatch batch = currentDrawBatch;
            currentDrawBatch = null;

            batch.Draw(baseRenderer, drawCount);

            drawCount = 0;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
        }
    }
}
