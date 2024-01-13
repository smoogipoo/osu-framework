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
    internal class EventPainter
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

        public EventPainter(DeferredRenderer deferredRenderer, IRenderer baseRenderer)
        {
            this.deferredRenderer = deferredRenderer;
            this.baseRenderer = (VeldridRenderer)baseRenderer;

            stencilStack = new GraphicsStateStack<StencilInfo>(setStencil);
            viewportStack = new GraphicsStateStack<RectangleI>(setViewport);
            scissorStack = new GraphicsStateStack<RectangleI>(setScissor);
            depthStack = new GraphicsStateStack<DepthInfo>(setDepth);
            scissorStateStack = new GraphicsStateStack<bool>(setScissorState);
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
            commands.ClearColorTarget(0, e.Info.Colour.ToRgbaFloat());

            if (pipeline.Outputs.DepthAttachment != null)
                commands.ClearDepthStencil((float)e.Info.Depth, (byte)e.Info.Stencil);
        }

        public void ProcessEvent(DrawVertexBatchEvent e)
        {
            baseRenderer.FlushCurrentBatch(null);
        }

        public void ProcessEvent(PushDepthInfoEvent e) => depthStack.Push(e.Info);

        public void ProcessEvent(PopDepthInfoEvent e) => depthStack.Pop();

        private void setDepth(ref DepthInfo depth)
        {
            pipeline.DepthStencilState.DepthTestEnabled = depth.DepthTest;
            pipeline.DepthStencilState.DepthWriteEnabled = depth.WriteDepth;
            pipeline.DepthStencilState.DepthComparison = depth.Function.ToComparisonKind();
        }

        public void ProcessEvent(PushMaskingInfoEvent e)
        {
            baseRenderer.PushMaskingInfo(e.Info);
        }

        public void ProcessEvent(PopMaskingInfoEvent e)
        {
            baseRenderer.PopMaskingInfo();
        }

        public void ProcessEvent(PushProjectionMatrixEvent e)
        {
            baseRenderer.PushProjectionMatrix(e.Matrix);
        }

        public void ProcessEvent(PopProjectionMatrixEvent e)
        {
            baseRenderer.PopProjectionMatrix();
        }

        public void ProcessEvent(PushQuadBatchEvent e)
        {
            // Todo: This method is only used in recursion.
            baseRenderer.PushQuadBatch(e.VertexBatch.Resolve<IVertexBatch<TexturedVertex2D>>(deferredRenderer));
        }

        public void ProcessEvent(PopQuadBatchEvent e)
        {
            // Todo: This method is only used in recursion.
            baseRenderer.PopQuadBatch();
        }

        public void ProcessEvent(PushScissorEvent e) => scissorStack.Push(e.Scissor);

        public void ProcessEvent(PopScissorEvent e) => scissorStack.Pop();

        private void setScissor(ref RectangleI scissor)
        {
            commands.SetScissorRect(0, (uint)scissor.X, (uint)scissor.Y, (uint)scissor.Width, (uint)scissor.Height);
        }

        public void ProcessEvent(PushScissorOffsetEvent e)
        {
            // Todo: Pretty sure this event is broken (or only used in recursion).
            baseRenderer.PushScissorOffset(e.Offset);
        }

        public void ProcessEvent(PopScissorOffsetEvent e)
        {
            // Todo: Pretty sure this event is broken (or only used in recursion).
            baseRenderer.PopScissorOffset();
        }

        public void ProcessEvent(PushScissorStateEvent e) => scissorStateStack.Push(e.Enabled);

        public void ProcessEvent(PopScissorStateEvent e) => scissorStateStack.Pop();

        private void setScissorState(ref bool enabled)
        {
            pipeline.RasterizerState.ScissorTestEnabled = enabled;
        }

        public void ProcessEvent(PushStencilInfoEvent e) => stencilStack.Push(e.Info);

        public void ProcessEvent(PopStencilInfoEvent e) => stencilStack.Pop();

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

        public void ProcessEvent(PushViewportEvent e) => viewportStack.Push(e.Viewport);

        public void ProcessEvent(PopViewportEvent e) => viewportStack.Pop();

        private void setViewport(ref RectangleI viewport)
        {
            commands.SetViewport(0, new Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height, 0, 1));
        }

        public void ProcessEvent(SetBlendEvent e)
        {
            pipeline.BlendState.AttachmentStates[0].BlendEnabled = !e.Parameters.IsDisabled;
            pipeline.BlendState.AttachmentStates[0].SourceColorFactor = e.Parameters.Source.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].SourceAlphaFactor = e.Parameters.SourceAlpha.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].DestinationColorFactor = e.Parameters.Destination.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].DestinationAlphaFactor = e.Parameters.DestinationAlpha.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].ColorFunction = e.Parameters.RGBEquation.ToBlendFunction();
            pipeline.BlendState.AttachmentStates[0].AlphaFunction = e.Parameters.AlphaEquation.ToBlendFunction();
        }

        public void ProcessEvent(SetBlendMaskEvent e)
        {
            pipeline.BlendState.AttachmentStates[0].ColorWriteMask = e.Mask.ToColorWriteMask();
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

            batch.Draw(baseRenderer, drawCount);

            drawCount = 0;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
        }
    }
}
