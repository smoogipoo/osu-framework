// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using SDL;

namespace osu.Framework.Graphics.Rendering.SDL3
{
    public static class SDL3Extensions
    {
        public static SDL_GPUPrimitiveType ToSDLPrimitiveType(this PrimitiveTopology topology)
        {
            return topology switch
            {
                PrimitiveTopology.Points => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_POINTLIST,
                PrimitiveTopology.Lines => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST,
                PrimitiveTopology.LineStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINESTRIP,
                PrimitiveTopology.Triangles => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                PrimitiveTopology.TriangleStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP,
                _ => throw new ArgumentOutOfRangeException(nameof(topology), topology, null)
            };
        }

        public static SDL_GPUCompareOp ToSDLCompareOp(this BufferTestFunction function)
        {
            return function switch
            {
                BufferTestFunction.Never => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NEVER,
                BufferTestFunction.LessThan => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS,
                BufferTestFunction.LessThanOrEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
                BufferTestFunction.Equal => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_EQUAL,
                BufferTestFunction.GreaterThanOrEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER_OR_EQUAL,
                BufferTestFunction.GreaterThan => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER,
                BufferTestFunction.NotEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NOT_EQUAL,
                BufferTestFunction.Always => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_ALWAYS,
                _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
            };
        }

        public static SDL_GPUStencilOp ToSDLStencilOp(this StencilOperation operation)
        {
            return operation switch
            {
                StencilOperation.Zero => SDL_GPUStencilOp.SDL_GPU_STENCILOP_ZERO,
                StencilOperation.Invert => SDL_GPUStencilOp.SDL_GPU_STENCILOP_INVERT,
                StencilOperation.Keep => SDL_GPUStencilOp.SDL_GPU_STENCILOP_KEEP,
                StencilOperation.Replace => SDL_GPUStencilOp.SDL_GPU_STENCILOP_REPLACE,
                StencilOperation.Increase => SDL_GPUStencilOp.SDL_GPU_STENCILOP_INCREMENT_AND_CLAMP,
                StencilOperation.Decrease => SDL_GPUStencilOp.SDL_GPU_STENCILOP_DECREMENT_AND_CLAMP,
                StencilOperation.IncreaseWrap => SDL_GPUStencilOp.SDL_GPU_STENCILOP_INCREMENT_AND_WRAP,
                StencilOperation.DecreaseWrap => SDL_GPUStencilOp.SDL_GPU_STENCILOP_DECREMENT_AND_WRAP,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }
    }
}
