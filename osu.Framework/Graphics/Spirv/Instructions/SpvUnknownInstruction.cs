// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace osu.Framework.Graphics.Spirv.Instructions
{
    public class SpvUnknownInstruction : SpvInstruction
    {
        private readonly int[] words;

        public SpvUnknownInstruction(SpvOp opCode, int wordCount)
            : base(opCode, wordCount)
        {
            words = new int[wordCount - 1];
        }

        internal override void ReadFrom(BinaryReader reader)
        {
            for (int i = 0; i < words.Length; i++)
                words[i] = reader.ReadInt32();
        }

        internal override void WriteTo(BinaryWriter writer)
        {
            for (int i = 0; i < words.Length; i++)
                writer.Write(words[i]);
        }
    }
}
