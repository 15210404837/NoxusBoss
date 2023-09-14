using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class SuperCosmicBeam : ModProjectile, IDrawPixelated
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            private set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLengthFactor => ref Projectile.ai[1];

        public static int LaserLifetime => 540;

        public static float MaxLaserLength => 9400f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

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

        public override void AI()
        {
            // Stick to Xeroc if possible.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Define the laser's direction.
            Projectile.rotation = XerocBoss.Myself.ai[2];
            Projectile.velocity = Projectile.rotation.ToRotationVector2();
            Projectile.Center = XerocBoss.Myself.Center + Projectile.velocity * 300f;

            // Fade in.
            LaserLengthFactor = Clamp(LaserLengthFactor + 0.15f, 0f, 1f);
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Decide the scale of the laser.
            Projectile.scale = GetLerpValue(0f, 12f, Time, true) * GetLerpValue(0f, 12f, LaserLifetime - Time, true);

            if (Time >= LaserLifetime)
                Projectile.Kill();

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Give a short time window before the laser starts doing damage, to prevent cheap hits.
            if (Time <= 22f)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity * LaserLengthFactor * MaxLaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
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

        public float LaserWidthFunction(float completionRatio) => Projectile.scale * Projectile.width;

        public Color LaserColorFunction(float completionRatio) => new Color(53, 18, 100) * GetLerpValue(0f, 0.12f, completionRatio, true) * Projectile.Opacity;

        public void DrawWithPixelation()
        {
            // Initialize the laser drawer.
            var laserShader = ShaderManager.GetShader("XerocCosmicLaserShader");
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, laserShader);

            // Calculate laser control points.
            Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            List<Vector2> laserControlPoints = Projectile.GetLaserControlPoints(16, LaserLengthFactor * MaxLaserLength, laserDirection);

            // Draw the laser.
            laserShader.TrySetParameter("uStretchReverseFactor", 0.15f);
            laserShader.TrySetParameter("scrollSpeedFactor", 1.3f);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Cosmos"), 1);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/FireNoise"), 2);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/BurnNoise"), 3);
            LaserDrawer.Draw(laserControlPoints, -Main.screenPosition, 45);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
