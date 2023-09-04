using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Fixes
{
    public class EternalGardenIntroBackgroundFix : ModSystem
    {
        public static bool ShouldDrawWhite
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On_Main.DrawMenu += DrawWhite;

            // Ensure that the MagicPixel texture is loaded when the mod is.
            if (Main.netMode != NetmodeID.Server)
                _ = TextureAssets.MagicPixel.Value;
        }

        // The reason this is necessary is because sometimes the custom drawing for the subworld entering has a one frame hiccup, and awkwardly draws the
        // regular background for some reason.
        private void DrawWhite(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);
            if (ShouldDrawWhite)
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Vector2 screenArea = new(Main.instance.GraphicsDevice.DisplayMode.Width, Main.instance.GraphicsDevice.DisplayMode.Width);
                Vector2 scale = screenArea / pixel.Size();
                Main.spriteBatch.Draw(pixel, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, scale, 0, 0f);
            }
        }
    }
}
