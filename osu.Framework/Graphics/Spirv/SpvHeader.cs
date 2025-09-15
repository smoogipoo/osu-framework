// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;

namespace osu.Framework.Graphics.Spirv
{
    public class SpvHeader
    {
        public const int SPIRV_HEADER_MAGIC = 0x07230203;

        public Version Version = new Version(1, 0);
        public int Generator;
        public int IdBounds;
        public int Schema;

        internal SpvHeader(BinaryReader reader)
        {
            int magic = reader.ReadInt32();
            if (magic != SPIRV_HEADER_MAGIC)
                throw new Exception("Not a SPIR-V binary file.");

            int versionWord = reader.ReadInt32();
            Version = new Version((versionWord >> 16) & 0xFF, (versionWord >> 8) & 0xFF);

            Generator = reader.ReadInt32();
            IdBounds = reader.ReadInt32();
            Schema = reader.ReadInt32();
        }

        public SpvHeader()
        {
        }
    }
}
