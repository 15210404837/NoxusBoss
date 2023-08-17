using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class TotalWhiteOverlaySystem : ModSystem
    {
        public static float WhiteInterpolant
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawWhite;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawWhite;
        }

        public override void PreUpdateEntities() => WhiteInterpolant = Clamp(WhiteInterpolant - 0.075f, 0f, 1f);

        private void DrawWhite(GameTime obj)
        {
            if (WhiteInterpolant <= 0f || Main.gameMenu)
                return;

            Main.spriteBatch.Begin();

            // Draw a pure-white background.
            Texture2D pixel = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Pixel").Value;
            Vector2 pixelScale = new Vector2(Main.screenWidth, Main.screenHeight) * 2f / pixel.Size();
            Main.spriteBatch.Draw(pixel, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, null, Color.White * WhiteInterpolant, 0f, pixel.Size() * 0.5f, pixelScale, 0, 0f);

            Main.spriteBatch.End();
        }
    }
}
