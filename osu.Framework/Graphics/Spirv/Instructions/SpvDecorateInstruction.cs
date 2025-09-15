// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace osu.Framework.Graphics.Spirv.Instructions
{
    public class SpvDecorateInstruction : SpvInstruction
    {
        public int Target { get; private set; }
        public SpvDecoration Decoration { get; private set; }
        public int[] ExtraOperands { get; private set; }

        public SpvDecorateInstruction(int wordCount)
            : base(SpvOp.SpvOpDecorate, wordCount)
        {
            ExtraOperands = new int[wordCount - 3];
        }

        internal override void ReadFrom(BinaryReader reader)
        {
            Target = reader.ReadInt32();
            Decoration = (SpvDecoration)reader.ReadInt32();
            for (int i = 0; i < ExtraOperands.Length; i++)
                ExtraOperands[i] = reader.ReadInt32();
        }

        internal override void WriteTo(BinaryWriter writer)
        {
            writer.Write(Target);
            writer.Write((int)Decoration);
            for (int i = 0; i < ExtraOperands.Length; i++)
                writer.Write(ExtraOperands[i]);
        }
    }
}
