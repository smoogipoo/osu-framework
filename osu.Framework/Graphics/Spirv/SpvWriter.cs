// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;

namespace osu.Framework.Graphics.Spirv
{
    public class SpvWriter : IDisposable
    {
        private readonly BinaryWriter writer;

        public SpvWriter(Stream stream, SpvHeader header, bool leaveOpen = false)
        {
            writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen);
            writer.Write(SpvHeader.SPIRV_HEADER_MAGIC);
            writer.Write((header.Version.Major << 16) | header.Version.Minor);
            writer.Write(header.Generator);
            writer.Write(header.IdBounds);
            writer.Write(header.Schema);
        }

        public void Write(SpvInstruction instruction)
        {
            writer.Write((instruction.WordCount << 16) | (int)instruction.OpCode);
            instruction.WriteTo(writer);
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}
