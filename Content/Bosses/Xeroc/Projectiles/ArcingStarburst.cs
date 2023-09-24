using CalamityMod;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class ArcingStarburst : ModProjectile, IDrawPixelated, IAdditiveDrawer
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            private set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float MaxSpeedFactor => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 13;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
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
                Color sparkColor = Color.Lerp(Color.Yellow, Color.Cyan, Main.rand.NextFloat(0.4f, 0.85f));
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
            int slowdownTime = 53;
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
                        Color sparkColor = Color.Lerp(Color.Yellow, Color.Cyan, Main.rand.NextFloat(0.4f, 0.98f));
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
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            Time++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(18f, 5f, completionRatio) * Projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            // Make the trail fade out at the end and fade in shparly at the start, to prevent the trail having a definitive, flat "start".
            float trailOpacity = GetLerpValue(0.75f, 0.27f, completionRatio, true) * GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;

            // Interpolate between a bunch of colors based on the completion ratio.
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Yellow, Color.Cyan, Lerp(0.35f, 0.95f, Projectile.identity / 14f % 1f));
            Color endColor = Color.Lerp(Color.DeepSkyBlue, Color.Black, 0.35f);
            Color color = MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;

            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            // Draw the bloom flare.
            Texture2D bloomFlare = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare").Value;
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;

            Color baseColor1 = Color.Turquoise;
            Color baseColor2 = Color.DeepSkyBlue;
            Color bloomFlareColor1 = baseColor1 with { A = 0 } * Projectile.Opacity * 0.54f;
            Color bloomFlareColor2 = baseColor2 with { A = 0 } * Projectile.Opacity * 0.54f;

            Vector2 bloomFlareDrawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(bloomFlare, bloomFlareDrawPosition, null, bloomFlareColor1, bloomFlareRotation, bloomFlare.Size() * 0.5f, Projectile.scale * 0.08f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, bloomFlareDrawPosition, null, bloomFlareColor2, -bloomFlareRotation, bloomFlare.Size() * 0.5f, Projectile.scale * 0.096f, 0, 0f);

            // Draw the star.
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Projectile.GetAlpha(Color.White);
            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale * 1f, 0, 0f);
        }

        public void DrawWithPixelation()
        {
            var fireTrailShader = ShaderManager.GetShader("GenericFlameTrail");
            TrailDrawer ??= new(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, fireTrailShader);

            // Draw a flame trail.
            fireTrailShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/TrailStreaks/StreakMagma"), 1);
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 11);
        }
    }
}
