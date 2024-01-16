// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Veldrid.Pipelines
{
    internal class VeldridDrawPipeline : VeldridRenderPipeline
    {
        private static readonly GlobalStatistic<int> stat_graphics_pipeline_created = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total pipelines created");

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();
        private readonly Dictionary<int, VeldridTextureResources> attachedTextures = new Dictionary<int, VeldridTextureResources>();
        private readonly Dictionary<string, IVeldridUniformBuffer> attachedUniformBuffers = new Dictionary<string, IVeldridUniformBuffer>();

        private GraphicsPipelineDescription pipelineDesc = new GraphicsPipelineDescription
        {
            RasterizerState = RasterizerStateDescription.CullNone,
            BlendState = BlendStateDescription.SingleOverrideBlend,
            ShaderSet = { VertexLayouts = new VertexLayoutDescription[1] }
        };

        private IFrameBuffer? currentFrameBuffer;
        private IShader? currentShader;
        private IVeldridVertexBuffer? currentVertexBuffer;
        private VeldridIndexBuffer? currentIndexBuffer;

        public VeldridDrawPipeline(IVeldridDevice device)
            : base(device)
        {
            pipelineDesc.Outputs = Device.SwapchainFramebuffer.OutputDescription;
        }

        public override void Begin()
        {
            base.Begin();

            attachedTextures.Clear();
            attachedUniformBuffers.Clear();
            currentFrameBuffer = null;
            currentShader = null;
            currentVertexBuffer = null;
            currentIndexBuffer = null;
        }

        public void Clear(ClearInfo clearInfo)
        {
            Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            var framebuffer = (currentFrameBuffer as VeldridFrameBuffer)?.Framebuffer ?? Device.SwapchainFramebuffer;
            if (framebuffer.DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        public void SetScissorState(bool enabled) => pipelineDesc.RasterizerState.ScissorTestEnabled = enabled;

        public void SetShader(IShader shader)
        {
            currentShader = shader;
            pipelineDesc.ShaderSet.Shaders = ((VeldridShader)shader).Shaders;
        }

        public void SetBlend(BlendingParameters blendingParameters)
        {
            pipelineDesc.BlendState.AttachmentStates[0].BlendEnabled = !blendingParameters.IsDisabled;
            pipelineDesc.BlendState.AttachmentStates[0].SourceColorFactor = blendingParameters.Source.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].SourceAlphaFactor = blendingParameters.SourceAlpha.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].DestinationColorFactor = blendingParameters.Destination.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].DestinationAlphaFactor = blendingParameters.DestinationAlpha.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].ColorFunction = blendingParameters.RGBEquation.ToBlendFunction();
            pipelineDesc.BlendState.AttachmentStates[0].AlphaFunction = blendingParameters.AlphaEquation.ToBlendFunction();
        }

        public void SetBlendMask(BlendingMask blendingMask)
        {
            pipelineDesc.BlendState.AttachmentStates[0].ColorWriteMask = blendingMask.ToColorWriteMask();
        }

        public void SetViewport(RectangleI viewport)
        {
            Commands.SetViewport(0, new Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height, 0, 1));
        }

        public void SetScissor(RectangleI scissor)
        {
            Commands.SetScissorRect(0, (uint)scissor.X, (uint)scissor.Y, (uint)scissor.Width, (uint)scissor.Height);
        }

        public void SetDepthInfo(DepthInfo depthInfo)
        {
            pipelineDesc.DepthStencilState.DepthTestEnabled = depthInfo.DepthTest;
            pipelineDesc.DepthStencilState.DepthWriteEnabled = depthInfo.WriteDepth;
            pipelineDesc.DepthStencilState.DepthComparison = depthInfo.Function.ToComparisonKind();
        }

        public void SetStencilInfo(StencilInfo stencilInfo)
        {
            pipelineDesc.DepthStencilState.StencilTestEnabled = stencilInfo.StencilTest;
            pipelineDesc.DepthStencilState.StencilReference = (uint)stencilInfo.TestValue;
            pipelineDesc.DepthStencilState.StencilReadMask = pipelineDesc.DepthStencilState.StencilWriteMask = (byte)stencilInfo.Mask;
            pipelineDesc.DepthStencilState.StencilBack.Pass = pipelineDesc.DepthStencilState.StencilFront.Pass = stencilInfo.TestPassedOperation.ToStencilOperation();
            pipelineDesc.DepthStencilState.StencilBack.Fail = pipelineDesc.DepthStencilState.StencilFront.Fail = stencilInfo.StencilTestFailOperation.ToStencilOperation();
            pipelineDesc.DepthStencilState.StencilBack.DepthFail = pipelineDesc.DepthStencilState.StencilFront.DepthFail = stencilInfo.DepthTestFailOperation.ToStencilOperation();
            pipelineDesc.DepthStencilState.StencilBack.Comparison = pipelineDesc.DepthStencilState.StencilFront.Comparison = stencilInfo.TestFunction.ToComparisonKind();
        }

        public void SetFrameBuffer(IFrameBuffer? frameBuffer)
        {
            currentFrameBuffer = frameBuffer;

            Framebuffer fb = (frameBuffer as VeldridFrameBuffer)?.Framebuffer ?? Device.SwapchainFramebuffer;

            Commands.SetFramebuffer(fb);
            pipelineDesc.Outputs = fb.OutputDescription;
        }

        public void SetVertexBuffer(IVeldridVertexBuffer vertexBuffer)
        {
            currentVertexBuffer = vertexBuffer;

            Commands.SetVertexBuffer(0, vertexBuffer.Buffer);
            pipelineDesc.ShaderSet.VertexLayouts[0] = vertexBuffer.Layout;

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);
        }

        public void SetIndexBuffer(VeldridIndexBuffer indexBuffer)
        {
            currentIndexBuffer = indexBuffer;
            Commands.SetIndexBuffer(indexBuffer.Buffer, VeldridIndexBuffer.FORMAT);
        }

        public void AttachTexture(int unit, VeldridTextureResources texture) => attachedTextures[unit] = texture;

        public void AttachUniformBuffer(string name, IVeldridUniformBuffer buffer) => attachedUniformBuffers[name] = buffer;

        public void DrawVertices(global::Veldrid.PrimitiveTopology topology, int vertexStart, int verticesCount)
        {
            if (currentShader == null)
                throw new InvalidOperationException("No shader bound.");

            if (currentIndexBuffer == null)
                throw new InvalidOperationException("No index buffer bound.");

            VeldridShader veldridShader = (VeldridShader)currentShader;

            pipelineDesc.PrimitiveTopology = topology;
            Array.Resize(ref pipelineDesc.ResourceLayouts, veldridShader.LayoutCount);

            // Activate texture layouts.
            foreach (var (unit, _) in attachedTextures)
            {
                var layout = veldridShader.GetTextureLayout(unit);
                if (layout == null)
                    continue;

                pipelineDesc.ResourceLayouts[layout.Set] = layout.Layout;
            }

            // Activate uniform buffer layouts.
            foreach (var (name, _) in attachedUniformBuffers)
            {
                var layout = veldridShader.GetUniformBufferLayout(name);
                if (layout == null)
                    continue;

                pipelineDesc.ResourceLayouts[layout.Set] = layout.Layout;
            }

            // Activate the pipeline.
            Commands.SetPipeline(createPipeline());

            // Activate texture resources.
            foreach (var (unit, texture) in attachedTextures)
            {
                var layout = veldridShader.GetTextureLayout(unit);
                if (layout == null)
                    continue;

                Commands.SetGraphicsResourceSet((uint)layout.Set, texture.GetResourceSet(layout.Layout));
            }

            // Activate uniform buffer resources.
            foreach (var (name, buffer) in attachedUniformBuffers)
            {
                var layout = veldridShader.GetUniformBufferLayout(name);
                if (layout == null)
                    continue;

                Commands.SetGraphicsResourceSet((uint)layout.Set, buffer.GetResourceSet(layout.Layout));
            }

            int indexStart = currentIndexBuffer.TranslateToIndex(vertexStart);
            int indicesCount = currentIndexBuffer.TranslateToIndex(verticesCount);
            Commands.DrawIndexed((uint)indicesCount, 1, (uint)indexStart, 0, 0);
        }

        private Pipeline createPipeline()
        {
            if (!pipelineCache.TryGetValue(pipelineDesc, out var instance))
            {
                pipelineCache[pipelineDesc.Clone()] = instance = Factory.CreateGraphicsPipeline(ref pipelineDesc);
                stat_graphics_pipeline_created.Value++;
            }

            return instance;
        }
    }
}
