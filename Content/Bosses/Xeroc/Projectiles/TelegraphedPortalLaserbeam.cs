using System.Collections.Generic;
using System.IO;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class TelegraphedPortalLaserbeam : ModProjectile, IDrawPixelated, IDrawAdditive
    {
        public PrimitiveTrailCopy TelegraphDrawer
        {
            get;
            private set;
        }

        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            private set;
        }

        public ref float TelegraphTime => ref Projectile.ai[0];

        public ref float LaserShootTime => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public ref float LaserLengthFactor => ref Projectile.localAI[1];

        public static float MaxLaserLength => 8000f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

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

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(LaserLengthFactor);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LaserLengthFactor = reader.ReadSingle();
        }

        public override void AI()
        {
            // Make the laser extend after the telegraph has vanished.
            if (Time >= TelegraphTime)
                LaserLengthFactor = Lerp(LaserLengthFactor, 1f, 0.08f);

            // Decide the rotation of the laser.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Define the universal opacity.
            Projectile.Opacity = GetLerpValue(TelegraphTime + LaserShootTime - 1f, TelegraphTime + LaserShootTime - 12f, Time, true);

            if (Time >= TelegraphTime + LaserShootTime)
                Projectile.Kill();

            // Periodically release post-firing particles.
            if (Time >= TelegraphTime && Time <= TelegraphTime + LaserShootTime - 4f && Projectile.WithinRange(Main.LocalPlayer.Center, 1000f))
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

            // Apply screen impact and particle effects when the laser fires.
            if (Time == TelegraphTime - 1f)
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

                SoundEngine.PlaySound(XerocBoss.PortalShootSound with { MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest }, Main.LocalPlayer.Center);

                if (OverallShakeIntensity <= 12f)
                    StartShakeAtPoint(Projectile.Center, 6f, TwoPi, Vector2.UnitX, 0.09f);
            }

            Time++;
        }

        public float TelegraphWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public Color TelegraphColorFunction(float completionRatio)
        {
            float timeFadeOpacity = GetLerpValue(TelegraphTime - 1f, TelegraphTime - 7f, Time, true) * GetLerpValue(0f, TelegraphTime - 20f, Time, true);
            float endFadeOpacity = GetLerpValue(0f, 0.15f, completionRatio, true) * GetLerpValue(1f, 0.67f, completionRatio, true);
            Color baseColor = Color.Lerp(new(206, 46, 164), Color.OrangeRed, Projectile.identity / 9f % 0.7f);
            return baseColor * endFadeOpacity * timeFadeOpacity * Projectile.Opacity * 0.3f;
        }

        public float LaserWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public Color LaserColorFunction(float completionRatio)
        {
            float timeFade = GetLerpValue(LaserShootTime - 1f, LaserShootTime - 8f, Time - TelegraphTime, true);
            float startFade = GetLerpValue(0f, 0.065f, completionRatio, true);
            Color baseColor = Color.Lerp(new(206, 46, 164), Color.Orange, Projectile.identity / 9f % 0.7f);

            return baseColor * Projectile.Opacity * timeFade * startFade * 0.75f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= TelegraphTime || Time >= TelegraphTime + LaserShootTime - 7f)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * LaserLengthFactor * MaxLaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public void DrawWithPixelation()
        {
            // Initialize primitive drawers.
            var telegraphShader = ShaderManager.GetShader("SideStreakShader");
            var laserShader = ShaderManager.GetShader("XerocPortalLaserShader");
            TelegraphDrawer ??= new(TelegraphWidthFunction, TelegraphColorFunction, null, true, telegraphShader);
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, laserShader);

            // Draw the telegraph at first.
            Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            if (Time <= TelegraphTime)
            {
                // Calculate telegraph control points. The key difference between this and the laser is that the telegraph always reaches out by the laser's maximum distance, while the laser bursts out a bit initially.
                List<Vector2> telegraphControlPoints = Projectile.GetLaserControlPoints(6, MaxLaserLength, laserDirection);

                telegraphShader.TrySetParameter("generalOpacity", Projectile.Opacity);
                TelegraphDrawer.Draw(telegraphControlPoints, -Main.screenPosition, 33);
                return;
            }

            // Calculate laser control points.
            List<Vector2> laserControlPoints = Projectile.GetLaserControlPoints(8, LaserLengthFactor * MaxLaserLength, laserDirection);

            // Draw the laser after the telegraph has ceased.
            bool drawAdditively = XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.CircularPortalLaserBarrages;
            laserShader.TrySetParameter("darknessNoiseScrollSpeed", 2.5f);
            laserShader.TrySetParameter("brightnessNoiseScrollSpeed", 1.7f);
            laserShader.TrySetParameter("darknessScrollOffset", Vector2.UnitY * (Projectile.identity * 0.3358f % 1f));
            laserShader.TrySetParameter("brightnessScrollOffset", Vector2.UnitY * (Projectile.identity * 0.3747f % 1f));
            laserShader.TrySetParameter("drawAdditively", drawAdditively);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"), 1);
            laserShader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons2"), 2);
            LaserDrawer.Draw(laserControlPoints, -Main.screenPosition, 45);
        }

        public override bool ShouldUpdatePosition() => false;

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            float glimmerCompletion = GetLerpValue(8f, TelegraphTime, Time, true);
            if (glimmerCompletion <= 0f || glimmerCompletion >= 1f)
                return;

            float glimmerScale = GetLerpValue(0f, 0.45f, glimmerCompletion, true) * GetLerpValue(1f, 0.95f, glimmerCompletion, true);
            float glimmerOpacity = Pow(GetLerpValue(0f, 0.32f, glimmerCompletion, true), 2f) * 0.5f;
            float glimmerRotation = Lerp(PiOver4, Pi * 4f + PiOver4, Pow(glimmerCompletion, 0.15f));
            Texture2D star = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/FourPointedStar").Value;
            Texture2D backglow = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare").Value;
            Texture2D circularGlow = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;

            // Draw the glimmer.
            Vector2 glimmerDrawPosition = Projectile.Center - Main.screenPosition;
            Color glimmerDrawColor = Projectile.GetAlpha(Color.Wheat) * glimmerOpacity;
            Color circularGlowDrawColor = Projectile.GetAlpha(Color.Pink) * glimmerOpacity;
            spriteBatch.Draw(star, glimmerDrawPosition, null, glimmerDrawColor, glimmerRotation, star.Size() * 0.5f, glimmerScale, 0, 0f);
            spriteBatch.Draw(backglow, glimmerDrawPosition, null, glimmerDrawColor * 0.3f, glimmerRotation, backglow.Size() * 0.5f, glimmerScale * 1.5f, 0, 0f);

            // Draw the circular glow.
            spriteBatch.Draw(circularGlow, glimmerDrawPosition, null, circularGlowDrawColor, Projectile.velocity.ToRotation(), circularGlow.Size() * 0.5f, glimmerScale * new Vector2(0.9f, 1.25f), 0, 0f);
        }
    }
}
