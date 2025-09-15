// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;
using osu.Framework.Graphics.Spirv.Instructions;

namespace osu.Framework.Graphics.Spirv
{
    public class SpvReader : IDisposable
    {
        public readonly SpvHeader Header;

        private readonly BinaryReader reader;

        public SpvReader(Stream stream, bool leaveOpen = false)
        {
            reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
            Header = new SpvHeader(reader);
        }

        public bool TryReadInstruction(out SpvInstruction instruction)
        {
            if (reader.PeekChar() == -1)
            {
                instruction = new SpvUnknownInstruction(SpvOp.SpvOpNop, 1);
                return false;
            }

            int header = reader.ReadInt32();
            int wordCount = header >> 16;
            SpvOp opCode = (SpvOp)(header & 0xFFFF);

            instruction = opCode switch
            {
                SpvOp.SpvOpDecorate => new SpvDecorateInstruction(wordCount),
                _ => new SpvUnknownInstruction(opCode, wordCount)
            };

            instruction.ReadFrom(reader);
            return true;
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
