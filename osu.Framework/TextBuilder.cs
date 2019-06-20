// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osuTK;

namespace osu.Framework
{
    /// <summary>
    /// A text builder for <see cref="SpriteText"/> and other text-based display components.
    /// </summary>
    public class TextBuilder
    {
        /// <summary>
        /// The size of the text area.
        /// </summary>
        public Vector2 TextSize { get; private set; }

        /// <summary>
        /// The characters generated by this <see cref="TextBuilder"/>.
        /// </summary>
        public readonly List<SpriteText.CharacterPart> Characters = new List<SpriteText.CharacterPart>();

        private FontStore.CharacterGlyph lastGlyph => Characters.Count == 0 ? null : Characters[Characters.Count - 1].Glyph;

        private readonly float fontSize;
        private readonly bool useFullGlyphHeight;
        private readonly Vector2 startOffset;
        private readonly Vector2 spacing;
        private readonly float maxWidth;

        private Vector2 currentPos;
        private float currentLineHeight;
        private bool currentNewLine = true;

        /// <summary>
        /// Creates a new <see cref="TextBuilder"/>.
        /// </summary>
        /// <param name="fontSize">The size of the font.</param>
        /// <param name="useFullGlyphHeight">True to use <paramref name="fontSize"/> as the height for each line. False if the height of each individual glyph should be used.</param>
        /// <param name="startOffset">The offset at which characters should begin being added at.</param>
        /// <param name="spacing">The spacing between characters.</param>
        /// <param name="maxWidth">The maximum width of the resulting text bounds.</param>
        public TextBuilder(float fontSize, float maxWidth = float.MaxValue, bool useFullGlyphHeight = true, Vector2 startOffset = default, Vector2 spacing = default)
        {
            this.fontSize = fontSize;
            this.useFullGlyphHeight = useFullGlyphHeight;
            this.startOffset = startOffset;
            this.spacing = spacing;
            this.maxWidth = maxWidth;

            currentPos = startOffset;
        }

        /// <summary>
        /// Whether characters can be added to this <see cref="TextBuilder"/>.
        /// </summary>
        protected virtual bool CanAddCharacters => true;

        /// <summary>
        /// Adds a character to this <see cref="TextBuilder"/>.
        /// </summary>
        /// <param name="glyph">The glyph of the character to add.</param>
        public void AddCharacter(FontStore.CharacterGlyph glyph)
        {
            if (!CanAddCharacters)
                return;

            glyph.ApplyScaleAdjust(fontSize);

            // For each character that is added:
            // 1. Add the kerning to the current position if required.
            // 2. Draw the character at the current position offset by the glyph.
            //    The offset is not applied to the current position, it is only a value to be used at draw-time.
            // 3. Advance the current position by glyph's XAdvance.

            // Kerning is not applied if the user provided a custom width
            float kerning = lastGlyph == null ? 0 : glyph.GetKerning(lastGlyph);

            // Check if there is enough space for the character and let subclasses decide whether to continue adding the character if not
            if (!HasAvailableSpace(kerning + glyph.XAdvance))
            {
                OnWidthExceeded();

                if (!CanAddCharacters)
                    return;
            }

            // The kerning is only added after it is guaranteed that the character will be added, to not leave the current position in a bad state
            currentPos.X += kerning;

            Characters.Add(new SpriteText.CharacterPart
            {
                Glyph = glyph,
                DrawRectangle = new RectangleF(new Vector2(currentPos.X + glyph.XOffset, currentPos.Y + glyph.YOffset),
                    new Vector2(glyph.Width, glyph.Height)),
                OnNewLine = currentNewLine,
            });

            // Move the current position
            currentPos.X += glyph.XAdvance;
            currentLineHeight = Math.Max(currentLineHeight, getGlyphHeight(glyph));
            currentNewLine = false;

            // Calculate the text size
            TextSize = Vector2.ComponentMax(TextSize, currentPos + new Vector2(0, currentLineHeight));
        }

