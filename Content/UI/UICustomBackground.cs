using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.UI;

namespace NoxusBoss.Content.UI
{
    public class UICustomBackground : UIElement
    {
        private readonly Color backgroundColor;

        private readonly Asset<Texture2D> backgroundTexture;

        public UICustomBackground(Asset<Texture2D> texture, Color color)
        {
            backgroundTexture = texture;
            backgroundColor = color;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            Point point = new((int)dimensions.X, (int)dimensions.Y);
            spriteBatch.Draw(backgroundTexture.Value, new Rectangle(point.X, point.Y, backgroundTexture.Width(), backgroundTexture.Height()), new Rectangle?(new Rectangle(0, 0, backgroundTexture.Width(), backgroundTexture.Height())), backgroundColor);
        }
    }
}
