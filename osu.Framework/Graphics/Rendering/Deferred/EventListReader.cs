// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public ref struct EventListReader(List<byte> events)
    {
        private ReadOnlySpan<byte> events = CollectionsMarshal.AsSpan(events);

        public bool ReadType(out RenderEventType type)
        {
            type = default;

            if (events.Length == 0)
                return false;

            type = (RenderEventType)events[0];
            events = events[1..];

            return true;
        }

        public T Read<T>()
            where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();

            ReadOnlySpan<byte> span = events[..size];
            events = events[size..];

            return MemoryMarshal.Read<T>(span);
        }

        public void Skip(RenderEventType type)
        {
            switch (type)
            {
                case RenderEventType.AddVertexToBatch:
                    events = events[Unsafe.SizeOf<AddVertexToBatchEvent>()..];
                    break;

                case RenderEventType.BindFrameBuffer:
                    events = events[Unsafe.SizeOf<BindFrameBufferEvent>()..];
                    break;

                case RenderEventType.BindShader:
                    events = events[Unsafe.SizeOf<BindShaderEvent>()..];
                    break;

                case RenderEventType.BindTexture:
                    events = events[Unsafe.SizeOf<BindTextureEvent>()..];
                    break;

                case RenderEventType.BindUniformBlock:
                    events = events[Unsafe.SizeOf<BindUniformBlockEvent>()..];
                    break;

                case RenderEventType.Clear:
                    events = events[Unsafe.SizeOf<ClearEvent>()..];
                    break;

                case RenderEventType.Disposal:
                    events = events[Unsafe.SizeOf<DisposalEvent>()..];
                    break;

                case RenderEventType.DrawVertexBatch:
                    events = events[Unsafe.SizeOf<DrawVertexBatchEvent>()..];
                    break;

                case RenderEventType.ExpensiveOperation:
                    events = events[Unsafe.SizeOf<ExpensiveOperationEvent>()..];
                    break;

                case RenderEventType.PopDepthInfo:
                    events = events[Unsafe.SizeOf<PopDepthInfoEvent>()..];
                    break;

                case RenderEventType.PopMaskingInfo:
                    events = events[Unsafe.SizeOf<PopMaskingInfoEvent>()..];
                    break;

                case RenderEventType.PopProjectionMatrix:
                    events = events[Unsafe.SizeOf<PopProjectionMatrixEvent>()..];
                    break;

                case RenderEventType.PopQuadBatch:
                    events = events[Unsafe.SizeOf<PopQuadBatchEvent>()..];
                    break;

                case RenderEventType.PopScissor:
                    events = events[Unsafe.SizeOf<PopScissorEvent>()..];
                    break;

                case RenderEventType.PopScissorOffset:
                    events = events[Unsafe.SizeOf<PopScissorOffsetEvent>()..];
                    break;

                case RenderEventType.PopScissorState:
                    events = events[Unsafe.SizeOf<PopScissorStateEvent>()..];
                    break;

                case RenderEventType.PopStencilInfo:
                    events = events[Unsafe.SizeOf<PopStencilInfoEvent>()..];
                    break;

                case RenderEventType.PopViewport:
                    events = events[Unsafe.SizeOf<PopViewportEvent>()..];
                    break;

                case RenderEventType.PushDepthInfo:
                    events = events[Unsafe.SizeOf<PushDepthInfoEvent>()..];
                    break;

                case RenderEventType.PushMaskingInfo:
                    events = events[Unsafe.SizeOf<PushMaskingInfoEvent>()..];
                    break;

                case RenderEventType.PushProjectionMatrix:
                    events = events[Unsafe.SizeOf<PushProjectionMatrixEvent>()..];
                    break;

                case RenderEventType.PushQuadBatch:
                    events = events[Unsafe.SizeOf<PushQuadBatchEvent>()..];
                    break;

                case RenderEventType.PushScissor:
                    events = events[Unsafe.SizeOf<PushScissorEvent>()..];
                    break;

                case RenderEventType.PushScissorOffset:
                    events = events[Unsafe.SizeOf<PushScissorOffsetEvent>()..];
                    break;

                case RenderEventType.PushScissorState:
                    events = events[Unsafe.SizeOf<PushScissorStateEvent>()..];
                    break;

                case RenderEventType.PushStencilInfo:
                    events = events[Unsafe.SizeOf<PushStencilInfoEvent>()..];
                    break;

                case RenderEventType.PushViewport:
                    events = events[Unsafe.SizeOf<PushViewportEvent>()..];
                    break;

                case RenderEventType.SetBlend:
                    events = events[Unsafe.SizeOf<SetBlendEvent>()..];
                    break;

                case RenderEventType.SetBlendMask:
                    events = events[Unsafe.SizeOf<SetBlendMaskEvent>()..];
                    break;

                case RenderEventType.SetUniformBufferData:
                    events = events[Unsafe.SizeOf<SetUniformBufferDataEvent>()..];
                    break;

                case RenderEventType.UnbindFrameBuffer:
                    events = events[Unsafe.SizeOf<UnbindFrameBufferEvent>()..];
                    break;

                case RenderEventType.UnbindShader:
                    events = events[Unsafe.SizeOf<UnbindShaderEvent>()..];
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
