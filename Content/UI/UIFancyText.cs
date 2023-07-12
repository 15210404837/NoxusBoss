using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CalamityMod.Items.Potions.Alcohol;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.UI;
using Terraria.UI.Chat;

namespace NoxusBoss.Content.UI
{
    public class UIFancyText : UIElement
    {
        internal struct TextPart
        {
            private bool changedByRegex;

            public string Text;

            public bool Italics;

            public int LineIndex;

            public float TextScale;

            public float HorizontalSpaceUsed;

            public Color TextColor;

            public DynamicSpriteFont Font;

            public static readonly Regex ItalicsEmphasis = new(@"\*\*[0-9a-zA-Z]+\*\*", RegexOptions.Compiled);

            public static readonly Regex ColorHexSpecifier = new(@"\[c\/([0-9a-fA-F]{6})\:(.*)\]", RegexOptions.Compiled);

            public TextPart(string text, int lineIndex, bool italics, float textScale, DynamicSpriteFont font, Color color, float alreadyUsedHorixontalSpace = 0f)
            {
                Text = text;
                LineIndex = lineIndex;
                Italics = italics;
                Font = font;
                TextScale = textScale;
                TextColor = color;
                HorizontalSpaceUsed = alreadyUsedHorixontalSpace + Font.MeasureString(Text).X * TextScale;
            }

            public static void SplitByRegex(List<TextPart> lines, Regex regex, float textScale, DynamicSpriteFont font, Func<Match, TextPart, TextPart> matchAction)
            {
                // Search for instances of a given pattern as an indicator for for change.
                for (int i = 0; i < lines.Count; i++)
                {
                    // Verify that there are any instances of the above pattern. If there are, split the line into three parts:
                    // 1. The left side.
                    // 2. The center, with the italics in use.
                    // 3. The right side.
                    // This also involves removing the original line.
                    if (regex.IsMatch(lines[i].Text) && !lines[i].changedByRegex)
                    {
                        // Remove the old, soon to be split line.
                        int lineIndex = lines[i].LineIndex;
                        string wholeLine = lines[i].Text;
                        Color originalColor = lines[i].TextColor;
                        lines.RemoveAt(i);

                        // Acquire the matched instance. If there are more than one successive loop instances will catch it.
                        var match = regex.Match(wholeLine);
                        string textThatUsesPattern = match.Value;

                        // Add the separated text to the list of lines.
                        TextPart left = new TextPart(wholeLine.Split(textThatUsesPattern).First(), lineIndex, false, textScale, font, originalColor) with { changedByRegex = true };
                        TextPart center = new TextPart(textThatUsesPattern, lineIndex, false, textScale, font, originalColor) with { changedByRegex = true };
                        TextPart right = new TextPart(wholeLine.Split(textThatUsesPattern)[1], lineIndex, false, textScale, font, originalColor) with { changedByRegex = true };

                        lines.Insert(i, right);
                        lines.Insert(i, matchAction(match, center));
                        lines.Insert(i, left);

                        // Go back to the start of the loop due to the fact that the line count is going to inevitably be altered.
                        i = 0;
                    }
                }

                // Reset the changed by regex attribute of the text.
                for (int i = 0; i < lines.Count; i++)
                {
                    lines[i] = lines[i] with { changedByRegex = false };
                }
            }

            public static TextPart[] SplitRawText(string text, float textScale, DynamicSpriteFont font, Color textColor)
            {
                // Firstly separate the base text by newlines.
                List<TextPart> lines = text.Split('\n').Select((t, index) => new TextPart(t, index, false, textScale, font, textColor)).ToList();

                // Search for instances of a [c/Hex:Text] pattern as an indicator for color overrides.
                SplitByRegex(lines, ColorHexSpecifier, textScale, font, (match, line) =>
                {
                    Color lineColor = line.TextColor;

                    // Define the text color and replace the text such that only the inside of the formatting is displayed.
                    int colorHex = Convert.ToInt32(match.Groups[1].Value, 16);
                    return line with
                    {
                        TextColor = new(colorHex >> 16 & 255, colorHex >> 8 & 255, colorHex & 255),
                        Text = match.Groups[2].Value
                    };
                });

                // Search for instances of a **Text** pattern as an indicator for italics.
                SplitByRegex(lines, ItalicsEmphasis, textScale, font, (_, line) =>
                {
                    line.Italics = true;
                    line.Text = line.Text.Replace("**", string.Empty);
                    return line;
                });

                return lines.ToArray();
            }
        }

