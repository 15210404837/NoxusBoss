using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Shaders;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class OriginalLightGlitchOverlaySystem : ModSystem
    {
        public static float OverlayInterpolant
        {
            get;
            set;
        }

        public static float GlitchIntensity
        {
            get;
            set;
        }

        public static float EyeOverlayOpacity
        {
            get;
            set;
        }

        public static float WhiteOverlayInterpolant
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawOverlay;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawOverlay;
        }

        public override void PreUpdateEntities()
        {
            // Get rid of the overlay if Xeroc is not present.
            // Otherwise, though, it is Xeroc's responsibility to turn this effect off.
            if (XerocBoss.Myself is null)
            {
                OverlayInterpolant = 0f;
                GlitchIntensity = 0f;
                EyeOverlayOpacity = 0f;
            }

            // Make the white overlay rapidly dissipate.
            WhiteOverlayInterpolant = Clamp(WhiteOverlayInterpolant * 0.92f - 0.056f, 0f, 1f);
            if (WhiteOverlayInterpolant >= 0.001f)
                TotalWhiteOverlaySystem.WhiteInterpolant = WhiteOverlayInterpolant;
        }

        private void DrawOverlay(GameTime obj)
        {
            if (OverlayInterpolant <= 0f || Main.gameMenu)
                return;

            // Disable this effect if the player has photosensitivity mode enabled, since the animations performed during this are very fast.
            if (NoxusBossConfig.Instance.PhotosensitivityMode)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            // Determine Xeroc's attack timer. The longer the attack persists, the more intense the jitter effects are.
            float attackTimer = XerocBoss.Myself.ModNPC<XerocBoss>().AttackTimer;

            // Calcuate the general opacity of everything. This sharply contrasts the white overlay system's white interpolant, so that it takes priority when drawing.
            float generalOpacity = Pow(1f - TotalWhiteOverlaySystem.WhiteInterpolant, 4f);
            DrawLightTextureOverlay(attackTimer, generalOpacity, out Vector2 lightScale);

            // Disable the shader so that the eye can be overlayed if necessary.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin();

            // Draw Xeroc's eye if necessary.
            Texture2D xerocEye = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/XerocEye").Value;
            Vector2 eyeOffset = -Vector2.UnitY * Main.rand.NextVector2Circular(1f, 1f) * lightScale.X * attackTimer * 0.125f;
            Main.spriteBatch.Draw(xerocEye, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + eyeOffset, null, Color.White * generalOpacity * EyeOverlayOpacity * 1.2f, 0f, xerocEye.Size() * 0.5f, lightScale.X * 0.67f, 0, 0f);

            Main.spriteBatch.End();
        }

        public static void DrawLightTextureOverlay(float attackTimer, float generalOpacity, out Vector2 lightScale)
        {
            // Calculate information about the draw effect. The frames interpolate based on Xeroc's attack timer from above, and the effect is rotated 180 degrees when the glitch is active.
            int lightFrame = Clamp((int)(attackTimer * 0.44f) + 1, 1, 15);
            SpriteEffects direction = SpriteEffects.None;
            if (GlitchIntensity >= 0.01f)
                direction = SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;

            // Collect the light texture and calculate how much it needs to be scaled by to cover the entire screen.
            Texture2D theOriginalLight = ModContent.Request<Texture2D>($"NoxusBoss/Assets/ExtraTextures/TheOriginalLight/frame{lightFrame}", AssetRequestMode.ImmediateLoad).Value;
            lightScale = new Vector2(Main.screenWidth, Main.screenHeight) / theOriginalLight.Size() * 1.05f;

            // Draw the base background.
            PrepareNoiseShader();
            Main.spriteBatch.Draw(theOriginalLight, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, null, Color.White * generalOpacity * OverlayInterpolant, 0f, theOriginalLight.Size() * 0.5f, lightScale, direction, 0f);

            // Draw a second, additive layer on top of the background that jitters.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            PrepareNoiseShader();

            Vector2 jitterOffset = Main.rand.NextVector2Unit() * Pow(attackTimer, 1.42f) * 0.09f;
            Main.spriteBatch.Draw(theOriginalLight, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + jitterOffset, null, Color.White * generalOpacity * OverlayInterpolant * 0.8f, 0f, theOriginalLight.Size() * 0.5f, lightScale, direction, 0f);
        }

        private static void PrepareNoiseShader()
        {
            var glitchShader = ShaderManager.GetShader("GlitchShader");
            glitchShader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/SharpNoise"), 1);
            glitchShader.TrySetParameter("coordinateZoomFactor", Vector2.One * 0.5f);
            glitchShader.TrySetParameter("glitchInterpolant", GlitchIntensity);
            glitchShader.Apply();
        }
    }
}