        /// <summary>
        /// Adds a new line to this <see cref="TextBuilder"/>.
        /// </summary>
        public void AddNewLine()
        {
            // Reset + vertically offset the current position
            currentPos.X = startOffset.X;
            currentPos.Y += currentLineHeight + spacing.Y;

            currentLineHeight = 0;
            currentNewLine = true;
        }

        /// <summary>
        /// Removes the last character added to this <see cref="TextBuilder"/>.
        /// </summary>
        public void RemoveLastCharacter()
        {
            if (Characters.Count == 0)
                return;

            SpriteText.CharacterPart currentCharacter = Characters[Characters.Count - 1];
            SpriteText.CharacterPart? lastCharacter = Characters.Count == 1 ? null : (SpriteText.CharacterPart?)Characters[Characters.Count - 2];

            Characters.RemoveAt(Characters.Count - 1);

            // For each character that is removed:
            // 1. Calculate the line height of the last line.
            // 2. If the character is the first on a new line, move the current position upwards by the calculated line height and to the end of the previous line.
            //    The position at the end of the line is the post-XAdvanced position.
            // 3. If the character is not the first on a new line, move the current position backwards by the XAdvance and the kerning from the previous glyph.
            //    This brings the current position to the post-XAdvanced position of the previous glyph.

            currentLineHeight = 0;

            // This is O(n^2) for removing all characters within a line, but is generally not used in such a case
            for (int i = Characters.Count - 1; i >= 0; i--)
            {
                currentLineHeight = Math.Max(currentLineHeight, getGlyphHeight(Characters[i].Glyph));

                if (Characters[i].OnNewLine)
                    break;
            }

            if (currentCharacter.OnNewLine)
            {
                // This character is the first on a new line - move up to the previous line
                currentPos.Y -= currentLineHeight + spacing.Y;
                currentPos.X = 0;

                if (lastCharacter != null)
                {
                    // The character's draw rectangle is the only marker that keeps a constant state for the position, but it has the glyph's XOffset added into it
                    // So the post-kerned position can be retrieved by taking the XOffset away, and the post-XAdvanced position is retrieved by adding the XAdvance back in
                    currentPos.X = lastCharacter.Value.DrawRectangle.Left - lastCharacter.Value.Glyph.XOffset + lastCharacter.Value.Glyph.XAdvance;
                }
            }
            else
            {
                // This character is not the first on a new line - move back within the current line (reversing the operations in AddCharacter())
                currentPos.X -= currentCharacter.Glyph.XAdvance;

                if (lastCharacter != null)
                    currentPos.X -= currentCharacter.Glyph.GetKerning(lastCharacter.Value.Glyph);
            }
        }

        /// <summary>
        /// Invoked when a character is being added that exceeds the maximum width of the text bounds.
        /// </summary>
        /// <remarks>
        /// The character will not continue being added if <see cref="CanAddCharacters"/> is changed during this invocation.
        /// </remarks>
        protected virtual void OnWidthExceeded()
        {
        }

        /// <summary>
        /// Whether there is enough space in the available text bounds.
        /// </summary>
        /// <param name="length">The space requested.</param>
        protected bool HasAvailableSpace(float length) => currentPos.X + length <= maxWidth;

        /// <summary>
        /// Retrieves the height of a glyph.
        /// </summary>
        /// <param name="glyph">The glyph to retrieve the height of.</param>
        /// <returns>The height of the glyph.</returns>
        private float getGlyphHeight(FontStore.CharacterGlyph glyph)
        {
            if (useFullGlyphHeight)
                return fontSize;

            // Space characters typically have heights that exceed the height of all other characters in the font
            // Thus, the height is forced to 0 such that only non-whitespace character heights are considered
            if (glyph.IsWhiteSpace)
                return 0;

            return glyph.YOffset + glyph.Height;
        }
    }
}
