// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    public class MaskingBuffer : IDisposable
    {
        /// <summary>
        /// Number of masking infos per array buffer. Must be at least 2.
        /// </summary>
        /// <remarks>
        /// The OpenGL spec guarantees a minimum size of 128MB for this type of buffer. This may differ for other backends.
        /// The total size as measured here, is just under 2MB.
        /// </remarks>
        public const int ARRAY_BUFFER_SIZE = 8192;

        /// <summary>
        /// Number of masking infos per uniform buffer. Must be at least 2.
        /// </summary>
        /// <remarks>
        /// The OpenGL spec guarantees a minimum size of 16KB for this type of buffer. This may differ for other backends.
        /// The total size as measured here, is just under 16KB.
        /// </remarks>
        public const int UNIFORM_BUFFER_SIZE = 64;

        public int CurrentOffset => currentIndex % bufferSize;

        private readonly IRenderer renderer;
        private readonly List<IArrayBuffer<ShaderMaskingInfo>> buffers = new List<IArrayBuffer<ShaderMaskingInfo>>();
        private readonly Stack<int> lastIndices = new Stack<int>();
        private readonly int bufferSize;

        /// <summary>
        /// A monotonically increasing (during a frame) index at which items are added to the buffer.
        /// </summary>
        private int nextAdditionIndex;

        /// <summary>
        /// The index which tracks the current masking info. This is incremented and decremented during a frame.
        /// </summary>
        private int currentIndex = -1;

        public MaskingBuffer(IRenderer renderer)
        {
            Trace.Assert(ARRAY_BUFFER_SIZE >= 2);
            Trace.Assert(UNIFORM_BUFFER_SIZE >= 2);

            this.renderer = renderer;

            ensureCapacity(1);

            bufferSize = buffers[0].Size;
        }

        public int Push(ShaderMaskingInfo maskingInfo)
        {
            lastIndices.Push(currentIndex);

            int currentBufferIndex = currentIndex / bufferSize;
            int currentBufferOffset = currentIndex % bufferSize;
            int newIndex = nextAdditionIndex++;
            int newBufferIndex = newIndex / bufferSize;
            int newBufferOffset = newIndex % bufferSize;

            // Ensure that the item can be stored.
            ensureCapacity(newBufferIndex + 1);

            // Flush the pipeline if this invocation transitions to a new buffer.
            if (newBufferIndex != currentBufferIndex)
            {
                renderer.FlushCurrentBatch(FlushBatchSource.SetMasking);

                //
                // When transitioning to a new buffer, we want to reduce a certain "ping-ponging" effect that occurs when there are many masking drawables at one level of the hierarchy.
                // For example, suppose that there are 1000 masking drawables at the current level in the draw hierarchy, and each of those draws 4 vertices.
                // Each drawable will Push() to transition to buffer X+1, and Pop() to transition back to buffer X.
                // Since we flush every Pop(), this is equivalent to drawing 4 vertices per flush.
                //
                // A little hack is employed to resolve this issue:
                // When transitioning to a new buffer, we copy the last item from the last buffer into the new buffer,
                // and adjust the stack so that we no longer refer to a position inside the last buffer upon a Pop().
                //
                // If the item to be copied would end up at the last index in the new buffer, then we also need to advance the buffer itself,
                // otherwise the user's item would be placed in a new buffer anyway and undo this optimisation.
                //
                // This can be thought of as a trade-off of space for performance (by reducing flushes).

                // If the copy would be placed at the end of the new buffer, advance the buffer.
                if (newBufferOffset == bufferSize - 1)
                {
                    nextAdditionIndex++;
                    newIndex++;
                    newBufferIndex++;
                    newBufferOffset = 0;

                    ensureCapacity(newBufferIndex + 1);
                }

                // Copy the current item from the last buffer into the new buffer.
                buffers[newBufferIndex][newBufferOffset] = buffers[currentBufferIndex][currentBufferOffset];

                // Adjust the stack so the last index points to the index in the new buffer, instead of currentIndex (from the old buffer).
                lastIndices.Pop();
                lastIndices.Push(newIndex);

                nextAdditionIndex++;
                newIndex++;
                newBufferOffset++;
            }

            // Add the item.
            buffers[newBufferIndex][newBufferOffset] = maskingInfo;
            currentIndex = newIndex;

            return newBufferOffset;
        }

        public void Pop()
        {
            int currentBufferIndex = currentIndex / bufferSize;
            int newIndex = lastIndices.Pop();
            int newBufferIndex = newIndex / bufferSize;

            // Flush the pipeline if this invocation transitions to a new buffer.
            if (newBufferIndex != currentBufferIndex)
                renderer.FlushCurrentBatch(FlushBatchSource.SetMasking);

            currentIndex = newIndex;
        }

        public IArrayBuffer<ShaderMaskingInfo> CurrentBuffer
        {
            get
            {
                Trace.Assert(currentIndex != -1);
                return buffers[currentIndex / bufferSize];
            }
        }

        public void Reset()
        {
            nextAdditionIndex = 0;
            currentIndex = -1;
            lastIndices.Clear();
        }

        private void ensureCapacity(int size)
        {
            while (buffers.Count < size)
                buffers.Add(renderer.CreateArrayBuffer<ShaderMaskingInfo>(UNIFORM_BUFFER_SIZE, ARRAY_BUFFER_SIZE));
        }

        public void Dispose()
        {
            foreach (var buffer in buffers)
                buffer.Dispose();
        }
    }
}
