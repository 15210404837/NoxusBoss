using System.Collections.Generic;
using System.IO;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.BaseEntities;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class TelegraphedPortalLaserbeam : BaseTelegraphedPrimitiveLaserbeam, IDrawPixelated, IDrawAdditive
    {
        // This laser should be drawn with pixelation, and as such should not be drawn manually via the base projectile.
        public override bool UseStandardDrawing => false;

        public override int TelegraphPointCount => 33;

        public override int LaserPointCount => 45;

        public override float MaxLaserLength => 8000f;

        public override float LaserExtendSpeedInterpolant => 0.081f;

        public override ManagedShader TelegraphShader => ShaderManager.GetShader("SideStreakShader");

        public override ManagedShader LaserShader => ShaderManager.GetShader("XerocPortalLaserShader");

        public override void SetDefaults()
        {
            Projectile.width = 112;
            Projectile.height = 112;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void PostAI()
        {
            // Fade out when the laser is about to die.
            Projectile.Opacity = GetLerpValue(TelegraphTime + LaserShootTime - 1f, TelegraphTime + LaserShootTime - 11f, Time, true);

            // Periodically release post-firing particles when the laser is firing.
            // For performance reasons, these effects do not occur if the player is too far away to reasonably witness them.
            bool laserIsFiring = Time >= TelegraphTime && Time <= TelegraphTime + LaserShootTime - 4f;
            if (laserIsFiring && Projectile.WithinRange(Main.LocalPlayer.Center, 1000f))
            {
                // Periodically release outward pulses.
                if (Time % 4f == 3f)
                {
                    PulseRing ring = new(Projectile.Center, Vector2.Zero, new(229, 60, 90), 0.5f, 2.75f, 12);
                    GeneralParticleHandler.SpawnParticle(ring);
                }

                // Create streaks of light.
                for (int i = 0; i < 9; i++)
                {
                    Color lightColor = Color.Lerp(Color.Wheat, Color.IndianRed, Main.rand.NextFloat(0.32f));
                    Vector2 lightDirection = Projectile.velocity.RotatedByRandom(1.23f);
                    SparkParticle lightStreak = new(Projectile.Center + Projectile.velocity * 30f, lightDirection * Projectile.width * Main.rand.NextFloat(0.07f, 0.3f), false, 16, 1.5f, lightColor);
                    GeneralParticleHandler.SpawnParticle(lightStreak);
                }
            }
        }

        public override void OnLaserFire()
        {
            ScreenEffectSystem.SetFlashEffect(Main.LocalPlayer.Center - Vector2.UnitY * 200f, 1.4f, 60);
            ScreenEffectSystem.SetChromaticAberrationEffect(Main.LocalPlayer.Center - Vector2.UnitY * 200f, 0.5f, 30);
            XerocKeyboardShader.BrightnessIntensity += 0.4f;

            // Create particles.
            for (int i = 0; i < Projectile.width / 4; i++)
            {
                int gasLifetime = Main.rand.Next(20, 24);
                float scale = 2.3f;
                Vector2 gasSpawnPosition = Projectile.Center + Projectile.velocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * 120f;
                Vector2 gasVelocity = Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(4f, 45f);
                Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));
                Particle gas = new HeavySmokeParticle(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                if (Main.rand.NextBool(3))
                    gas = new MediumMistParticle(gasSpawnPosition, gasVelocity, gasColor, Color.Black, scale * 1.2f, 255f);

                GeneralParticleHandler.SpawnParticle(gas);
            }

            SoundEngine.PlaySound(XerocBoss.PortalShootSound, Main.LocalPlayer.Center);

            if (OverallShakeIntensity <= 12f)
                StartShakeAtPoint(Projectile.Center, 6f, TwoPi, Vector2.UnitX, 0.09f);
        }

        public override float TelegraphWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public override Color TelegraphColorFunction(float completionRatio)
        {
            float timeFadeOpacity = GetLerpValue(TelegraphTime - 1f, TelegraphTime - 7f, Time, true) * GetLerpValue(0f, TelegraphTime - 20f, Time, true);
            float endFadeOpacity = GetLerpValue(0f, 0.15f, completionRatio, true) * GetLerpValue(1f, 0.67f, completionRatio, true);
            Color baseColor = Color.Lerp(new(206, 46, 164), Color.OrangeRed, Projectile.identity / 9f % 0.7f);
            return baseColor * endFadeOpacity * timeFadeOpacity * Projectile.Opacity * 0.3f;
        }

        public override float LaserWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public override Color LaserColorFunction(float completionRatio)
        {
            float timeFade = GetLerpValue(LaserShootTime - 1f, LaserShootTime - 8f, Time - TelegraphTime, true);
            float startFade = GetLerpValue(0f, 0.065f, completionRatio, true);
            Color baseColor = Color.Lerp(new(206, 46, 164), Color.Orange, Projectile.identity / 9f % 0.7f);

            return baseColor * Projectile.Opacity * timeFade * startFade * 0.75f;
        }

        public override void PrepareTelegraphShader(ManagedShader telegraphShader)
        {
            telegraphShader.TrySetParameter("generalOpacity", Projectile.Opacity);
        }

        public override void PrepareLaserShader(ManagedShader laserShader)
        {
            bool drawAdditively = XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.CircularPortalLaserBarrages;
            laserShader.TrySetParameter("darknessNoiseScrollSpeed", 2.5f);
            laserShader.TrySetParameter("brightnessNoiseScrollSpeed", 1.7f);
            laserShader.TrySetParameter("darknessScrollOffset", Vector2.UnitY * (Projectile.identity * 0.3358f % 1f));
            laserShader.TrySetParameter("brightnessScrollOffset", Vector2.UnitY * (Projectile.identity * 0.3747f % 1f));
            laserShader.TrySetParameter("drawAdditively", drawAdditively);
            laserShader.SetTexture(WavyBlotchNoise, 1);
            laserShader.SetTexture(DendriticNoiseZoomedOut, 2);
        }

        public void DrawWithPixelation() => DrawTelegraphOrLaser();

        // This is unrelated to the laser's drawing itself, and serves more as stuff that exists at the point at which the laser is being fired, to give the impression of a focal point.
        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            float glimmerCompletion = GetLerpValue(8f, TelegraphTime, Time, true);
            if (glimmerCompletion <= 0f || glimmerCompletion >= 1f)
                return;

            // Calculate draw information for the glimmer and glow.
            float glimmerScale = GetLerpValue(0f, 0.45f, glimmerCompletion, true) * GetLerpValue(1f, 0.95f, glimmerCompletion, true);
            float glimmerOpacity = Pow(GetLerpValue(0f, 0.32f, glimmerCompletion, true), 2f) * 0.5f;
            float glimmerRotation = Lerp(PiOver4, Pi * 4f + PiOver4, Pow(glimmerCompletion, 0.15f));

            // Draw the glimmer.
            Vector2 glimmerDrawPosition = Projectile.Center - Main.screenPosition;
            Color glimmerDrawColor = Projectile.GetAlpha(Color.Wheat) * glimmerOpacity;
            Color circularGlowDrawColor = Projectile.GetAlpha(Color.Pink) * glimmerOpacity;
            spriteBatch.Draw(FourPointedStarTexture, glimmerDrawPosition, null, glimmerDrawColor, glimmerRotation, FourPointedStarTexture.Size() * 0.5f, glimmerScale, 0, 0f);
            spriteBatch.Draw(BloomFlare, glimmerDrawPosition, null, glimmerDrawColor * 0.3f, glimmerRotation, BloomFlare.Size() * 0.5f, glimmerScale * 1.5f, 0, 0f);

            // Draw the circular glow.
            spriteBatch.Draw(HollowCircleSoftEdge, glimmerDrawPosition, null, circularGlowDrawColor, Projectile.velocity.ToRotation(), HollowCircleSoftEdge.Size() * 0.5f, glimmerScale * new Vector2(0.9f, 1.25f), 0, 0f);
        }
    }
}
