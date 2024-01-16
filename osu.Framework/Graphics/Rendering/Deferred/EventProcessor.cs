// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Statistics;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    // internal class EventProcessor
    // {
    //     private readonly DeferredRenderer deferredRenderer;
    //
    //     public EventProcessor(DeferredRenderer deferredRenderer)
    //     {
    //         this.deferredRenderer = deferredRenderer;
    //     }
    //
    //     public void ProcessEvents(EventListReader reader)
    //     {
    //         while (reader.Next())
    //         {
    //             switch (reader.CurrentType())
    //             {
    //                 case RenderEventType.AddPrimitiveToBatch:
    //                     AddPrimitiveToBatchEvent e = reader.Current<AddPrimitiveToBatchEvent>();
    //                     IDeferredVertexBatch batch = e.VertexBatch.Dereference<IDeferredVertexBatch>(deferredRenderer);
    //
    //                     batch.WritePrimitive(e.Memory, commands);
    //                     break;
    //             }
    //         }
    //
    //         reader.Reset();
    //
    //         while (reader.Next())
    //         {
    //             switch (reader.CurrentType())
    //             {
    //                 case RenderEventType.AddPrimitiveToBatch:
    //                     processEvent(reader.Current<AddPrimitiveToBatchEvent>());
    //                     break;
    //
    //                 case RenderEventType.BindFrameBuffer:
    //                     processEvent(reader.Current<BindFrameBufferEvent>());
    //                     break;
    //
    //                 case RenderEventType.UnbindFrameBuffer:
    //                     processEvent(reader.Current<UnbindFrameBufferEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetShader:
    //                     processEvent(reader.Current<SetShaderEvent>());
    //                     break;
    //
    //                 case RenderEventType.BindTexture:
    //                     processEvent(reader.Current<BindTextureEvent>());
    //                     break;
    //
    //                 case RenderEventType.UnbindTexture:
    //                     processEvent(reader.Current<UnbindTextureEvent>());
    //                     break;
    //
    //                 case RenderEventType.BindUniformBlock:
    //                     processEvent(reader.Current<BindUniformBlockEvent>());
    //                     break;
    //
    //                 case RenderEventType.Clear:
    //                     processEvent(reader.Current<ClearEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetDepthInfo:
    //                     processEvent(reader.Current<SetDepthInfoEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetScissor:
    //                     processEvent(reader.Current<SetScissorEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetScissorState:
    //                     processEvent(reader.Current<SetScissorStateEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetStencilInfo:
    //                     processEvent(reader.Current<SetStencilInfoEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetViewport:
    //                     processEvent(reader.Current<SetViewportEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetBlend:
    //                     processEvent(reader.Current<SetBlendEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetBlendMask:
    //                     processEvent(reader.Current<SetBlendMaskEvent>());
    //                     break;
    //
    //                 case RenderEventType.SetUniformBufferData:
    //                     processEvent(reader.Current<SetUniformBufferDataEvent>());
    //                     break;
    //
    //                 case RenderEventType.Flush:
    //                     processEvent(reader.Current<FlushEvent>());
    //                     break;
    //
    //                 default:
    //                     throw new ArgumentOutOfRangeException();
    //             }
    //         }
    //     }
    //
    //     private void processEvent(AddPrimitiveToBatchEvent e)
    //     {
    //     }
    //
    //     private void processEvent(BindFrameBufferEvent e)
    //     {
    //         e.FrameBuffer.Dereference<DeferredFrameBuffer>(deferredRenderer).Resource.Bind();
    //     }
    //
    //     private void processEvent(UnbindFrameBufferEvent e)
    //     {
    //         e.FrameBuffer.Dereference<DeferredFrameBuffer>(deferredRenderer).Resource.Unbind();
    //     }
    //
    //     private void processEvent(SetShaderEvent e)
    //     {
    //         e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource.Bind();
    //     }
    //
    //     private void processEvent(BindTextureEvent e)
    //     {
    //         baseRenderer.BindTexture(e.Texture.Dereference<Texture>(deferredRenderer), e.Unit, e.WrapModeS, e.WrapModeT);
    //     }
    //
    //     private void processEvent(UnbindTextureEvent e)
    //     {
    //         baseRenderer.UnbindTexture(e.Unit);
    //     }
    //
    //     private void processEvent(BindUniformBlockEvent e)
    //     {
    //         e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource.BindUniformBlock(
    //             e.Name.Dereference<string>(deferredRenderer),
    //             e.Buffer.Dereference<IDeferredUniformBuffer>(deferredRenderer).GetBuffer());
    //     }
    //
    //     private void processEvent(ClearEvent e)
    //     {
    //         commands.ClearColorTarget(0, e.Info.Colour.ToRgbaFloat());
    //
    //         if (pipeline.Outputs.DepthAttachment != null)
    //             commands.ClearDepthStencil((float)e.Info.Depth, (byte)e.Info.Stencil);
    //     }
    //
    //     private void processEvent(SetDepthInfoEvent e)
    //     {
    //         pipeline.DepthStencilState.DepthTestEnabled = e.Info.DepthTest;
    //         pipeline.DepthStencilState.DepthWriteEnabled = e.Info.WriteDepth;
    //         pipeline.DepthStencilState.DepthComparison = e.Info.Function.ToComparisonKind();
    //     }
    //
    //     private void processEvent(SetScissorEvent e)
    //     {
    //         commands.SetScissorRect(0, (uint)e.Scissor.X, (uint)e.Scissor.Y, (uint)e.Scissor.Width, (uint)e.Scissor.Height);
    //     }
    //
    //     private void processEvent(SetScissorStateEvent e)
    //     {
    //         pipeline.RasterizerState.ScissorTestEnabled = e.Enabled;
    //     }
    //
    //     private void processEvent(SetStencilInfoEvent e)
    //     {
    //         pipeline.DepthStencilState.StencilTestEnabled = e.Info.StencilTest;
    //         pipeline.DepthStencilState.StencilReference = (uint)e.Info.TestValue;
    //         pipeline.DepthStencilState.StencilReadMask = pipeline.DepthStencilState.StencilWriteMask = (byte)e.Info.Mask;
    //         pipeline.DepthStencilState.StencilBack.Pass = pipeline.DepthStencilState.StencilFront.Pass = e.Info.TestPassedOperation.ToStencilOperation();
    //         pipeline.DepthStencilState.StencilBack.Fail = pipeline.DepthStencilState.StencilFront.Fail = e.Info.StencilTestFailOperation.ToStencilOperation();
    //         pipeline.DepthStencilState.StencilBack.DepthFail = pipeline.DepthStencilState.StencilFront.DepthFail = e.Info.DepthTestFailOperation.ToStencilOperation();
    //         pipeline.DepthStencilState.StencilBack.Comparison = pipeline.DepthStencilState.StencilFront.Comparison = e.Info.TestFunction.ToComparisonKind();
    //     }
    //
    //     private void processEvent(SetViewportEvent e)
    //     {
    //         commands.SetViewport(0, new Viewport(e.Viewport.Left, e.Viewport.Top, e.Viewport.Width, e.Viewport.Height, 0, 1));
    //     }
    //
    //     private void processEvent(SetBlendEvent e)
    //     {
    //         pipeline.BlendState.AttachmentStates[0].BlendEnabled = !e.Parameters.IsDisabled;
    //         pipeline.BlendState.AttachmentStates[0].SourceColorFactor = e.Parameters.Source.ToBlendFactor();
    //         pipeline.BlendState.AttachmentStates[0].SourceAlphaFactor = e.Parameters.SourceAlpha.ToBlendFactor();
    //         pipeline.BlendState.AttachmentStates[0].DestinationColorFactor = e.Parameters.Destination.ToBlendFactor();
    //         pipeline.BlendState.AttachmentStates[0].DestinationAlphaFactor = e.Parameters.DestinationAlpha.ToBlendFactor();
    //         pipeline.BlendState.AttachmentStates[0].ColorFunction = e.Parameters.RGBEquation.ToBlendFunction();
    //         pipeline.BlendState.AttachmentStates[0].AlphaFunction = e.Parameters.AlphaEquation.ToBlendFunction();
    //     }
    //
    //     private void processEvent(SetBlendMaskEvent e)
    //     {
    //         pipeline.BlendState.AttachmentStates[0].ColorWriteMask = e.Mask.ToColorWriteMask();
    //     }
    //
    //     private void processEvent(SetUniformBufferDataEvent e)
    //     {
    //         e.Buffer.Dereference<IDeferredUniformBuffer>(deferredRenderer).SetDataFromBuffer(e.Data.GetRegion(deferredRenderer));
    //     }
    //
    //     private void processEvent(FlushEvent e)
    //     {
    //         IDeferredVertexBatch batch = e.VertexBatch.Dereference<IDeferredVertexBatch>(deferredRenderer);
    //         batch.Draw(baseRenderer, e.VertexCount);
    //         FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
    //     }
    // }
}
