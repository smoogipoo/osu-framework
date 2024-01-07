// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class EventListReader
    {
        private readonly List<byte> renderEvents;
        private int currentPos;

        public EventListReader(List<byte> renderEvents)
        {
            this.renderEvents = renderEvents;
        }

        public bool ReadType(out RenderEventType type)
        {
            type = default;

            if (currentPos >= renderEvents.Count)
                return false;

            type = (RenderEventType)renderEvents[currentPos++];
            return true;
        }

        public T Read<T>()
            where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();

            Span<byte> span = CollectionsMarshal.AsSpan(renderEvents).Slice(currentPos, size);
            currentPos += size;

            T result = MemoryMarshal.Read<T>(span);

            return result;
        }

        public void Skip(RenderEventType type)
        {
            switch (type)
            {
                case RenderEventType.AddVertexToBatch:
                    currentPos += Unsafe.SizeOf<AddVertexToBatchEvent>();
                    break;

                case RenderEventType.BindFrameBuffer:
                    currentPos += Unsafe.SizeOf<BindFrameBufferEvent>();
                    break;

                case RenderEventType.BindShader:
                    currentPos += Unsafe.SizeOf<BindShaderEvent>();
                    break;

                case RenderEventType.BindTexture:
                    currentPos += Unsafe.SizeOf<BindTextureEvent>();
                    break;

                case RenderEventType.BindUniformBlock:
                    currentPos += Unsafe.SizeOf<BindUniformBlockEvent>();
                    break;

                case RenderEventType.Clear:
                    currentPos += Unsafe.SizeOf<ClearEvent>();
                    break;

                case RenderEventType.Disposal:
                    currentPos += Unsafe.SizeOf<DisposalEvent>();
                    break;

                case RenderEventType.DrawVertexBatch:
                    currentPos += Unsafe.SizeOf<DrawVertexBatchEvent>();
                    break;

                case RenderEventType.ExpensiveOperation:
                    currentPos += Unsafe.SizeOf<ExpensiveOperationEvent>();
                    break;

                case RenderEventType.PopDepthInfo:
                    currentPos += Unsafe.SizeOf<PopDepthInfoEvent>();
                    break;

                case RenderEventType.PopMaskingInfo:
                    currentPos += Unsafe.SizeOf<PopMaskingInfoEvent>();
                    break;

                case RenderEventType.PopProjectionMatrix:
                    currentPos += Unsafe.SizeOf<PopProjectionMatrixEvent>();
                    break;

                case RenderEventType.PopQuadBatch:
                    currentPos += Unsafe.SizeOf<PopQuadBatchEvent>();
                    break;

                case RenderEventType.PopScissor:
                    currentPos += Unsafe.SizeOf<PopScissorEvent>();
                    break;

                case RenderEventType.PopScissorOffset:
                    currentPos += Unsafe.SizeOf<PopScissorOffsetEvent>();
                    break;

                case RenderEventType.PopScissorState:
                    currentPos += Unsafe.SizeOf<PopScissorStateEvent>();
                    break;

                case RenderEventType.PopStencilInfo:
                    currentPos += Unsafe.SizeOf<PopStencilInfoEvent>();
                    break;

                case RenderEventType.PopViewport:
                    currentPos += Unsafe.SizeOf<PopViewportEvent>();
                    break;

                case RenderEventType.PushDepthInfo:
                    currentPos += Unsafe.SizeOf<PushDepthInfoEvent>();
                    break;

                case RenderEventType.PushMaskingInfo:
                    currentPos += Unsafe.SizeOf<PushMaskingInfoEvent>();
                    break;

                case RenderEventType.PushProjectionMatrix:
                    currentPos += Unsafe.SizeOf<PushProjectionMatrixEvent>();
                    break;

                case RenderEventType.PushQuadBatch:
                    currentPos += Unsafe.SizeOf<PushQuadBatchEvent>();
                    break;

                case RenderEventType.PushScissor:
                    currentPos += Unsafe.SizeOf<PushScissorEvent>();
                    break;

                case RenderEventType.PushScissorOffset:
                    currentPos += Unsafe.SizeOf<PushScissorOffsetEvent>();
                    break;

                case RenderEventType.PushScissorState:
                    currentPos += Unsafe.SizeOf<PushScissorStateEvent>();
                    break;

                case RenderEventType.PushStencilInfo:
                    currentPos += Unsafe.SizeOf<PushStencilInfoEvent>();
                    break;

                case RenderEventType.PushViewport:
                    currentPos += Unsafe.SizeOf<PushViewportEvent>();
                    break;

                case RenderEventType.SetBlend:
                    currentPos += Unsafe.SizeOf<SetBlendEvent>();
                    break;

                case RenderEventType.SetBlendMask:
                    currentPos += Unsafe.SizeOf<SetBlendMaskEvent>();
                    break;

                case RenderEventType.SetUniformBufferData:
                    currentPos += Unsafe.SizeOf<SetUniformBufferDataEvent>();
                    break;

                case RenderEventType.UnbindFrameBuffer:
                    currentPos += Unsafe.SizeOf<UnbindFrameBufferEvent>();
                    break;

                case RenderEventType.UnbindShader:
                    currentPos += Unsafe.SizeOf<UnbindShaderEvent>();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
