// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal ref struct EventListReader
    {
        private readonly List<EventList.EventBuffer> buffers;

        private int bufferIndex;
        private ReadOnlySpan<byte> data;

        public EventListReader(List<EventList.EventBuffer> buffers)
        {
            this.buffers = buffers;
            data = buffers.Count > 0 ? buffers[0].GetData() : ReadOnlySpan<byte>.Empty;
            bufferIndex = 0;
        }

        public bool ReadType(out RenderEventType type)
        {
            type = default;

            if (data.Length == 0)
                return false;

            type = (RenderEventType)data[0];
            advanceBuffer(1);

            return true;
        }

        public T Read<T>()
            where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();

            ReadOnlySpan<byte> span = data[..size];
            advanceBuffer(size);

            return MemoryMarshal.Read<T>(span);
        }

        public void Skip(RenderEventType type)
        {
            switch (type)
            {
                case RenderEventType.AddVertexToBatch:
                    advanceBuffer(Unsafe.SizeOf<AddVertexToBatchEvent>());
                    break;

                case RenderEventType.BindFrameBuffer:
                    advanceBuffer(Unsafe.SizeOf<BindFrameBufferEvent>());
                    break;

                case RenderEventType.BindShader:
                    advanceBuffer(Unsafe.SizeOf<BindShaderEvent>());
                    break;

                case RenderEventType.BindTexture:
                    advanceBuffer(Unsafe.SizeOf<BindTextureEvent>());
                    break;

                case RenderEventType.BindUniformBlock:
                    advanceBuffer(Unsafe.SizeOf<BindUniformBlockEvent>());
                    break;

                case RenderEventType.Clear:
                    advanceBuffer(Unsafe.SizeOf<ClearEvent>());
                    break;

                case RenderEventType.DrawVertexBatch:
                    advanceBuffer(Unsafe.SizeOf<DrawVertexBatchEvent>());
                    break;

                case RenderEventType.PopDepthInfo:
                    advanceBuffer(Unsafe.SizeOf<PopDepthInfoEvent>());
                    break;

                case RenderEventType.PopMaskingInfo:
                    advanceBuffer(Unsafe.SizeOf<PopMaskingInfoEvent>());
                    break;

                case RenderEventType.PopProjectionMatrix:
                    advanceBuffer(Unsafe.SizeOf<PopProjectionMatrixEvent>());
                    break;

                case RenderEventType.PopQuadBatch:
                    advanceBuffer(Unsafe.SizeOf<PopQuadBatchEvent>());
                    break;

                case RenderEventType.PopScissor:
                    advanceBuffer(Unsafe.SizeOf<PopScissorEvent>());
                    break;

                case RenderEventType.PopScissorOffset:
                    advanceBuffer(Unsafe.SizeOf<PopScissorOffsetEvent>());
                    break;

                case RenderEventType.PopScissorState:
                    advanceBuffer(Unsafe.SizeOf<PopScissorStateEvent>());
                    break;

                case RenderEventType.PopStencilInfo:
                    advanceBuffer(Unsafe.SizeOf<PopStencilInfoEvent>());
                    break;

                case RenderEventType.PopViewport:
                    advanceBuffer(Unsafe.SizeOf<PopViewportEvent>());
                    break;

                case RenderEventType.PushDepthInfo:
                    advanceBuffer(Unsafe.SizeOf<PushDepthInfoEvent>());
                    break;

                case RenderEventType.PushMaskingInfo:
                    advanceBuffer(Unsafe.SizeOf<PushMaskingInfoEvent>());
                    break;

                case RenderEventType.PushProjectionMatrix:
                    advanceBuffer(Unsafe.SizeOf<PushProjectionMatrixEvent>());
                    break;

                case RenderEventType.PushQuadBatch:
                    advanceBuffer(Unsafe.SizeOf<PushQuadBatchEvent>());
                    break;

                case RenderEventType.PushScissor:
                    advanceBuffer(Unsafe.SizeOf<PushScissorEvent>());
                    break;

                case RenderEventType.PushScissorOffset:
                    advanceBuffer(Unsafe.SizeOf<PushScissorOffsetEvent>());
                    break;

                case RenderEventType.PushScissorState:
                    advanceBuffer(Unsafe.SizeOf<PushScissorStateEvent>());
                    break;

                case RenderEventType.PushStencilInfo:
                    advanceBuffer(Unsafe.SizeOf<PushStencilInfoEvent>());
                    break;

                case RenderEventType.PushViewport:
                    advanceBuffer(Unsafe.SizeOf<PushViewportEvent>());
                    break;

                case RenderEventType.SetBlend:
                    advanceBuffer(Unsafe.SizeOf<SetBlendEvent>());
                    break;

                case RenderEventType.SetBlendMask:
                    advanceBuffer(Unsafe.SizeOf<SetBlendMaskEvent>());
                    break;

                case RenderEventType.SetUniformBufferData:
                    advanceBuffer(Unsafe.SizeOf<SetUniformBufferDataEvent>());
                    break;

                case RenderEventType.UnbindFrameBuffer:
                    advanceBuffer(Unsafe.SizeOf<UnbindFrameBufferEvent>());
                    break;

                case RenderEventType.UnbindShader:
                    advanceBuffer(Unsafe.SizeOf<UnbindShaderEvent>());
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void advanceBuffer(int length)
        {
            data = data[length..];

            if (data.Length == 0 && bufferIndex < buffers.Count - 1)
                data = buffers[++bufferIndex].GetData();
        }
    }
}
