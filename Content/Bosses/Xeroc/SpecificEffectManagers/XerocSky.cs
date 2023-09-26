﻿using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers
{
    public class XerocSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => XerocSky.SkyIntensityOverride > 0f || NPC.AnyNPCs(ModContent.NPCType<XerocBoss>());

        public override void Load()
        {
            On_Main.DrawSunAndMoon += MakeMoonFadeAway;
            On_Main.DrawBackground += NoBackgroundDuringXerocFight;
            On_Main.DrawSurfaceBG += NoBackgroundDuringXerocFight2;
        }

        private void MakeMoonFadeAway(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            orig(self, sceneArea, moonColor * Pow(1f - XerocSky.SkyEyeOpacity, 2f), sunColor, tempMushroomInfluence);

            // Draw Xeroc's eye on top of the moon.
            if (XerocSky.SkyEyeOpacity > 0f)
            {
                Vector2 eyeDrawPosition = new Vector2(sceneArea.totalWidth * 0.5f, Main.moonModY + 230f) + sceneArea.SceneLocalScreenPositionOffset;
                XerocSky.ReplaceMoonWithXerocEye(eyeDrawPosition + Vector2.UnitY * 190f);
            }
        }

        private void NoBackgroundDuringXerocFight(On_Main.orig_DrawBackground orig, Main self)
        {
            if (XerocSky.HeavenlyBackgroundIntensity < 0.3f)
                orig(self);
        }

        private void NoBackgroundDuringXerocFight2(On_Main.orig_DrawSurfaceBG orig, Main self)
        {
            if (XerocSky.HeavenlyBackgroundIntensity < 0.3f)
                orig(self);
            else
            {
                SkyManager.Instance.ResetDepthTracker();
                SkyManager.Instance.DrawToDepth(Main.spriteBatch, 1f / 0.12f);
                if (!Main.mapFullscreen)
                    SkyManager.Instance.DrawRemainingDepth(Main.spriteBatch);
            }
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:XerocSky", isActive);
        }
    }

    public class XerocSky : CustomSky
    {
        public class BackgroundSmoke
        {
            public int Time;

            public int Lifetime;

            public float Rotation;

            public Vector2 DrawPosition;

            public Vector2 Velocity;

            public Color SmokeColor;

            public void Update()
            {
                Time++;
                DrawPosition += Velocity;
                Velocity *= 0.983f;
                SmokeColor *= 0.997f;
                Rotation += Velocity.X * 0.01f;
            }
        }

        public static bool IsEffectActive
        {
            get;
            private set;
        }

        public static float Intensity
        {
            get;
            private set;
        }

        public static float StarRecedeInterpolant
        {
            get;
            set;
        }

        public static float SkyIntensityOverride
        {
            get;
            set;
        }

        public static float SkyEyeOpacity
        {
            get;
            set;
        }

        public static float SkyEyeScale
        {
            get;
            set;
        } = 1f;

        public static float SkyPupilScale
        {
            get;
            set;
        } = 1f;

        public static float KaleidoscopeInterpolant
        {
            get;
            set;
        }

        public static Vector2 SkyPupilOffset
        {
            get;
            set;
        }

        public static Vector2 EyeDrawPosition
        {
            get;
            set;
        }

        public static float SeamScale
        {
            get;
            set;
        }

        public static float HeavenlyBackgroundIntensity
        {
            get;
            set;
        }

        public static float ManualSunOpacity
        {
            get;
            set;
        }

        public static float ManualSunScale
        {
            get;
            set;
        } = 1f;

        public static float DifferentStarsInterpolant
        {
            get;
            set;
        }

        public static Vector2 ManualSunDrawPosition
        {
            get;
            set;
        } = DefaultManualSunDrawPosition;

        // The effective screen size changes between world contexts and background contexts, and as such it's necessary to correct for the background context.
        // This value exists as a reference for the world context to allow for that correction to the calculated.
        public static Vector2 OriginalScreenSizeForSun
        {
            get;
            set;
        }

        public static List<BackgroundSmoke> SmokeParticles
        {
            get;
            private set;
        } = new();

        public static bool AlreadyDrewThisFrame
        {
            get;
            set;
        }

        public static Vector2 DefaultManualSunDrawPosition => Vector2.UnitY * -2500f;

        public static float SeamAngle => 1.67f;

        public static float SeamSlope => Tan(-SeamAngle);

        public override void Update(GameTime gameTime)
        {
            AlreadyDrewThisFrame = false;

            // Make the intensity go up or down based on whether the sky is in use.
            Intensity = Clamp(Intensity + IsEffectActive.ToDirectionInt() * 0.01f, 0f, 1f);

            // Make the star recede interpolant go up or down based on how strong the intensity is. If the intensity is at its maximum the effect is uninterrupted.
            StarRecedeInterpolant = Clamp(StarRecedeInterpolant - (1f - Intensity) * 0.11f, 0f, 1f);

            // Disable ambient sky objects like wyverns and eyes appearing in front of the background.
            if (IsEffectActive)
                SkyManager.Instance["Ambience"].Deactivate();

            if (!XerocDimensionSkyGenerator.InProximityOfDivineMonolith)
                SkyIntensityOverride = Clamp(SkyIntensityOverride - 0.07f, 0f, 1f);
            if (Intensity < 1f)
                SkyEyeOpacity = Clamp(SkyEyeOpacity - 0.02f, 0f, Intensity + 0.001f);

            float minKaleidoscopeInterpolant = 0f;
            if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentPhase >= 2)
            {
                var currentAttack = XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack;
                if (currentAttack == XerocBoss.XerocAttackType.SuperCosmicLaserbeam)
                    minKaleidoscopeInterpolant = 0f;
                else if (currentAttack == XerocBoss.XerocAttackType.TimeManipulation)
                    minKaleidoscopeInterpolant = 0.7f;
                else
                    minKaleidoscopeInterpolant = 0.9f;
            }

            // Make a bunch of things return to the base value.
            if (!Main.gamePaused && !IsEffectActive)
            {
                SkyPupilOffset = Utils.MoveTowards(Vector2.Lerp(SkyPupilOffset, Vector2.Zero, 0.03f), Vector2.Zero, 4f);
                SkyPupilScale = Lerp(SkyPupilScale, 1f, 0.05f);
                SkyEyeScale = Lerp(SkyEyeScale, 1f, 0.05f);
                SeamScale = Clamp(SeamScale * 0.87f - 0.023f, 0f, 300f);
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.02f, 0f, 2.5f);
                ManualSunOpacity = Clamp(ManualSunOpacity - 0.04f, 0f, 1f);
                ManualSunScale = Clamp(ManualSunScale * 0.92f - 0.3f, 0f, 50f);
                DifferentStarsInterpolant = Clamp(DifferentStarsInterpolant - 0.1f, 0f, 1f);
                KaleidoscopeInterpolant = Clamp(KaleidoscopeInterpolant * 0.95f - 0.15f, minKaleidoscopeInterpolant, 1f);
            }
            else if (KaleidoscopeInterpolant < minKaleidoscopeInterpolant || (HeavenlyBackgroundIntensity < 1f && minKaleidoscopeInterpolant >= 0.01f))
            {
                KaleidoscopeInterpolant = minKaleidoscopeInterpolant;
                HeavenlyBackgroundIntensity = 1f;
            }

            // Make the eye disappear from the background if Xeroc is already visible in the foreground.
            if (XerocBoss.Myself is not null && XerocBoss.Myself.Opacity >= 0.3f)
                SkyEyeScale = 0f;

            if (!IsEffectActive)
                SeamScale = 0f;
        }

        public static void UpdateSmokeParticles()
        {
            // Randomly emit smoke.
            int smokeReleaseChance = 2;
            if (Main.rand.NextBool(smokeReleaseChance))
            {
                for (int i = 0; i < 4; i++)
                {
                    SmokeParticles.Add(new()
                    {
                        DrawPosition = new Vector2(Main.rand.NextFloat(-400f, Main.screenWidth + 400f), Main.screenHeight + 372f),
                        Velocity = -Vector2.UnitY * Main.rand.NextFloat(5f, 23f) + Main.rand.NextVector2Circular(3f, 3f),
                        SmokeColor = Color.Lerp(Color.Coral, Color.Wheat, Main.rand.NextFloat(0.5f, 0.85f)) * 0.9f,
                        Rotation = Main.rand.NextFloat(TwoPi),
                        Lifetime = Main.rand.Next(120, 480)
                    });
                }
            }

            // Update smoke particles.
            SmokeParticles.RemoveAll(s => s.Time >= s.Lifetime);
            foreach (BackgroundSmoke smoke in SmokeParticles)
                smoke.Update();
        }

        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, Color.White, Intensity * Lerp(0.4f, 1f, HeavenlyBackgroundIntensity) * 0.9f);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            // Ensure that the background only draws once per frame for efficiency.
            if (minDepth >= -1000000f || (AlreadyDrewThisFrame && Main.instance.IsActive))
                return;

            // Draw the sky background overlay, sun, and smoke.
            AlreadyDrewThisFrame = true;
            if (maxDepth >= 0f && minDepth < 0f)
            {
                Main.spriteBatch.Draw(XerocDimensionSkyGenerator.XerocDimensionTarget, Vector2.Zero, Color.White * Pow(Intensity, 2f));

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, GetCustomSkyBackgroundMatrix());
                DrawManualSun(Vector2.UnitY * Main.moonModY);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, GetCustomSkyBackgroundMatrix());
            }
        }

        public static void ReplaceMoonWithXerocEye(Vector2 eyePosition)
        {
            // Store the eye position.
            EyeDrawPosition = eyePosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // Draw a glowing orb over the moon.
            float glowDissipateFactor = Remap(SkyEyeOpacity, 0.2f, 1f, 1f, 0.74f);
            Vector2 backglowOrigin = BloomCircle.Size() * 0.5f;
            Vector2 baseScale = Vector2.One * SkyEyeOpacity * Lerp(1.9f, 2f, Cos01(Main.GlobalTimeWrappedHourly * 4f)) * SkyEyeScale;

            // Make everything "blink" at first.
            baseScale.Y *= 1f - Convert01To010(GetLerpValue(0.25f, 0.75f, SkyEyeOpacity, true));

            Main.spriteBatch.Draw(BloomCircle, eyePosition, null, Color.White * glowDissipateFactor, 0f, backglowOrigin, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircle, eyePosition, null, Color.IndianRed * glowDissipateFactor * 0.4f, 0f, backglowOrigin, baseScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircle, eyePosition, null, Color.Coral * glowDissipateFactor * 0.3f, 0f, backglowOrigin, baseScale * 1.7f, 0, 0f);

            // Draw a bloom flare over the orb.
            Main.spriteBatch.Draw(BloomFlare, eyePosition, null, Color.LightCoral * glowDissipateFactor * 0.6f, Main.GlobalTimeWrappedHourly * 0.4f, BloomFlare.Size() * 0.5f, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, eyePosition, null, Color.Coral * glowDissipateFactor * 0.6f, Main.GlobalTimeWrappedHourly * -0.26f, BloomFlare.Size() * 0.5f, baseScale * 0.7f, 0, 0f);

            // Draw the spires over the bloom flare.
            Main.spriteBatch.Draw(ChromaticSpires, eyePosition, null, Color.White * glowDissipateFactor, 0f, ChromaticSpires.Size() * 0.5f, baseScale * 0.8f, 0, 0f);

            // Draw the eye.
            Texture2D eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/XerocEye").Value;
            Texture2D pupilTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/XerocPupil").Value;
            Vector2 eyeScale = baseScale * 0.4f;
            Main.spriteBatch.Draw(eyeTexture, eyePosition, null, Color.White * SkyEyeOpacity, 0f, eyeTexture.Size() * 0.5f, eyeScale, 0, 0f);
            Main.spriteBatch.Draw(pupilTexture, eyePosition + (new Vector2(6f, 0f) + SkyPupilOffset) * eyeScale, null, Color.White * SkyEyeOpacity, 0f, pupilTexture.Size() * 0.5f, eyeScale * SkyPupilScale, 0, 0f);

            Main.spriteBatch.ResetBlendState();
        }

        public static void DrawManualSun(Vector2 drawOffset)
        {
            if (ManualSunOpacity <= 0f)
                return;

            float realisticStarInterpolant = GetLerpValue(1.8f, 5f, ManualSunScale, true);
            Texture2D sunTexture = TextureAssets.Sun.Value;
            Vector2 sunDrawPosition = ManualSunDrawPosition * new Vector2(Main.screenWidth, Main.screenHeight) / OriginalScreenSizeForSun + drawOffset;
            Main.spriteBatch.Draw(sunTexture, sunDrawPosition, null, Color.White * (1f - realisticStarInterpolant), 0f, sunTexture.Size() * 0.5f, ManualSunScale, 0, 0f);

            // Draw a more realistic sun as it gets bigger.
            if (realisticStarInterpolant >= 0.01f)
            {
                // Draw a bloom flare behind the sun.
                Main.spriteBatch.Draw(BloomFlare, sunDrawPosition, null, Color.LightCoral * realisticStarInterpolant * 0.6f, Main.GlobalTimeWrappedHourly * 0.4f, BloomFlare.Size() * 0.5f, ManualSunScale * 0.18f, 0, 0f);
                Main.spriteBatch.Draw(BloomFlare, sunDrawPosition, null, Color.Coral * realisticStarInterpolant * 0.8f, Main.GlobalTimeWrappedHourly * -0.26f, BloomFlare.Size() * 0.5f, ManualSunScale * 0.18f, 0, 0f);

                Color sunColor = Color.Lerp(Color.Orange, Color.Red, 0.32f);
                float sunScale = ManualSunScale * Intensity * 67f;

                var fireballShader = ShaderManager.GetShader("FireballShader");
                fireballShader.TrySetParameter("mainColor", sunColor.ToVector3() * ManualSunOpacity);
                fireballShader.TrySetParameter("resolution", new Vector2(200f, 200f) * ManualSunScale);
                fireballShader.TrySetParameter("speed", 0.56f);
                fireballShader.TrySetParameter("zoom", 0.0004f);
                fireballShader.TrySetParameter("dist", 60f);
                fireballShader.TrySetParameter("opacity", ManualSunOpacity * realisticStarInterpolant * Intensity * 0.9f);
                fireballShader.SetTexture(VoidTexture, 1);
                fireballShader.SetTexture(InvisiblePixel, 2);

                Main.spriteBatch.Draw(InvisiblePixel, sunDrawPosition, null, Color.White, 0f, InvisiblePixel.Size() * 0.5f, sunScale, SpriteEffects.None, 0f);

                fireballShader.TrySetParameter("mainColor", Color.Wheat.ToVector3() * ManualSunOpacity * realisticStarInterpolant * 0.5f);
                fireballShader.Apply();
                Main.spriteBatch.Draw(InvisiblePixel, sunDrawPosition, null, Color.White, 0f, InvisiblePixel.Size() * 0.5f, sunScale, SpriteEffects.None, 0f);
            }
        }

        public override float GetCloudAlpha() => 1f - Clamp(Intensity, SkyIntensityOverride, 1f);

        public override void Activate(Vector2 position, params object[] args)
        {
            IsEffectActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            IsEffectActive = false;
        }

        public override void Reset()
        {
            IsEffectActive = false;
        }

        public override bool IsActive()
        {
            return IsEffectActive || Intensity > 0f;
        }
    }
}
