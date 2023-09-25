using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.BaseEntities;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class TelegraphedLightLaserbeam : BaseTelegraphedPrimitiveLaserbeam, IDrawPixelated
    {
        // This laser should be drawn with pixelation, and as such should not be drawn manually via the base projectile.
        public override bool UseStandardDrawing => false;

        public override int TelegraphPointCount => 33;

        public override int LaserPointCount => 45;

        public override float MaxLaserLength => 8000f;

        public override float LaserExtendSpeedInterpolant => 0.08f;

        public override ManagedShader TelegraphShader => ShaderManager.GetShader("SideStreakShader");

        public override ManagedShader LaserShader => ShaderManager.GetShader("XerocStarLaserShader");

        public override void SetDefaults()
        {
            Projectile.width = 108;
            Projectile.height = 108;

            if (AnyProjectiles(ModContent.ProjectileType<TelegraphedScreenSlice>()))
                Projectile.Size *= 0.4f;

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
            Projectile.Opacity = GetLerpValue(TelegraphTime + LaserShootTime - 1f, TelegraphTime + LaserShootTime - 12f, Time, true);
        }

        public override void OnLaserFire()
        {
            // Apply screen impact and particle effects when the laser fires.
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

            // Play an explosion sound.
            SoundEngine.PlaySound(XerocBoss.ExplosionTeleportSound with { MaxInstances = 1 }, Main.LocalPlayer.Center);

            if (OverallShakeIntensity <= 5.4f)
                StartShakeAtPoint(Projectile.Center, 2f);
        }

        public override float TelegraphWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public override Color TelegraphColorFunction(float completionRatio)
        {
            float timeFadeOpacity = GetLerpValue(TelegraphTime - 1f, TelegraphTime - 7f, Time, true) * GetLerpValue(0f, TelegraphTime - 20f, Time, true);
            float endFadeOpacity = GetLerpValue(0f, 0.15f, completionRatio, true) * GetLerpValue(1f, 0.67f, completionRatio, true);
            return Color.LightCoral * endFadeOpacity * timeFadeOpacity * Projectile.Opacity * 0.3f;
        }

        public override float LaserWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public override Color LaserColorFunction(float completionRatio)
        {
            float timeFade = GetLerpValue(LaserShootTime - 1f, LaserShootTime - 8f, Time - TelegraphTime, true);
            float startFade = GetLerpValue(0f, 0.065f, completionRatio, true);
            Color baseColor = Color.Lerp(Color.IndianRed, Color.Coral, Projectile.identity / 9f % 1f);
            return baseColor * Projectile.Opacity * timeFade * startFade * 0.75f;
        }

        public override void PrepareTelegraphShader(ManagedShader telegraphShader)
        {
            telegraphShader.TrySetParameter("generalOpacity", Projectile.Opacity);
        }

        public override void PrepareLaserShader(ManagedShader laserShader)
        {
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"), 1);
        }

        public void DrawWithPixelation() => DrawTelegraphOrLaser();
    }
}
