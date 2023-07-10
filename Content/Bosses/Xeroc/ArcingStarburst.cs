using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class ArcingStarburst : ModProjectile, IDrawPixelatedPrims
    {
        public PrimitiveTrail TrailDrawer
        {
            get;
            private set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float MaxSpeedFactor => ref Projectile.ai[1];

        public override string Texture => "NoxusBoss/Content/Bosses/Xeroc/Starburst";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Starburst");
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 13;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = Projectile.MaxUpdates * 120;
        }

        public override void AI()
        {
            if (MaxSpeedFactor <= 0f)
                MaxSpeedFactor = 1f;

            // Release short-lived orange-red sparks.
            if (Main.rand.NextBool(15))
            {
                Color sparkColor = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.25f, 0.75f));
                sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
                spark.noLight = true;
                spark.color = sparkColor;
                spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                spark.noGravity = spark.velocity.Length() >= 3.5f;
                spark.scale = spark.velocity.Length() * 0.1f + 0.64f;
            }

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            // Hande arcing behaviors.
            int slowdownTime = 48;
            int redirectTime = 27;
            int fastHomeTime = 86;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 directionToTarget = Projectile.SafeDirectionTo(target.Center);
            if (Time <= slowdownTime)
                Projectile.velocity *= 0.84f;
            else if (Time <= slowdownTime + redirectTime)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * 10f, 0.035f);
            else if (Time <= slowdownTime + redirectTime + fastHomeTime)
            {
                float maxBaseSpeed = Lerp(23.75f, 32f, Projectile.identity / 8f % 1f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * MaxSpeedFactor * maxBaseSpeed, 0.017f);

                // Die if the player has been touched, to prevent unfair telefrags.
                if (Projectile.WithinRange(target.Center, 28f))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        Color sparkColor = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.25f, 0.75f));
                        sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

                        Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
                        spark.noLight = true;
                        spark.color = sparkColor;
                        spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                        spark.noGravity = true;
                        spark.scale = spark.velocity.Length() * 0.1f + 0.94f;
                    }

                    Projectile.Kill();
                }
            }
            else
                Projectile.velocity *= 1.019f;

            // Fade in and out based on how long the starburst has existed.
            Projectile.Opacity = GetLerpValue(0f, 24f, Projectile.timeLeft, true) * GetLerpValue(0f, 12f, Time, true);

            Time++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(25f, 5f, completionRatio) * Projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            // Make the trail fade out at the end and fade in shparly at the start, to prevent the trail having a definitive, flat "start".
            float trailOpacity = GetLerpValue(0.75f, 0.27f, completionRatio, true) * GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;

            // Interpolate between a bunch of colors based on the completion ratio.
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.OrangeRed, Color.Yellow, 0.4f);
            Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;

            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

            // Draw a bloom flare behind the starburst.
            Starburst.DrawStarburstBloomFlare(Projectile);

            // Draw afterimages that trail closely behind the star core.
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

        public void Draw()
        {
            TrailDrawer ??= new(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            // Draw a flame trail.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/TrailStreaks/StreakMagma"));
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 20);
        }
    }
}
