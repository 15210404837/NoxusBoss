using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class XerocStarFleeEffectSystem : ModSystem
    {
        public override void OnModLoad()
        {
            On.Terraria.Main.DrawStarsInBackground += MakeStarsFleeWrapper;
        }

        private void MakeStarsFleeWrapper(On.Terraria.Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea)
        {
            if (Main.gameMenu || Main.dayTime)
                orig(self, sceneArea);
            else
                MakeStarsFlee(sceneArea);
        }

        public static void MakeStarsFlee(Main.SceneArea sceneArea)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Don't draw stars if the camera is underground.
            if (Main.screenPosition.Y >= Main.worldSurface * 16.0 + 16.0)
                return;

            float graveyardOpacityFactor = 1f;
            if (Main.GraveyardVisualIntensity > 0f)
            {
                graveyardOpacityFactor = 1f - Main.GraveyardVisualIntensity * 1.4f;
                if (graveyardOpacityFactor <= 0f)
                    return;
            }

            // Don't drwa stars if the background is too bright.
            Color colorOfTheSkies = Main.ColorOfTheSkies;
            if (255f * (1f - Main.cloudAlpha * Main.atmo) - colorOfTheSkies.R - 25f <= 0f)
                return;

            for (int i = 0; i < Main.numStars; i++)
            {
                Star star = Main.star[i];

                // Don't attempt to draw null or hidden stars.
                if (star is null || star.hidden)
                    continue;

                float starFadeOpacityFactor = 1f - star.fadeIn;
                int r = (int)((byte.MaxValue - colorOfTheSkies.R - 100) * star.twinkle * starFadeOpacityFactor);
                int g = (int)((byte.MaxValue - colorOfTheSkies.G - 100) * star.twinkle * starFadeOpacityFactor);
                int b = (int)((byte.MaxValue - colorOfTheSkies.B - 100) * star.twinkle * starFadeOpacityFactor);
                r = (r + b + g) / 3;

                // Don't bother drawing if the luminence of the color is zero.
                if (r <= 0)
                    continue;

                r = (int)(r * 1.4);
                if (r > 255)
                    r = 255;

                g = r;
                b = r;

                // Calculate draw values for the given star.
                Texture2D starTexture = TextureAssets.Star[star.type].Value;
                Color color = new Color((byte)r, (byte)g, (byte)b) * graveyardOpacityFactor;
                Vector2 starDrawPosition = new Vector2(star.position.X / 1920f, star.position.Y / 1200f) * new Vector2(sceneArea.totalWidth, sceneArea.totalHeight) + new Vector2(0f, sceneArea.bgTopY) + sceneArea.SceneLocalScreenPositionOffset;
                Vector2 origin = starTexture.Size() * 0.5f;
                Vector2 directionAwayFromCenter = (starDrawPosition - new Vector2(sceneArea.totalWidth, sceneArea.totalHeight) * 0.5f).SafeNormalize(Vector2.UnitY);

                // Make stars recede away if necessary.
                starDrawPosition += directionAwayFromCenter * Pow(XerocSky.StarRecedeInterpolant, 3.7f) * 2000f;

                // Draw trails for falling stars.
                if (star.falling || XerocSky.StarRecedeInterpolant > 0f)
                {
                    star.fadeIn = 0f;

                    Vector2 offsetDirection = star.fallSpeed;
                    double afterimageCount = XerocSky.StarRecedeInterpolant > 0f ? 25 : star.fallTime;
                    if (afterimageCount > 30)
                        afterimageCount = 30;
                    if (XerocSky.StarRecedeInterpolant > 0f)
                        offsetDirection = directionAwayFromCenter * XerocSky.StarRecedeInterpolant * 10f;

                    for (int j = 1; j < afterimageCount; j++)
                    {
                        Vector2 afterimageOffset = -offsetDirection * j * 0.4f;
                        float afterimageScale = star.scale * (1f - j / 30f) * star.twinkle * Main.ForcedMinimumZoom;
                        Main.spriteBatch.Draw(starTexture, starDrawPosition + afterimageOffset, null, color * (1f - j / 30f), star.rotation, origin, afterimageScale, SpriteEffects.None, 0f);
                    }
                }

                // Draw the star.
                Main.spriteBatch.Draw(starTexture, starDrawPosition, null, color, star.rotation, origin, star.scale * star.twinkle * Main.ForcedMinimumZoom, SpriteEffects.None, 0f);
            }
        }
    }
}
