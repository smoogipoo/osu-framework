// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class GraphicsStateStack<T>
        where T : IEquatable<T>
    {
        private readonly GraphicsStateChangeDelegate<T> onNewState;
        private readonly Stack<T> stack = new Stack<T>();
        private T currentState = default!;

        public GraphicsStateStack(GraphicsStateChangeDelegate<T> onNewState)
        {
            this.onNewState = onNewState;
        }

        public void Push(T state)
        {
            stack.Push(state);

            if (!state.Equals(currentState))
                onNewState(ref state);

            currentState = state;
        }

        public void Pop()
        {
            stack.Pop();

            if (stack.Count == 0)
                return;

            T newState = stack.Peek();

            if (!newState.Equals(currentState))
                onNewState(ref newState);

            currentState = newState;
        }

        public void Clear()
        {
            stack.Clear();
            currentState = default!;
        }
    }

    public delegate void GraphicsStateChangeDelegate<T>(ref T newState);
}
