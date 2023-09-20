using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class SlowSolarSpark : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 11;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 120;
            Projectile.scale = 1.25f;
        }

        public override void AI()
        {
            // Release short-lived sparks.
            if (Main.rand.NextBool(24))
            {
                Color sparkColor = Color.Lerp(Color.Yellow, Color.Wheat, Main.rand.NextFloat(0.2f, 0.84f));
                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.TreasureSparkle);
                spark.noLight = true;
                spark.color = sparkColor;
                spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                spark.noGravity = true;
                spark.scale = spark.velocity.Length() * 0.1f + 0.8f;
            }

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            // Gradually accelerate. This is mostly to give a smooth aesthetic moreso than as a gameplay mechanic, since this attack is supposed to be one that requires weaving.
            if (Projectile.velocity.Length() <= 24f)
                Projectile.velocity += Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.15f;

            // Decide rotation.
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).ToRotation() - PiOver2;

            // Fade in and out.
            Projectile.Opacity = GetLerpValue(0f, 4f, Time, true) * GetLerpValue(0f, 18f, Projectile.timeLeft, true);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Draw afterimages.
            int afterimageCount = 10;
            for (int i = afterimageCount - 1; i >= 0; i--)
            {
                float afterimageRotation = Projectile.oldRot[i];
                SpriteEffects directionForImage = Projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                // Make afterimages clump near the true position.
                drawPosition = Vector2.Lerp(drawPosition, Projectile.Center - Main.screenPosition, 0.5f);

                float afterimageScale = Projectile.scale * ((afterimageCount - i) / (float)afterimageCount);

                Color color = Projectile.GetAlpha(Color.White) * ((afterimageCount - i) / (float)afterimageCount);
                color.A = 0;

                Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, (Projectile.frame + i / 3) % Main.projFrames[Type]);
                Main.spriteBatch.Draw(texture, drawPosition, frame, color, afterimageRotation, frame.Size() * 0.5f, afterimageScale, directionForImage, 0f);
            }
            return false;
        }
    }
}
