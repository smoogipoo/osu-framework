// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Configuration
{
    public enum RendererType
    {
        [Description("Automatic")]
        Automatic,

        [Description("Metal")]
        Metal,

        [Description("Vulkan")]
        Vulkan,

        [Description("Direct3D 11")]
        Direct3D11,

        [Description("OpenGL")]
        OpenGL,

        [Description("OpenGL (Legacy)")]
        OpenGLLegacy,

        [Description("Deferred (Metal)")]
        Deferred_Metal,

        [Description("Deferred (Vulkan)")]
        Deferred_Vulkan,

        [Description("Deferred (Direct3D 11)")]
        Deferred_Direct3D11,

        [Description("Deferred (OpenGL)")]
        Deferred_OpenGL
    }
}
