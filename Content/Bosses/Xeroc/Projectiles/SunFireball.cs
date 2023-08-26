using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Particles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class SunFireball : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Release fire particles.
            for (int i = 0; i < 3; i++)
            {
                Color fireColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.2f, 0.4f));
                fireColor = Color.Lerp(fireColor, Color.White, Main.rand.NextFloat(0.4f));
                float angularVelocity = Main.rand.NextFloat(0.035f, 0.08f);
                FireballParticle fire = new(Projectile.Center, Projectile.velocity * 0.6f, fireColor, 10, Main.rand.NextFloat(0.47f, 0.64f) * Projectile.scale, 1f, true, Main.rand.NextBool().ToDirectionInt() * angularVelocity);
                GeneralParticleHandler.SpawnParticle(fire);
            }

            // Add a mild amount of slithering movement.
            float slitherOffset = Sin(Time / 11.75f + Projectile.identity * 0.4f) * GetLerpValue(36f, 54f, Time, true) * 6.4f;
            Vector2 perpendicularDirection = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(PiOver2);
            Projectile.Center += perpendicularDirection * slitherOffset;

            // Accelerate over enough time has passed.
            // At the very start of the projectile's lifetime it decelerates.
            if (Time <= 40f)
                Projectile.velocity *= 0.943f;
            else if (Projectile.velocity.Length() < 21f)
                Projectile.velocity *= 1.0247f;

            // Make the fire grow in size.
            Projectile.scale = Clamp(Projectile.scale + 0.067f, 0f, 1.2f);
            Vector2 newScale = Vector2.One * Projectile.scale * 30f;
            if (Projectile.Size != newScale)
                Projectile.Size = newScale;

            // Increment time.
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.White;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], drawColor);
            return false;
        }
    }
}
