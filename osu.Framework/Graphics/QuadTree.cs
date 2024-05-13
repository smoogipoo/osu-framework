// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Graphics
{
    public class QuadTree<TPoint>
        where TPoint : struct, QuadTreePoint
    {
        private readonly QuadTreeNode rootNode;

        public QuadTree(RectangleF area)
        {
            rootNode = new QuadTreeNode(null, area);
        }

        public bool Insert(TPoint point) => rootNode.Insert(point);

        public bool TryGetClosest(TPoint point, out TPoint closest) => rootNode.TryGetClosest(point, out closest);

        public IEnumerable<RectangleF> EnumerateAreas() => rootNode.EnumerateAreas();

        private class QuadTreeNode
        {
            private const int capacity = 4;

            public readonly RectangleF Area;
            private readonly QuadTreeNode? parent;

            private List<TPoint>? points = new List<TPoint>(capacity);
            private QuadTreeNode? topLeft;
            private QuadTreeNode? topRight;
            private QuadTreeNode? bottomLeft;
            private QuadTreeNode? bottomRight;

            public QuadTreeNode(QuadTreeNode? parent, RectangleF area)
            {
                this.parent = parent;
                Area = area;
            }

            public bool Insert(TPoint point)
            {
                // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
                if (!Area.Contains(point.Location))
                    return false;

                if (points?.Count == capacity)
                    subDivide();

                if (points != null)
                {
                    points.Add(point);
                    return true;
                }

                Debug.Assert(topLeft != null);
                Debug.Assert(topRight != null);
                Debug.Assert(bottomLeft != null);
                Debug.Assert(bottomRight != null);

                return topLeft.Insert(point)
                       || topRight.Insert(point)
                       || bottomLeft.Insert(point)
                       || bottomRight.Insert(point);
            }

            public bool TryGetClosest(TPoint point, out TPoint closest)
            {
                QuadTreeNode? closestNode = findContainingNode(point);

                if (closestNode == null)
                {
                    closest = default;
                    return false;
                }

                Debug.Assert(closestNode.points != null);

                float distToLeft = MathF.Abs(point.Location.X - closestNode.Area.Left);
                float distToRight = MathF.Abs(point.Location.X - closestNode.Area.Right);
                float distToTop = MathF.Abs(point.Location.Y - closestNode.Area.Top);
                float distToBottom = MathF.Abs(point.Location.Y - closestNode.Area.Bottom);
                float distToClosestBoundary = MathF.Min(MathF.Min(MathF.Min(distToLeft, distToRight), distToTop), distToBottom);

                TPoint closestPoint = default;
                float distToClosestPoint = float.MaxValue;

                foreach (var pt in closestNode.points)
                    computeDistanceTo(pt);

                // We're closer to the point than to any boundary of this node.
                if (closestNode.parent == null || distToClosestBoundary >= distToClosestPoint)
                {
                    closest = closestPoint;
                    return true;
                }

                // We're closer to the boundary than to any point in this node.
                // We need to check the neighbouring boundaries to see if there's any point closer.
                foreach (var pt in closestNode.parent.enumeratePoints(closestNode))
                    computeDistanceTo(pt);

                closest = closestPoint;
                return true;

                void computeDistanceTo(TPoint pt)
                {
                    float dist = (point.Location - pt.Location).Length;

                    if (dist < distToClosestPoint)
                    {
                        distToClosestPoint = dist;
                        closestPoint = pt;
                    }
                }
            }

            private QuadTreeNode? findContainingNode(TPoint point)
            {
                // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
                if (!Area.Contains(point.Location))
                    return null;

                if (points != null)
                    return this;

                Debug.Assert(topLeft != null);
                Debug.Assert(topRight != null);
                Debug.Assert(bottomLeft != null);
                Debug.Assert(bottomRight != null);

                return topLeft.findContainingNode(point)
                       ?? topRight.findContainingNode(point)
                       ?? bottomLeft.findContainingNode(point)
                       ?? bottomRight.findContainingNode(point);
            }

            public IEnumerable<RectangleF> EnumerateAreas()
            {
                yield return Area;

                if (topLeft != null)
                {
                    foreach (var area in topLeft.EnumerateAreas())
                        yield return area;
                }

                if (topRight != null)
                {
                    foreach (var area in topRight.EnumerateAreas())
                        yield return area;
                }

                if (bottomLeft != null)
                {
                    foreach (var area in bottomLeft.EnumerateAreas())
                        yield return area;
                }

                if (bottomRight != null)
                {
                    foreach (var area in bottomRight.EnumerateAreas())
                        yield return area;
                }
            }

            private IEnumerable<TPoint> enumeratePoints(QuadTreeNode? exceptNode)
            {
                if (points != null)
                {
                    foreach (var pt in points)
                        yield return pt;

                    yield break;
                }

                Debug.Assert(topLeft != null);
                Debug.Assert(topRight != null);
                Debug.Assert(bottomLeft != null);
                Debug.Assert(bottomRight != null);

                if (topLeft != exceptNode)
                {
                    foreach (var pt in topLeft.enumeratePoints(exceptNode))
                        yield return pt;
                }

                if (topRight != exceptNode)
                {
                    foreach (var pt in topRight.enumeratePoints(exceptNode))
                        yield return pt;
                }

                if (bottomLeft != exceptNode)
                {
                    foreach (var pt in bottomLeft.enumeratePoints(exceptNode))
                        yield return pt;
                }

                if (bottomRight != exceptNode)
                {
                    foreach (var pt in bottomRight.enumeratePoints(exceptNode))
                        yield return pt;
                }
            }

            private void subDivide()
            {
                Debug.Assert(points != null);

                topLeft = new QuadTreeNode(this, new RectangleF(Area.Location, Area.Size / 2));
                topRight = new QuadTreeNode(this, new RectangleF(new Vector2(Area.Centre.X, Area.Y), Area.Size / 2));
                bottomLeft = new QuadTreeNode(this, new RectangleF(new Vector2(Area.X, Area.Centre.Y), Area.Size / 2));
                bottomRight = new QuadTreeNode(this, new RectangleF(Area.Centre, Area.Size / 2));

                foreach (var p in points)
                {
                    bool _ = topLeft.Insert(p)
                             || topRight.Insert(p)
                             || bottomLeft.Insert(p)
                             || bottomRight.Insert(p);
                }

                points = null;
            }
        }
    }

    public interface QuadTreePoint
    {
        Vector2 Location { get; }
    }

    public readonly struct QuadTreeVector2Point : QuadTreePoint
    {
        public Vector2 Location { get; }

        public QuadTreeVector2Point(Vector2 location)
        {
            Location = location;
        }

        public static implicit operator QuadTreeVector2Point(Vector2 position) => new QuadTreeVector2Point(position);
        public static implicit operator Vector2(QuadTreeVector2Point point) => point.Location;
    }
}
