using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Fixes;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class TotalWhiteOverlaySystem : ModSystem
    {
        public static int TimeSinceMonologueBegan
        {
            get;
            set;
        }

        public static int TimeSinceWorldgenFinished
        {
            get;
            set;
        }

        public static float WhiteInterpolant
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawWhite;
            On_SoundEngine.PlaySound_int_int_int_int_float_float += DisableSoundsDuringMonologue;
        }

        private SoundEffectInstance DisableSoundsDuringMonologue(On_SoundEngine.orig_PlaySound_int_int_int_int_float_float orig, int type, int x, int y, int Style, float volumeScale, float pitchOffset)
        {
            if (Main.gameMenu || TimeSinceMonologueBegan <= 60)
                return orig(type, x, y, Style, volumeScale, pitchOffset);

            return null;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawWhite;
        }

        public override void PreUpdateEntities()
        {
            WhiteInterpolant = Clamp(WhiteInterpolant - 0.075f, 0f, 1f);

            // Edge case: If the world is being regenerated due to the Purifier, keep everything white.
            if (WorldGen.generatingWorld)
                WhiteInterpolant = 1f;
        }

        private void DrawWhite(GameTime obj)
        {
            if (TimeSinceWorldgenFinished >= 1)
                WorldGen.generatingWorld = true;

            if (WhiteInterpolant <= 0f || (Main.gameMenu && !EternalGardenIntroBackgroundFix.ShouldDrawWhite))
            {
                TimeSinceMonologueBegan = 0;
                return;
            }

            Main.spriteBatch.Begin();

            // Draw a pure-white background.
            Texture2D pixel = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Pixel").Value;
            Vector2 pixelScale = new Vector2(Main.screenWidth, Main.screenHeight) * 2f / pixel.Size();
            Main.spriteBatch.Draw(pixel, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, null, Color.White * WhiteInterpolant, 0f, pixel.Size() * 0.5f, pixelScale, 0, 0f);

            // Draw a silly monologue if the world is being regenerated due to the Purifier.
            if (WorldGen.generatingWorld)
                DrawWorldgenMonologue();
            else
                TimeSinceMonologueBegan = 0;

            Main.spriteBatch.End();
        }

        private void DrawWorldgenMonologue()
        {
            // Increment the monologue timer.
            TimeSinceMonologueBegan++;

            // Increment the post-worldgen timer if it's been activated.
            if (TimeSinceWorldgenFinished >= 1)
                TimeSinceWorldgenFinished++;
            if (TimeSinceWorldgenFinished >= 240)
            {
                WorldGen.generatingWorld = false;
                WorldGen.SaveAndQuit();
                WhiteInterpolant = 0f;
                TimeSinceWorldgenFinished = 0;
            }

            // Draw credits on the bottom right of the screen.
            var font = FontAssets.MouseText.Value;
            int textLineCounter = 0;
            float creditTextScale = 1.2f;
            string creditText = Language.GetTextValue($"Mods.{Mod.Name}.Dialog.CelesteMusicCreditText");
            Color textColor = Color.Black * GetLerpValue(210f, 450f, TimeSinceMonologueBegan, true) * GetLerpValue(210f, 60f, TimeSinceWorldgenFinished, true);
            foreach (string creditLine in creditText.Split('\n'))
            {
                Vector2 creditTextSize = font.MeasureString(creditLine);
                Vector2 creditDrawPosition = new Vector2(Main.screenWidth - 110f, Main.screenHeight - 90f) - Vector2.UnitX * creditTextSize * 0.5f + Vector2.UnitY * textLineCounter * creditTextScale * 32f;
                Main.spriteBatch.DrawString(font, creditLine, creditDrawPosition, textColor, 0f, Vector2.UnitY * creditTextSize * 0.5f, creditTextScale, 0, 0f);
                textLineCounter++;
            }
            textLineCounter = 0;

            // Draw the funny rant text.
            string rantText = Language.GetTextValue($"Mods.{Mod.Name}.Dialog.PurifierEntertainingRantText");
            foreach (string rantLine in WordwrapString(rantText, font, 560, 200, out _))
            {
                if (string.IsNullOrEmpty(rantLine))
                    continue;

                Vector2 rantTextSize = font.MeasureString(rantLine);
                Vector2 rantDrawPosition = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight - 50f) - Vector2.UnitX * rantTextSize * 0.5f + Vector2.UnitY * textLineCounter * creditTextScale * 32f;
                rantDrawPosition.Y -= TimeSinceMonologueBegan * 0.45f - 150f;

                Main.spriteBatch.DrawString(font, rantLine, rantDrawPosition, textColor, 0f, Vector2.UnitY * rantTextSize * 0.5f, creditTextScale, 0, 0f);
                textLineCounter++;
            }
        }
    }
}
