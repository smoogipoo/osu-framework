// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Graphics.Veldrid.Textures;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventProcessor
    {
        private const string debug_output_path = "";

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
            printEventsForDebug(reader);
            reader.Reset();

            processUploads(reader);
            reader.Reset();

            processEvents(reader);
            reader.Reset();
        }

        private void printEventsForDebug(EventListReader reader)
        {
            if (string.IsNullOrEmpty(debug_output_path))
                return;

            StringBuilder builder = new StringBuilder();
            int indent = 0;

            while (reader.Next())
            {
                string info;
                int indentChange = 0;

                switch (reader.CurrentType())
                {
                    case RenderEventType.DrawNodeAction:
                    {
                        ref DrawNodeActionEvent e = ref reader.Current<DrawNodeActionEvent>();

                        info = $"DrawNode.{e.Action} ({e.DrawNode.Dereference<DrawNode>(deferredRenderer)})";

                        switch (e.Action)
                        {
                            case DrawNodeActionType.Enter:
                                indentChange += 2;
                                break;

                            case DrawNodeActionType.Exit:
                                indentChange -= 2;
                                break;
                        }

                        break;
                    }

                    default:
                    {
                        info = $"{reader.CurrentType().ToString()}";
                        break;
                    }
                }

                indent += Math.Min(0, indentChange);
                builder.AppendLine($"{new string(' ', indent)}{info}");
                indent += Math.Max(0, indentChange);
            }

            File.WriteAllText(debug_output_path, builder.ToString());
        }

        private void processUploads(EventListReader reader)
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
        }

        private void processEvents(EventListReader reader)
        {
            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.SetFrameBuffer:
                        processEvent(reader.Current<SetFrameBufferEvent>());
                        break;

                    case RenderEventType.UnsetFrameBuffer:
                        processEvent(reader.Current<UnsetFrameBufferEvent>());
                        break;

                    case RenderEventType.ResizeFrameBuffer:
                        processEvent(reader.Current<ResizeFrameBufferEvent>());
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
                }
            }
        }

        private void processEvent(in SetFrameBufferEvent e) => pipeline.SetFrameBuffer(e.FrameBuffer.Dereference<DeferredFrameBuffer>(deferredRenderer));

        private void processEvent(in UnsetFrameBufferEvent _) => pipeline.SetFrameBuffer(null);

        private void processEvent(in ResizeFrameBufferEvent e) => e.FrameBuffer.Dereference<DeferredFrameBuffer>(deferredRenderer).Resize(e.Size);

        private void processEvent(in SetShaderEvent e) => pipeline.SetShader(e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource);

        private void processEvent(in SetTextureEvent e) => pipeline.AttachTexture(e.Unit, e.Texture.Dereference<IVeldridTexture>(deferredRenderer));

        private void processEvent(in BindUniformBlockEvent e)
        {
            e.Shader.Dereference<DeferredShader>(deferredRenderer).Resource.BindUniformBlock(
                e.Name.Dereference<string>(deferredRenderer),
                e.Buffer.Dereference<IUniformBuffer>(deferredRenderer));
        }

        private void processEvent(in ClearEvent e) => pipeline.Clear(e.Info);

        private void processEvent(in SetDepthInfoEvent e) => pipeline.SetDepthInfo(e.Info);

        private void processEvent(in SetScissorEvent e) => pipeline.SetScissor(e.Scissor);

        private void processEvent(in SetScissorStateEvent e) => pipeline.SetScissorState(e.Enabled);

        private void processEvent(in SetStencilInfoEvent e) => pipeline.SetStencilInfo(e.Info);

        private void processEvent(in SetViewportEvent e) => pipeline.SetViewport(e.Viewport);

        private void processEvent(in SetBlendEvent e) => pipeline.SetBlend(e.Parameters);

        private void processEvent(in SetBlendMaskEvent e) => pipeline.SetBlendMask(e.Mask);

        private void processEvent(in SetUniformBufferDataEvent e) => e.Buffer.Dereference<IDeferredUniformBuffer>(deferredRenderer).MoveNext();

        private void processEvent(in FlushEvent e) => e.VertexBatch.Dereference<IDeferredVertexBatch>(deferredRenderer).Draw(pipeline, e.VertexCount);
    }
}
