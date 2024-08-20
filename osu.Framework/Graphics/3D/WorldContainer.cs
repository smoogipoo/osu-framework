// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Graphics._3D
{
    public partial class WorldContainer : Drawable
    {
        private readonly SortedList<Model> children = new SortedList<Model>();

        public IReadOnlyList<Model> Children
        {
            get => children;
            set => ChildrenEnumerable = value;
        }

        public Model Child { set; }

        public IEnumerable<Model> ChildrenEnumerable { set; }

        public void Add(Model model)
        {
        }

        public void AddRange(IEnumerable<Model> collection)
        {
        }

        /// <summary>
        /// Remove the provided model from this container's children.
        /// </summary>
        /// <param name="model">The model to be removed.</param>
        /// <param name="disposeImmediately">Whether removed item should be immediately disposed.</param>
        /// <remarks>
        /// <paramref name="disposeImmediately"/> should be <c>true</c> unless the removed item is to be re-used in the future.
        /// If <c>false</c>, ensure the removed item is manually disposed (or added back to another part of the hierarchy) else
        /// object leakage may occur.
        /// </remarks>
        /// <returns>Whether the model was removed.</returns>
        public bool Remove(Model model, bool disposeImmediately)
        {
        }

        /// <summary>
        /// Remove all matching children from this container.
        /// </summary>
        /// <param name="range">The models to be removed.</param>
        /// <param name="disposeImmediately">Whether removed items should be immediately disposed.</param>
        /// <remarks>
        /// <paramref name="disposeImmediately"/> should be <c>true</c> unless the removed items are to be re-used in the future.
        /// If <c>false</c>, ensure removed items are manually disposed else object leakage may occur.
        /// </remarks>
        public void RemoveRange(IEnumerable<Model> range, bool disposeImmediately)
        {
        }

        /// <summary>
        /// Remove all matching children from this container.
        /// </summary>
        /// <param name="match">A predicate used to find matching items.</param>
        /// <param name="disposeImmediately">Whether removed items should be immediately disposed.</param>
        /// <remarks>
        /// <paramref name="disposeImmediately"/> should be <c>true</c> unless the removed items are to be re-used in the future.
        /// If <c>false</c>, ensure removed items are manually disposed else object leakage may occur.
        /// </remarks>
        /// <returns>The number of matching items removed.</returns>
        public int RemoveAll(Predicate<Model> match, bool disposeImmediately)
        {
        }

        protected class ChildComparer : IComparer<Model>
        {
            private readonly WorldContainer owner;

            public ChildComparer(WorldContainer owner)
            {
                this.owner = owner;
            }

            public int Compare(Model? x, Model? y) => owner.Compare(x, y);
        }

        /// <summary>
        /// Compares two <see cref="Children"/> to determine their sorting.
        /// </summary>
        /// <param name="x">The first child to compare.</param>
        /// <param name="y">The second child to compare.</param>
        /// <returns>-1 if <paramref name="x"/> comes before <paramref name="y"/>, and 1 otherwise.</returns>
        protected int Compare(Model? x, Model? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            int i = x.Position.Z.CompareTo(y.Position.Z);
            if (i != 0) return i;

            return x.ChildID.CompareTo(y.ChildID);
        }
    }
}
