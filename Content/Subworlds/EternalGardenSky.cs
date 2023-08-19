using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Subworlds
{
    public class EternalGardenSky : CustomSky
    {
        private bool skyActive;

        private float opacity;

        public override void Deactivate(params object[] args)
        {
            skyActive = false;
        }

        public override void Reset()
        {
            skyActive = false;
        }

        public override bool IsActive()
        {
            return skyActive || opacity > 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            skyActive = true;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (Main.gameMenu)
                return;

            // Disable ambient sky objects like wyverns and eyes appearing in the background.
            if (skyActive)
                SkyManager.Instance["Ambience"].Deactivate();

            // The speed of the parallax. The closer the layer to the player the faster it will be.
            float screenParallaxSpeed = 0.2f;

            int frameOffset = (int)(Main.GameUpdateCount / 10U) % 4;
            Texture2D backgroundTexture = ModContent.Request<Texture2D>($"Terraria/Images/Background_{frameOffset + 251}").Value;
            Texture2D waterTexture = ModContent.Request<Texture2D>($"NoxusBoss/Content/Backgrounds/GardenLake{frameOffset + 1}").Value;

            // Apply parallax effects.
            int x = (int)((Main.screenPosition.X + 1500f) * screenParallaxSpeed) % waterTexture.Width;
            int y = (int)(Main.screenPosition.Y * screenParallaxSpeed * 0.5f);

            // Loop the background horizontally.
            for (int i = -2; i <= 2; i++)
            {
                // Draw the base background.
                Vector2 layerPosition = new(Main.screenWidth * 0.5f - x + waterTexture.Width * i, Main.screenHeight - y + screenParallaxSpeed * 100f);
                spriteBatch.Draw(backgroundTexture, layerPosition - backgroundTexture.Size() * 0.5f, null, new(22, 22, 22), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

                // Draw the brightened water.
                spriteBatch.Draw(waterTexture, layerPosition - waterTexture.Size() * 0.5f, null, Color.SkyBlue * opacity * 0.24f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame || Main.gameMenu)
                skyActive = false;

            if (skyActive && opacity < 1f)
                opacity += 0.02f;
            else if (!skyActive && opacity > 0f)
                opacity -= 0.02f;
        }

        public override float GetCloudAlpha() => 0f;
    }
}
