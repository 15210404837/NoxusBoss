using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.BaseEntities;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class SuperCosmicBeam : BasePrimitiveLaserbeam, IDrawPixelated
    {
        // This laser should be drawn with pixelation, and as such should not be drawn manually via the base projectile.
        public override bool UseStandardDrawing => false;

        public static int DefaultLifetime => 540;

        public override int LaserPointCount => 45;

        public override float LaserExtendSpeedInterpolant => 0.15f;

        public override float MaxLaserLength => 9400f;

        public override ManagedShader LaserShader => ShaderManager.GetShader("XerocCosmicLaserShader");

        // Since this laserbeam is super big, ensure that it doesn't get pruned based on distance from the camera.
        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20000;

        public override void SetDefaults()
        {
            Projectile.width = 800;
            Projectile.height = 800;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
            Projectile.Opacity = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        // This uses PreAI instead of PostAI to ensure that the AI hook uses the correct, updated velocity when deciding Projectile.rotation.
        public override bool PreAI()
        {
            // Check if Xeroc is present. If he isn't, disappear immediately.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return false;
            }

            // Stick to Xeroc.
            Projectile.Center = XerocBoss.Myself.Center + Projectile.velocity * 300f;

            // Inherit the direction of the laser from Xeroc's direction angle AI value.
            Projectile.velocity = XerocBoss.Myself.ai[2].ToRotationVector2();

            // Grow at the start of the laser's lifetime and shrink again at the end of it.
            Projectile.scale = GetLerpValue(0f, 12f, Time, true) * GetLerpValue(0f, 12f, LaserShootTime - Time, true);

            // Rapidly fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Give a short time window before the laser starts doing damage, to prevent cheap hits.
            if (Time <= 22f)
                return false;

            return base.Colliding(projHitbox, targetHitbox);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Allow the target to be hit multiple times in rapid succession, similar to Sans' low iframes hit effect.
            target.GetModPlayer<NoxusPlayer>().ImmuneTimeOverride = 7;

            // Release on-hit particles.
            float particleIntensity = 1f - target.statLife / (float)target.statLifeMax2;
            float particleAppearInterpolant = GetLerpValue(0.02f, 0.1f, particleIntensity, true);
            float deathFadeOut = GetLerpValue(0.49f, 0.95f, particleIntensity, true);
            for (int i = 0; i < particleAppearInterpolant * (1f - deathFadeOut) * 11f + 4f; i++)
            {
                if (Main.rand.NextFloat() < deathFadeOut - 0.3f)
                    continue;

                Dust light = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Square(-25f, 25f), 264);
                light.velocity = Projectile.SafeDirectionTo(target.Center).RotatedByRandom(0.72f) * Main.rand.NextFloat(1.2f, 8f);
                light.color = Color.Lerp(Color.Cyan, Color.Fuchsia, Sin01(Main.GlobalTimeWrappedHourly * 10f + i * 0.2f)) * particleAppearInterpolant * (1f - deathFadeOut);
                light.scale = Main.rand.NextFloat(0.5f, 1.8f) * Lerp(1f, 0.1f, particleIntensity);
                light.fadeIn = 0.7f;
                light.noGravity = true;
            }

            target.immuneAlpha = (int)(particleIntensity * 255);

            ExpandingGreyscaleCircleParticle circle = new(target.Center, Vector2.Zero, new Color(219, 194, 229) * 0.3f, 8, 0.04f);
            GeneralParticleHandler.SpawnParticle(circle);
        }

        public override float LaserWidthFunction(float completionRatio) => Projectile.scale * Projectile.width;

        public override Color LaserColorFunction(float completionRatio) => new Color(53, 18, 100) * GetLerpValue(0f, 0.12f, completionRatio, true) * Projectile.Opacity;

        public override void PrepareLaserShader(ManagedShader laserShader)
        {
            laserShader.TrySetParameter("uStretchReverseFactor", 0.15f);
            laserShader.TrySetParameter("scrollSpeedFactor", 1.3f);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Cosmos"), 1);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/FireNoise"), 2);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/BurnNoise"), 3);
        }

        public void DrawWithPixelation() => DrawLaser();
    }
}
