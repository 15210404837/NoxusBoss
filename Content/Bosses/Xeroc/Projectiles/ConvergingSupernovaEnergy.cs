using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class ConvergingSupernovaEnergy : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 70;
            Projectile.scale = 1.5f;
        }

        public override void AI()
        {
            // Release short-lived cyan-green sparks.
            if (Main.rand.NextBool(24))
            {
                Color sparkColor = Color.Lerp(Color.ForestGreen, Color.Cyan, Main.rand.NextFloat(0.32f, 0.75f));
                sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
                spark.noLight = true;
                spark.color = sparkColor;
                spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                spark.noGravity = spark.velocity.Length() >= 7.5f;
                spark.scale = spark.velocity.Length() * 0.1f + 0.8f;
            }

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            // Gradually accelerate.
            if (Projectile.velocity.Length() <= 16f)
                Projectile.velocity *= 1.03f;

            // Decide rotation.
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).ToRotation() - PiOver2;

            // Fade in and out.
            Projectile.Opacity = GetLerpValue(0f, 12f, Time, true) * GetLerpValue(0f, 12f, Projectile.timeLeft, true);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

            // Draw afterimages.
            int afterimageCount = 5;
            for (int i = 0; i < afterimageCount; ++i)
            {
                float afterimageRotation = Projectile.oldRot[i];
                SpriteEffects directionForImage = Projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                // Make afterimages clump near the true position.
                drawPosition = Vector2.Lerp(drawPosition, Projectile.Center - Main.screenPosition, 0.6f);

                float afterimageScale = Projectile.scale * ((afterimageCount - i) / (float)afterimageCount);

                Color color = Projectile.GetAlpha(lightColor) * ((afterimageCount - i) / (float)afterimageCount);
                color.A = 0;

                Main.spriteBatch.Draw(texture, drawPosition, frame, color, afterimageRotation, frame.Size() * 0.5f, afterimageScale, directionForImage, 0f);
            }
            return false;
        }
    }
}
