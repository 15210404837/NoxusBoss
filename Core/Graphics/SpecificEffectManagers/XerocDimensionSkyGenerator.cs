using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers;
using NoxusBoss.Content.Items.Accessories.VanityEffects;
using NoxusBoss.Content.MainMenuThemes;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers.XerocSky;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class XerocDimensionSkyGenerator : ModSystem
    {
        public static bool InProximityOfDivineMonolith
        {
            get;
            set;
        }

        // Ideally it'd be possible to just turn InProximityOfDivineMonolith back to false if it was already on and its effects were registered, but since NearbyEffects hooks
        // don't run on the same update cycle as the PrepareDimensionTarget method this delay exists.
        public static int TimeSinceCloseToDivineMonolith
        {
            get;
            set;
        }

        public static float DivineMonolithIntensity
        {
            get;
            set;
        }

        public static ManagedRenderTarget XerocDimensionTarget
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // This render target should not be automatically disposed because of how much effort is necessary to regenerate it.
            Main.QueueMainThreadAction(() => XerocDimensionTarget = new(true, RenderTargetManager.CreateScreenSizedTarget, false));
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
                for (int i = 0; i < 2; i++)
                    XerocDimensionMetaball.CreateParticle(Main.MouseWorld, Main.rand.NextVector2Circular(4f, 4f), 28f);
            }
        }

        private void PrepareDimensionTarget(GameTime obj)
        {
            if (XerocDimensionTarget is null || !CosmicBackgroundSystem.HasLoaded)
                return;

            if (HeavenlyBackgroundIntensity > 0f && !IsEffectActive)
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.02f, 0f, 1f);

            // Don't bother doing anything if the background should not be drawn, for performance reasons.
            bool backgroundInactive = HeavenlyBackgroundIntensity <= 0f;
            if (XerocBoss.Myself is not null)
            {
                var xerocAttack = XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack;
                if (xerocAttack == XerocBoss.XerocAttackType.Awaken || xerocAttack == XerocBoss.XerocAttackType.OpenScreenTear || xerocAttack == XerocBoss.XerocAttackType.RoarAnimation)
                    backgroundInactive = false;
            }

            bool usingXerocMenu = Main.gameMenu && MenuLoader.CurrentMenu == XerocDimensionMainMenu.Instance;
            if (backgroundInactive && !usingXerocMenu && DivineMonolithIntensity <= 0f && !DeificTouch.UsingEffect)
                return;

            // Ensure that the target has the correct screen size.
            int width = Main.instance.GraphicsDevice.Viewport.Width;
            int height = Main.instance.GraphicsDevice.Viewport.Height;
            if (XerocDimensionTarget.Width != width || XerocDimensionTarget.Height != height)
                XerocDimensionTarget.Recreate(width, height);

            // Increase the divine monolith proximity timer.
            if (!Main.gamePaused && Main.instance.IsActive)
                TimeSinceCloseToDivineMonolith++;
            if (TimeSinceCloseToDivineMonolith >= 10)
                InProximityOfDivineMonolith = false;

            // Evaluate the intensity of the effect. If it is not in use, don't waste resources attempting to update it.
            float intensity = HeavenlyBackgroundIntensity * Remap(ManualSunScale, 1f, 12f, 1f, 0.45f);
            if (!IsEffectActive && DeificTouch.UsingEffect && HeavenlyBackgroundIntensity <= 0f && Intensity <= 0f)
                intensity = 1.5f;
            if (usingXerocMenu)
            {
                intensity = 1.18f;
                Main.time = 24000;
                Main.dayTime = true;
            }
            if (!Main.gameMenu && (InProximityOfDivineMonolith || DivineMonolithIntensity > 0f))
            {
                DivineMonolithIntensity = Clamp(DivineMonolithIntensity + InProximityOfDivineMonolith.ToDirectionInt() * 0.075f, 0f, 1f);
                intensity = MathF.Max(intensity, DivineMonolithIntensity);
                SkyIntensityOverride = intensity;
            }

            if (intensity <= 0.001f && XerocBoss.Myself is null)
                return;

            // Update the Xeroc smoke particles.
            UpdateSmokeParticles();

            var gd = Main.instance.GraphicsDevice;

            // Switch to the dimension render target.
            gd.SetRenderTarget(XerocDimensionTarget);
            gd.Clear(Color.Transparent);

            // Draw the sky background overlay, sun, and smoke.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            DrawBackground(intensity);
            Main.spriteBatch.End();

            gd.SetRenderTarget(null);
        }

        public static void DrawBackground(float backgroundIntensity)
        {
            float kaleidoscopeOverpowerInterpolant = Pow(1f - KaleidoscopeInterpolant, 0.4f);
            DrawSkyOverlay(backgroundIntensity * kaleidoscopeOverpowerInterpolant);
            CosmicBackgroundSystem.Draw(backgroundIntensity * Remap(KaleidoscopeInterpolant, 1f, 0.1f, 0.2f, 1f));
            DrawSmoke(backgroundIntensity * kaleidoscopeOverpowerInterpolant);
            DrawGalaxies(backgroundIntensity * kaleidoscopeOverpowerInterpolant);

            if (KaleidoscopeInterpolant >= 0.001f)
                DrawKaleidoscopicBackground(backgroundIntensity * KaleidoscopeInterpolant);
        }

        public static void DrawSkyOverlay(float backgroundIntensity)
        {
            // Draw the sky overlay.
            Rectangle screenArea = new(0, 210, Main.screenWidth, Main.screenHeight - 120);
            Texture2D skyTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/SpecificEffectManagers/XerocSky").Value;
            Main.spriteBatch.Draw(skyTexture, screenArea, new Color(88, 88, 88) * backgroundIntensity * (Main.gameMenu ? 1f : Intensity) * 0.76f);
        }

        public static void DrawSmoke(float backgroundIntensity)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            // Draw all active smoke particles in the background.
            Texture2D smokeTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/RancorFog").Value;
            foreach (BackgroundSmoke smoke in SmokeParticles)
                Main.spriteBatch.Draw(smokeTexture, smoke.DrawPosition, null, smoke.SmokeColor * GetLerpValue(1f, 15f, smoke.Lifetime - smoke.Time, true) * backgroundIntensity * 0.44f, smoke.Rotation, smokeTexture.Size() * 0.5f, 1.56f, 0, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
        }

        public static void DrawGalaxies(float backgroundIntensity)
        {
            if (backgroundIntensity > 1f)
                backgroundIntensity = 1f;

            var galaxyShader = ShaderManager.GetShader("GalaxyShader");
            var gd = Main.instance.GraphicsDevice;
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
                    M21 = Lerp(0.2f, 0.3f, RandomFloat(ref seed)),
                    M22 = Lerp(0.7f, 1.3f, RandomFloat(ref seed))
                };
                galaxyShader.TrySetParameter("transformation", transformation);
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

                Main.spriteBatch.Draw(MoltenNoise, galaxySpawnPosition, null, galaxyColor1, 0f, MoltenNoise.Size() * 0.5f, baseGalaxyScale, 0, 0f);
                Main.spriteBatch.Draw(MoltenNoise, galaxySpawnPosition, null, galaxyColor2, 0f, MoltenNoise.Size() * 0.5f, baseGalaxyScale * 0.8f, 0, 0f);
                tries = 0;

                galaxyPositions.Add(galaxySpawnPosition);
            }
        }

        public static void DrawKaleidoscopicBackground(float backgroundIntensity)
        {
            if (XerocBoss.Myself is null)
                return;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            // Apply the kaleidoscope shader.
            bool inFinalPhase = XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentPhase >= 2;
            bool clockAttack = XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.TimeManipulation;
            bool cosmicLaserAttack = XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.SuperCosmicLaserbeam;
            float generalBrightness = 1f;
            float animationSpeed = 0.1687f;
            if (clockAttack)
            {
                generalBrightness = 1.3f;
                animationSpeed = 0.14f;
            }
            else if (inFinalPhase)
            {
                generalBrightness = 2f;
                animationSpeed = 0.31f;
            }
            if (cosmicLaserAttack)
                generalBrightness = 0f;

            var kaleidoscopeShader = ShaderManager.GetShader("KaleidoscopeShader");
            kaleidoscopeShader.TrySetParameter("totalSplits", inFinalPhase ? 4f : 7f);
            kaleidoscopeShader.TrySetParameter("distanceBandingFactor", 0f);
            kaleidoscopeShader.TrySetParameter("animationSpeed", animationSpeed);
            kaleidoscopeShader.TrySetParameter("greyscaleInterpolant", inFinalPhase ? 0.03f : 1f);
            kaleidoscopeShader.TrySetParameter("contrastPower", 1.9f);
            kaleidoscopeShader.TrySetParameter("generalBrightness", generalBrightness);
            kaleidoscopeShader.TrySetParameter("vignetteStrength", 1.8f);
            kaleidoscopeShader.TrySetParameter("screenPosition", Main.screenPosition);
            kaleidoscopeShader.TrySetParameter("zoom", Vector2.One * (inFinalPhase ? 1.5f : 0.4f));
            kaleidoscopeShader.Apply();

            // Draw the pattern.
            Rectangle screenArea = new(0, -170, 1920, 1250);
            Texture2D patternTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/SpecificEffectManagers/BackgroundPattern").Value;
            if (inFinalPhase)
                patternTexture = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/TheOriginalLight/Background").Value;

            Main.spriteBatch.Draw(patternTexture, screenArea, Color.White * backgroundIntensity);
        }
    }
}
