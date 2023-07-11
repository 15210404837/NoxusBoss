using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace NoxusBoss.Content.UI
{
    public class UIDialogOptions : UIElement
    {
        private readonly float textScale;

        private readonly Color backgroundColor;

        private readonly Color backgroundHoverColor;

        private readonly Dialog[] dialog;

        private readonly Asset<Texture2D> backgroundTexture;

        internal readonly DynamicSpriteFont font;

        internal readonly Func<bool> visibilityCondition;

        public List<Dialog> ValidDialog => dialog.Where(d => d.CanBeDisplayed).ToList();

        public List<Rectangle> TextBoxes
        {
            get
            {
                if (!visibilityCondition())
                    return new();

                var validDialog = ValidDialog;
                CalculatedStyle dimensions = GetDimensions();
                List<Rectangle> boxes = new();
                for (int i = 0; i < validDialog.Count; i++)
                {
                    Point point = new((int)dimensions.X, (int)(dimensions.Y + dimensions.Height * i));
                    Rectangle box = new(point.X, point.Y, (int)dimensions.Width, (int)dimensions.Height);
                    boxes.Add(box);
                }

                return boxes;
            }
        }

        public UIDialogOptions(Dialog[] dialog, DynamicSpriteFont font, Color baseColor, Color hoverColor, float textScale, Func<bool> visibilityCondition)
        {
            this.textScale = textScale;
            this.dialog = dialog;
            this.font = font;
            this.visibilityCondition = visibilityCondition;
            backgroundTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/UI/DialogOptionBackground");
            backgroundColor = baseColor;
            backgroundHoverColor = hoverColor;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!visibilityCondition())
                return;

            var validDialog = ValidDialog;
            CalculatedStyle dimensions = GetDimensions();

            for (int i = 0; i < validDialog.Count; i++)
            {
                // Draw the background behind everything else.
                Color backgroundColor = MouseScreenRectangle.Intersects(TextBoxes[i]) ? backgroundHoverColor : this.backgroundColor;
                Point point = new((int)dimensions.X, (int)(dimensions.Y + dimensions.Height * i));
                Vector2 backgroundScale = new Vector2(dimensions.Width, dimensions.Height) / backgroundTexture.Value.Size();
                spriteBatch.Draw(backgroundTexture.Value, point.ToVector2(), null, backgroundColor, 0f, Vector2.Zero, backgroundScale, 0, 0f);

                // Draw the dialog options.
                int width = (int)(backgroundScale.X * backgroundTexture.Value.Width);
                string text = validDialog[i].Inquiry;
                string[] wrappedText = WordwrapString(text, font, (int)(width / textScale * 0.96f), 50, out int lineCount);
                for (int j = 0; j < lineCount + 1; j++)
                {
                    string line = wrappedText[j];
                    Vector2 textSize = font.MeasureString(line);
                    Vector2 textDrawPosition = point.ToVector2() + new Vector2(width * textScale * 0.05f, textSize.Y * (j * 0.75f + 0.5f) * textScale);
                    Vector2 textOrigin = Vector2.UnitY * textSize * 0.5f;
                    ChatManager.DrawColorCodedString(spriteBatch, font, line, textDrawPosition, Color.White, 0f, textOrigin, Vector2.One * textScale, -1f);
                }
            }
        }
    }
}
