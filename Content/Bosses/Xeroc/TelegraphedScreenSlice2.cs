using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class TelegraphedScreenSlice2 : ModProjectile, IDrawAdditive
    {
        public ref float TelegraphTime => ref Projectile.ai[0];

        public ref float LineLength => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public static int SliceTime => 19;

        public int ShotProjectileTelegraphTime => (int)(TelegraphTime * 2f - 14f);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20000;

        public override void SetDefaults()
        {
            Projectile.width = 115;
            Projectile.height = 115;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
        }

        public override void AI()
        {
            // Decide the rotation of the line.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Define the universal opacity.
            Projectile.Opacity = GetLerpValue(TelegraphTime + SliceTime - 1f, TelegraphTime + SliceTime - 12f, Time, true);

            if (Time >= TelegraphTime + SliceTime)
                Projectile.Kill();

            // Split the screen if the telegraph is over.
            if (Time == TelegraphTime - 1f)
                LocalScreenSplitSystem.Start(Projectile.Center + Projectile.velocity * LineLength * 0.5f, SliceTime * 2 + 3, Projectile.rotation, Projectile.width * 0.15f);

            if (Time >= TelegraphTime + SliceTime)
                Projectile.Kill();

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= TelegraphTime)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * LineLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Create a telegraph.
            if (Time <= TelegraphTime)
            {
                float localLineLength = LineLength * GetLerpValue(0f, 17f, Time, true);
                float telegraphInterpolant = GetLerpValue(0f, TelegraphTime * 0.5f, Time, true);
                spriteBatch.DrawBloomLine(Projectile.Center, Projectile.Center + Projectile.velocity * localLineLength, Color.IndianRed * telegraphInterpolant, Projectile.width * telegraphInterpolant * 2f);
                spriteBatch.DrawBloomLine(Projectile.Center, Projectile.Center + Projectile.velocity * localLineLength, Color.Wheat * telegraphInterpolant, Projectile.width * telegraphInterpolant);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