        private float textScale = 1f;

        private Vector2 textSize = Vector2.Zero;

        private Color color = Color.White;

        private Color shadowColor = new(44, 44, 44);

        private string visibleText;

        private string lastTextReference;

        private readonly DynamicSpriteFont font;

        private readonly DynamicSpriteFont fontItalics;

        public bool DynamicallyScaleDownToWidth;

        public float TextOriginX
        {
            get;
            set;
        }

        public float TextOriginY
        {
            get;
            set;
        }

        public float WrappedTextBottomPadding
        {
            get;
            set;
        }

        public string Text
        {
            get;
            private set;
        } = string.Empty;

        public UIFancyText(string text, DynamicSpriteFont font, Color textColor, float textScale = 1f)
        {
            TextOriginX = 0.5f;
            TextOriginY = 0f;
            color = textColor;
            this.font = font;
            WrappedTextBottomPadding = 20f;
            InternalSetText(text, textScale);
        }

        public UIFancyText(string text, DynamicSpriteFont font, DynamicSpriteFont fontItalics, Color textColor, float textScale = 1f) : this(text, font, textColor, textScale)
        {
            this.fontItalics = fontItalics;
        }

        public override void Recalculate()
        {
            InternalSetText(Text, textScale);
            base.Recalculate();
        }

        public void SetText(string text)
        {
            InternalSetText(text, textScale);
        }

        public void SetText(string text, float textScale)
        {
            InternalSetText(text, textScale);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            VerifyTextState();
            CalculatedStyle innerDimensions = GetInnerDimensions();
            Vector2 position = innerDimensions.Position() - Vector2.UnitY * textScale * 2f;

            // Do a bunch of offset and scaling math.
            position.X += (innerDimensions.Width - textSize.X) * TextOriginX;
            position.Y += (innerDimensions.Height - textSize.Y) * TextOriginY;
            float scale = textScale;
            if (DynamicallyScaleDownToWidth && textSize.X > innerDimensions.Width)
                scale *= innerDimensions.Width / textSize.X;

            Color baseColor = shadowColor * (color.A / 255f);
            Vector2 origin = Vector2.Zero;
            Vector2 baseScale = new(scale);

            // Split the text into parts and draw them individually.
            // This is necessary because certain things such as emphasis or color variance have to be drawn separately from the rest of the line.
            var splitText = TextPart.SplitRawText(visibleText, scale, font, color);
            int totalLines = splitText.Max(t => t.LineIndex);
            for (int i = 0; i < totalLines + 1; i++)
            {
                var linesForText = splitText.Where(t => t.LineIndex == i).ToList();

                // Draw the line parts.
                int partIndex = 0;
                float horizontalOffset = 0f;
                foreach (TextPart line in linesForText)
                {
                    Vector2 currentPosition = position + new Vector2(horizontalOffset, i * scale * 48f);
                    var font = line.Italics ? (fontItalics ?? this.font) : this.font;

                    ChatManager.DrawColorCodedStringShadow(spriteBatch, font, line.Text, currentPosition, baseColor, 0f, origin, baseScale, -1f, 0.2f);
                    ChatManager.DrawColorCodedString(spriteBatch, font, line.Text, currentPosition, line.TextColor, 0f, origin, baseScale, -1f);
                    partIndex++;
                    horizontalOffset += font.MeasureString(line.Text).X * line.TextScale;
                }
            }
        }

        private void VerifyTextState()
        {
            if (lastTextReference == Text)
                return;

            InternalSetText(Text, textScale);
        }

        private void InternalSetText(string text, float textScale)
        {
            Text = text;
            this.textScale = textScale;
            lastTextReference = text.ToString();

            float width = Parent?.Width.Pixels ?? 100f;
            visibleText = font.CreateWrappedText(lastTextReference, width / textScale * 0.8f);

            Vector2 textSize = font.MeasureString(visibleText);
            Vector2 clampTextSize = new Vector2(textSize.X, textSize.Y + WrappedTextBottomPadding) * textScale;

            this.textSize = clampTextSize;
            MinWidth.Set(clampTextSize.X + PaddingLeft + PaddingRight, 0f);
            MinHeight.Set(clampTextSize.Y + PaddingTop + PaddingBottom, 0f);
        }
    }
}
