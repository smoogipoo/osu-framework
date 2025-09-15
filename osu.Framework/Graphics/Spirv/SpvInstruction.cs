// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace osu.Framework.Graphics.Spirv
{
    public abstract class SpvInstruction
    {
        public readonly SpvOp OpCode;
        public readonly int WordCount;

        protected SpvInstruction(SpvOp opCode, int wordCount)
        {
            OpCode = opCode;
            WordCount = wordCount;
        }

        internal abstract void ReadFrom(BinaryReader reader);

        internal abstract void WriteTo(BinaryWriter writer);
    }
}
