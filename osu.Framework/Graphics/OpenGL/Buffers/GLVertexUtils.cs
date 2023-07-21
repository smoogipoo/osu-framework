// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    /// <summary>
    /// Helper method that provides functionality to enable and bind GL vertex attributes.
    /// </summary>
    internal static class GLVertexUtils<T>
        where T : unmanaged, IVertex
    {
        /// <summary>
        /// The stride of the vertex of type <typeparamref name="T"/>.
        /// </summary>
        public static readonly int STRIDE = Marshal.SizeOf(default(T));

        // ReSharper disable StaticMemberInGenericType
        private static readonly List<VertexMemberAttribute> attributes = new List<VertexMemberAttribute>();

        static GLVertexUtils()
        {
            addAttributesRecursive(typeof(T), 0);
        }

        private static void addAttributesRecursive(Type type, int currentOffset)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                int fieldOffset = currentOffset + Marshal.OffsetOf(type, field.Name).ToInt32();

                if (typeof(IVertex).IsAssignableFrom(field.FieldType))
                {
                    // Vertices may contain others, but the attributes of contained vertices belong to the parent when marshalled, so they are recursively added for their parent
                    // Their field offsets must be adjusted to reflect the position of the child attribute in the parent vertex
                    addAttributesRecursive(field.FieldType, fieldOffset);
                }
                else if (field.IsDefined(typeof(VertexMemberAttribute), true))
                {
                    var attrib = (VertexMemberAttribute?)field.GetCustomAttribute(typeof(VertexMemberAttribute));
                    Debug.Assert(attrib != null);

                    // Because this is an un-seen vertex, the attribute locations are unknown, but they're needed for marshalling
                    attrib.Offset = new IntPtr(fieldOffset);

                    attributes.Add(attrib);
                }
            }
        }

        public static void SetAttributes()
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                GL.EnableVertexAttribArray(i);

                switch (attributes[i].Type)
                {
                    case VertexAttribPointerType.Int:
                        GL.VertexAttribIPointer(i, attributes[i].Count, VertexAttribIntegerType.Int, STRIDE, attributes[i].Offset);
                        break;

                    case VertexAttribPointerType.UnsignedInt:
                        GL.VertexAttribIPointer(i, attributes[i].Count, VertexAttribIntegerType.UnsignedInt, STRIDE, attributes[i].Offset);
                        break;

                    case VertexAttribPointerType.Byte:
                        GL.VertexAttribIPointer(i, attributes[i].Count, VertexAttribIntegerType.Byte, STRIDE, attributes[i].Offset);
                        break;

                    case VertexAttribPointerType.UnsignedByte:
                        GL.VertexAttribIPointer(i, attributes[i].Count, VertexAttribIntegerType.UnsignedByte, STRIDE, attributes[i].Offset);
                        break;

                    case VertexAttribPointerType.Short:
                        GL.VertexAttribIPointer(i, attributes[i].Count, VertexAttribIntegerType.Short, STRIDE, attributes[i].Offset);
                        break;

                    case VertexAttribPointerType.UnsignedShort:
                        GL.VertexAttribIPointer(i, attributes[i].Count, VertexAttribIntegerType.UnsignedShort, STRIDE, attributes[i].Offset);
                        break;

                    default:
                        GL.VertexAttribPointer(i, attributes[i].Count, attributes[i].Type, attributes[i].Normalized, STRIDE, attributes[i].Offset);
                        break;
                }
            }
        }
    }
}
