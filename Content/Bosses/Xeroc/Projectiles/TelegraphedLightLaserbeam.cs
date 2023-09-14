﻿using System.Collections.Generic;
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
    public class TelegraphedLightLaserbeam : ModProjectile, IDrawPixelated
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
            Projectile.width = 108;
            Projectile.height = 108;

            if (AnyProjectiles(ModContent.ProjectileType<TelegraphedScreenSlice>()))
                Projectile.Size /= 2.5f;

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

            // Apply screen impact and particle effects when the laser fires.
            if (Time == TelegraphTime - 1f)
            {
                ScreenEffectSystem.SetFlashEffect(Projectile.Center, 1f, 20);
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

                SoundEngine.PlaySound(XerocBoss.ExplosionTeleportSound with { MaxInstances = 1 }, Main.LocalPlayer.Center);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 7.5f;
            }

            Time++;
        }

        public float TelegraphWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public Color TelegraphColorFunction(float completionRatio)
        {
            float timeFadeOpacity = GetLerpValue(TelegraphTime - 1f, TelegraphTime - 7f, Time, true) * GetLerpValue(0f, TelegraphTime - 20f, Time, true);
            float endFadeOpacity = GetLerpValue(0f, 0.15f, completionRatio, true) * GetLerpValue(1f, 0.67f, completionRatio, true);
            return Color.LightCoral * endFadeOpacity * timeFadeOpacity * Projectile.Opacity * 0.3f;
        }

        public float LaserWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public Color LaserColorFunction(float completionRatio)
        {
            float timeFade = GetLerpValue(LaserShootTime - 1f, LaserShootTime - 8f, Time - TelegraphTime, true);
            float startFade = GetLerpValue(0f, 0.065f, completionRatio, true);
            Color baseColor = Color.Lerp(Color.IndianRed, Color.Coral, Projectile.identity / 9f % 1f);
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
            var laserShader = ShaderManager.GetShader("XerocStarLaserShader");
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
            List<Vector2> laserControlPoints = Projectile.GetLaserControlPoints(9, LaserLengthFactor * MaxLaserLength, laserDirection);

            // Draw the laser.
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"), 1);
            LaserDrawer.Draw(laserControlPoints, -Main.screenPosition, 45);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
