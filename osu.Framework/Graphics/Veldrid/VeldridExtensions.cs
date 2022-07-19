// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    public static class VeldridExtensions
    {
        public static RgbaFloat ToRgbaFloat(this Color4 colour) => new RgbaFloat(colour.R, colour.G, colour.B, colour.A);

        // todo: ColorWriteMask is necessary for front-to-back render support.
        // public static BlendAttachmentDescription ToBlendAttachment(this BlendingParameters parameters, ColorWriteMask writeMask = ColorWriteMask.All) => new BlendAttachmentDescription
        public static BlendAttachmentDescription ToBlendAttachment(this BlendingParameters parameters) => new BlendAttachmentDescription
        {
            BlendEnabled = !parameters.IsDisabled,
            SourceColorFactor = parameters.Source.ToBlendFactor(),
            SourceAlphaFactor = parameters.SourceAlpha.ToBlendFactor(),
            DestinationColorFactor = parameters.Destination.ToBlendFactor(),
            DestinationAlphaFactor = parameters.DestinationAlpha.ToBlendFactor(),
            ColorFunction = parameters.RGBEquation.ToBlendFunction(),
            AlphaFunction = parameters.AlphaEquation.ToBlendFunction(),
            // ColorWriteMask = writeMask,
        };

        public static BlendFactor ToBlendFactor(this BlendingType type)
        {
            switch (type)
            {
                case BlendingType.DstAlpha:
                    return BlendFactor.DestinationAlpha;

                case BlendingType.DstColor:
                    return BlendFactor.DestinationColor;

                case BlendingType.SrcAlpha:
                    return BlendFactor.SourceAlpha;

                case BlendingType.SrcColor:
                    return BlendFactor.SourceColor;

                case BlendingType.OneMinusDstAlpha:
                    return BlendFactor.InverseDestinationAlpha;

                case BlendingType.OneMinusDstColor:
                    return BlendFactor.InverseDestinationColor;

                case BlendingType.OneMinusSrcAlpha:
                    return BlendFactor.InverseSourceAlpha;

                case BlendingType.OneMinusSrcColor:
                    return BlendFactor.InverseSourceColor;

                case BlendingType.One:
                    return BlendFactor.One;

                case BlendingType.Zero:
                    return BlendFactor.Zero;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static BlendFunction ToBlendFunction(this BlendingEquation equation)
        {
            switch (equation)
            {
                case BlendingEquation.Add:
                    return BlendFunction.Add;

                case BlendingEquation.Subtract:
                    return BlendFunction.Subtract;

                case BlendingEquation.ReverseSubtract:
                    return BlendFunction.ReverseSubtract;

                case BlendingEquation.Min:
                    return BlendFunction.Minimum;

                case BlendingEquation.Max:
                    return BlendFunction.Maximum;

                default:
                    throw new ArgumentOutOfRangeException(nameof(equation));
            }
        }

        public static SamplerFilter ToSamplerFilter(this All mode)
        {
            switch (mode)
            {
                case All.Linear:
                    return SamplerFilter.MinLinear_MagLinear_MipLinear;

                case All.Nearest:
                    return SamplerFilter.MinPoint_MagPoint_MipPoint;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static ComparisonKind ToComparisonKind(this DepthFunction function)
        {
            switch (function)
            {
                case DepthFunction.Always:
                    return ComparisonKind.Always;

                case DepthFunction.Never:
                    return ComparisonKind.Never;

                case DepthFunction.Less:
                    return ComparisonKind.Less;

                case DepthFunction.Equal:
                    return ComparisonKind.Equal;

                case DepthFunction.Lequal:
                    return ComparisonKind.LessEqual;

                case DepthFunction.Greater:
                    return ComparisonKind.Greater;

                case DepthFunction.Notequal:
                    return ComparisonKind.NotEqual;

                case DepthFunction.Gequal:
                    return ComparisonKind.GreaterEqual;

                default:
                    throw new ArgumentOutOfRangeException(nameof(function));
            }
        }

        public static VertexElementFormat ToVertexElementFormat(this VertexAttribPointerType type, int count)
        {
            switch (type)
            {
                case VertexAttribPointerType.Byte when count == 2:
                    return VertexElementFormat.SByte2;

                case VertexAttribPointerType.Byte when count == 4:
                    return VertexElementFormat.SByte4;

                case VertexAttribPointerType.UnsignedByte when count == 2:
                    return VertexElementFormat.Byte2;

                case VertexAttribPointerType.UnsignedByte when count == 4:
                    return VertexElementFormat.Byte4;

                case VertexAttribPointerType.Short when count == 2:
                    return VertexElementFormat.Short2;

                case VertexAttribPointerType.Short when count == 4:
                    return VertexElementFormat.Short4;

                case VertexAttribPointerType.UnsignedShort when count == 2:
                    return VertexElementFormat.UShort2;

                case VertexAttribPointerType.UnsignedShort when count == 4:
                    return VertexElementFormat.UShort4;

                case VertexAttribPointerType.Int when count == 1:
                    return VertexElementFormat.Int1;

                case VertexAttribPointerType.Int when count == 2:
                    return VertexElementFormat.Int2;

                case VertexAttribPointerType.Int when count == 3:
                    return VertexElementFormat.Int3;

                case VertexAttribPointerType.Int when count == 4:
                    return VertexElementFormat.Int4;

                case VertexAttribPointerType.UnsignedInt when count == 1:
                    return VertexElementFormat.UInt1;

                case VertexAttribPointerType.UnsignedInt when count == 2:
                    return VertexElementFormat.UInt2;

                case VertexAttribPointerType.UnsignedInt when count == 3:
                    return VertexElementFormat.UInt3;

                case VertexAttribPointerType.UnsignedInt when count == 4:
                    return VertexElementFormat.UInt4;

                case VertexAttribPointerType.Float when count == 1:
                    return VertexElementFormat.Float1;

                case VertexAttribPointerType.Float when count == 2:
                    return VertexElementFormat.Float2;

                case VertexAttribPointerType.Float when count == 3:
                    return VertexElementFormat.Float3;

                case VertexAttribPointerType.Float when count == 4:
                    return VertexElementFormat.Float4;

                case VertexAttribPointerType.HalfFloat when count == 1:
                    return VertexElementFormat.Half1;

                case VertexAttribPointerType.HalfFloat when count == 2:
                    return VertexElementFormat.Half2;

                case VertexAttribPointerType.HalfFloat when count == 4:
                    return VertexElementFormat.Half4;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static PrimitiveTopology ToPrimitiveTopology(this PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Points:
                    return PrimitiveTopology.PointList;

                case PrimitiveType.Lines:
                    return PrimitiveTopology.LineList;

                case PrimitiveType.LineStrip:
                    return PrimitiveTopology.LineStrip;

                case PrimitiveType.Triangles:
                    return PrimitiveTopology.TriangleList;

                case PrimitiveType.TriangleStrip:
                    return PrimitiveTopology.TriangleStrip;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
