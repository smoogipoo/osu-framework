// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable StaticMemberInGenericType

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Vertices
{
    /// <summary>
    /// Helper method that provides functionality to enable and bind vertex attributes.
    /// </summary>
    internal static class VertexUtils<T>
        where T : IVertex
    {
        /// <summary>
        /// The stride of the vertex of type <see cref="T"/>.
        /// </summary>
        public static readonly int STRIDE = BlittableValueType.StrideOf(default(T));

        private static readonly List<VertexMemberAttribute> attributes = new List<VertexMemberAttribute>();

        static VertexUtils()
        {
            // Use reflection to retrieve the members attached with a VertexMemberAttribute
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(t => t.IsDefined(typeof(VertexMemberAttribute), true)))
            {
                var attrib = (VertexMemberAttribute)field.GetCustomAttribute(typeof(VertexMemberAttribute));

                // Because this is an un-seen vertex, the attribute locations are unknown, but they're needed for marshalling
                attrib.Offset = Marshal.OffsetOf(typeof(T), field.Name);

                attributes.Add(attrib);
            }
        }

        public static void SetAttributes()
        {
            for (int i = 0; i < attributes.Count; ++i)
            {
                GL.EnableVertexAttribArray(i);
                GL.VertexAttribPointer(i, attributes[i].Count, attributes[i].Type, attributes[i].Normalized, STRIDE, attributes[i].Offset);
            }
        }
    }
}
