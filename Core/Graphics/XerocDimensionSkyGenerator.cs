using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Items;
using NoxusBoss.Content.MainMenuThemes;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.Bosses.Xeroc.XerocSky;

namespace NoxusBoss.Core.Graphics
{
    public class XerocDimensionSkyGenerator : ModSystem
    {
        public static ManagedRenderTarget XerocDimensionTarget
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() => XerocDimensionTarget = new(true, RenderTargetManager.CreateScreenSizedTarget));
            Main.OnPreDraw += PrepareDimensionTarget;
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PrepareDimensionTarget;
        }

        public override void PreUpdateEntities()
        {
            // Create Xeroc Dimension metaballs over the mouse cursor if the special accessory is being used.
            if (DeificTouch.UsingEffect)
            {
                for (int i = 0; i < 3; i++)
                    XerocDimensionMetaball.CreateParticle(Main.MouseWorld, Main.rand.NextVector2Circular(4f, 4f), 50f);
            }
        }

        private void PrepareDimensionTarget(GameTime obj)
        {
            if (XerocDimensionTarget is null)
                return;
            if (HeavenlyBackgroundIntensity > 0f && !IsEffectActive)
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.02f, 0f, 1f);

            // Ensure that the target has the correct screen size.
            if (XerocDimensionTarget.Width != Main.screenWidth || XerocDimensionTarget.Height != Main.screenHeight)
                XerocDimensionTarget.Recreate(Main.screenWidth, Main.screenHeight);

            // Evaluate the intensity of the effect. If it is not in use, don't waste resources attempting to update it.
            float intensity = HeavenlyBackgroundIntensity * Remap(ManualSunScale, 1f, 12f, 1f, 0.45f);
            if (!IsEffectActive && DeificTouch.UsingEffect && HeavenlyBackgroundIntensity <= 0f && Intensity <= 0f)
                intensity = 1.5f;
            if (Main.gameMenu && MenuLoader.CurrentMenu == XerocDimensionMainMenu.Instance)
            {
                intensity = 0.85f;
                Main.time = 24000;
                Main.dayTime = true;
            }

            if (intensity <= 0.001f && XerocBoss.Myself is null)
                return;

            // Update the Xeroc smoke particles.
            UpdateSmokeParticles();

            var gd = Main.instance.GraphicsDevice;

            // Switch to the dimension render target.
            gd.SetRenderTarget(XerocDimensionTarget.Target);
            gd.Clear(Color.Transparent);

            // Draw the sky background overlay, sun, and smoke.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            DrawBackground(intensity);
            Main.spriteBatch.End();

            gd.SetRenderTarget(null);
        }

        public static void DrawBackground(float backgroundIntensity)
        {
            DrawSkyOverlay(backgroundIntensity);
            CosmicBackgroundSystem.Draw(backgroundIntensity);
            DrawSmoke(backgroundIntensity);
            DrawGalaxies(backgroundIntensity);
        }

        public static void DrawSkyOverlay(float backgroundIntensity)
        {
            // Draw the sky overlay.
            Rectangle screenArea = new(0, 210, Main.screenWidth, Main.screenHeight - 120);
            Texture2D skyTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocSky").Value;

            Main.spriteBatch.Draw(skyTexture, screenArea, Color.White * backgroundIntensity * Intensity * 0.76f);
        }

        public static void DrawSmoke(float backgroundIntensity)
        {
            // Draw all active smoke particles in the background.
            Texture2D smokeTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/RancorFog").Value;
            foreach (BackgroundSmoke smoke in SmokeParticles)
                Main.spriteBatch.Draw(smokeTexture, smoke.DrawPosition, null, smoke.SmokeColor * GetLerpValue(1f, 15f, smoke.Lifetime - smoke.Time, true) * backgroundIntensity * 0.44f, smoke.Rotation, smokeTexture.Size() * 0.5f, 1.56f, 0, 0f);
        }

        public static void DrawGalaxies(float backgroundIntensity)
        {
            if (backgroundIntensity > 1f)
                backgroundIntensity = 1f;

            var galaxyShader = GameShaders.Misc[$"NoxusBoss:GalaxyShader"];
            var gd = Main.instance.GraphicsDevice;
            Texture2D noise = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/MoltenNoise").Value;
            Vector2 scalingFactor = new(gd.DisplayMode.Width / 2560f, gd.DisplayMode.Height / 1440f);

            // Draw galaxies in the sky.
            ulong seed = (ulong)Main.ActiveWorldFileData.Seed;
            if (Main.gameMenu)
                seed = 774uL;

            List<Vector2> galaxyPositions = new();

            int tries = 0;
            for (int i = 0; i < 45; i++)
            {
                // Randomly decide the orientation of galaxies.
                Matrix transformation = new()
                {
                    M11 = Lerp(0.7f, 1.3f, RandomFloat(ref seed)),
                    M12 = Lerp(-0.3f, 0.3f, RandomFloat(ref seed)),
                    M21 = Lerp(0.3f, 0.3f, RandomFloat(ref seed)),
                    M22 = Lerp(0.7f, 1.3f, RandomFloat(ref seed))
                };
                galaxyShader.Shader.Parameters["transformation"].SetValue(transformation);
                galaxyShader.Apply();

                float baseGalaxyScale = Lerp(0.16f, 1.1f, Pow(RandomFloat(ref seed), 6.3f)) * backgroundIntensity;

                // Randomly place galaxies throughout the sky. They attempt to avoid each other. If these attempts fail, the process is immediately terminated.
                Vector2 galaxySpawnPosition = new Vector2(Lerp(100f, 1900f, RandomFloat(ref seed)), Lerp(100f, 360f, RandomFloat(ref seed))) * scalingFactor;
                if (galaxyPositions.Any(g => g.WithinRange(galaxySpawnPosition, 60f)))
                {
                    i--;
                    tries++;
                    if (tries >= 30)
                        break;

                    continue;
                }

                // Randomly decide galaxy colors based on the world seed.
                float hue = RandomFloat(ref seed);
                float distanceFadeOut = Remap(baseGalaxyScale, 0.4f, 0.9f, 0.2f, 1f);
                Color galaxyColor1 = Main.hslToRgb(hue, 1f, 0.67f) * backgroundIntensity * distanceFadeOut;
                Color galaxyColor2 = Main.hslToRgb((hue + 0.11f) % 1f, 1f, 0.67f) * backgroundIntensity * distanceFadeOut;
                galaxyColor1.G /= 2;
                galaxyColor2.G /= 3;

                Main.spriteBatch.Draw(noise, galaxySpawnPosition, null, galaxyColor1, 0f, noise.Size() * 0.5f, baseGalaxyScale, 0, 0f);
                Main.spriteBatch.Draw(noise, galaxySpawnPosition, null, galaxyColor2, 0f, noise.Size() * 0.5f, baseGalaxyScale * 0.8f, 0, 0f);
                tries = 0;

                galaxyPositions.Add(galaxySpawnPosition);
            }
        }
    }
}
