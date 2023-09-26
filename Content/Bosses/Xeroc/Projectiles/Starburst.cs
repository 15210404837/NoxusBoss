using System.Linq;
using CalamityMod;
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
    public class Starburst : ModProjectile, IDrawPixelated
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            private set;
        }

        public bool Big => Projectile.ai[1] == 2f || BigAndHoming;

        public bool BigAndHoming => Projectile.ai[1] == 3f;

        public ref float Time => ref Projectile.ai[0];

        public bool Redirect => Projectile.ai[1] == 1f || BigAndHoming;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 30;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 150;
            if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.StarManagement)
                Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            // Accelerate over time.
            float maxSpeed = 33f;
            if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.StarManagement)
                maxSpeed = 22.5f;

            if (Projectile.velocity.Length() <= maxSpeed && !ClockConstellation.TimeIsStopped)
                Projectile.velocity *= Big ? 1.0284f : 1.04f;

            // Keep the projectile in stasis if time is stopped.
            if (ClockConstellation.TimeIsStopped)
                Projectile.timeLeft++;

            // Release short-lived orange-red sparks.
            if (Main.rand.NextBool(12))
            {
                Color sparkColor = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.25f, 0.75f));
                sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
                spark.noLight = true;
                spark.color = sparkColor;
                spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                spark.noGravity = spark.velocity.Length() >= 4.2f;
                spark.scale = spark.velocity.Length() * 0.1f + 0.8f;
            }

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            if (Projectile.localAI[0] == 0f && Big)
            {
                Projectile.Size *= 1.8f;
                Projectile.scale *= 1.8f;
                Projectile.localAI[0] = 1f;
            }

            // Sharply redirect towards the closest player if this projectile is instructed to do so.
            if (Redirect && Time >= 65f)
            {
                float redirectAngularVelocity = ToRadians(8f);
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Vector2 idealDirection = Projectile.SafeDirectionTo(target.Center);
                Projectile.velocity = currentDirection.ToRotation().AngleTowards(idealDirection.ToRotation(), redirectAngularVelocity).ToRotationVector2() * Projectile.velocity.Length();
                if (Projectile.velocity.Length() >= 21f)
                    Projectile.velocity *= 0.95f;
            }

            // Die shortly after redirecting, assuming that behavior is in use.
            if (Redirect)
                Projectile.Opacity = GetLerpValue(112f, 100f, Time, true);
            if (Redirect && Time >= 112f)
                Projectile.Kill();
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
            Color color = MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;

            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public static void DrawStarburstBloomFlare(Projectile projectile, float opacityFactor = 1f)
        {
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + projectile.identity;

            Color baseColor1 = ClockConstellation.TimeIsStopped ? Color.Turquoise : Color.Yellow;
            Color baseColor2 = ClockConstellation.TimeIsStopped ? Color.Cyan : Color.Lerp(Color.Red, Color.Wheat, Cos01(Main.GlobalTimeWrappedHourly * 3f + projectile.identity * 0.2f));

            // Make starbursts within the eject range of the clock during the time stop red, to indicate that they're going to be shot outward.
            float backglowOpacityFactor = opacityFactor;
            if (ClockConstellation.TimeIsStopped)
            {
                var clocks = AllProjectilesByID(ModContent.ProjectileType<ClockConstellation>());
                if (clocks.Any() && projectile.WithinRange(clocks.First().Center, ClockConstellation.StarburstEjectDistance))
                {
                    baseColor1 = Color.Red;
                    baseColor2 = Color.Red;
                    backglowOpacityFactor *= 0.25f;
                }

                // Disable soft backglow bloom for the cyan starbursts so that they're more clear against the clock death zone.
                else
                    backglowOpacityFactor = 0f;
            }

            // Draw the bloom flare.
            Color bloomFlareColor1 = baseColor1 with
            {
                A = 0
            } * projectile.Opacity * opacityFactor * 0.45f;
            Color bloomFlareColor2 = baseColor2 with
            {
                A = 0
            } * projectile.Opacity * opacityFactor * 0.45f;
            Vector2 bloomFlareDrawPosition = projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(BloomFlare, bloomFlareDrawPosition, null, bloomFlareColor1, bloomFlareRotation, BloomFlare.Size() * 0.5f, projectile.scale * 0.08f, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, bloomFlareDrawPosition, null, bloomFlareColor2, -bloomFlareRotation, BloomFlare.Size() * 0.5f, projectile.scale * 0.096f, 0, 0f);

            // Draw the backglow.
            Main.spriteBatch.Draw(BloomCircleSmall, bloomFlareDrawPosition, null, Color.Red with
            {
                A = 0
            } * backglowOpacityFactor * 0.5f, 0f, BloomCircleSmall.Size() * 0.5f, projectile.scale * 0.3f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, bloomFlareDrawPosition, null, Color.Wheat with
            {
                A = 0
            } * backglowOpacityFactor * 0.4f, 0f, BloomCircleSmall.Size() * 0.5f, projectile.scale * 0.8f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Draw a bloom flare behind the starburst.
            DrawStarburstBloomFlare(Projectile);

            // Draw afterimages that trail closely behind the star core.
            int afterimageCount = Big ? 16 : 5;
            for (int i = 0; i < afterimageCount; ++i)
            {
                float afterimageRotation = Projectile.oldRot[i];
                SpriteEffects directionForImage = Projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                // Make afterimages clump near the true position.
                drawPosition = Vector2.Lerp(drawPosition, Projectile.Center - Main.screenPosition, 0.67f);

                float afterimageScale = Projectile.scale * Lerp(1f, 0.33f, 1f - ((afterimageCount - i) / (float)afterimageCount));

                Color color = Projectile.GetAlpha(lightColor) * ((afterimageCount - i) / (float)afterimageCount);
                color.A = 0;

                Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, (Projectile.frame + i / 4) % Main.projFrames[Type]);
                Main.spriteBatch.Draw(texture, drawPosition, frame, color, afterimageRotation, frame.Size() * 0.5f, afterimageScale, directionForImage, 0f);
            }
            return false;
        }

        public void DrawWithPixelation()
        {
            if (Big)
                return;

            var fireTrailShader = ShaderManager.GetShader("GenericFlameTrail");
            TrailDrawer ??= new(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, fireTrailShader);

            // Draw a flame trail.
            fireTrailShader.SetTexture(StreakMagma, 1);
            TrailDrawer.Draw(Projectile.oldPos.Take(8), Projectile.Size * 0.5f - Main.screenPosition, 9);
        }

        public override bool ShouldUpdatePosition() => !ClockConstellation.TimeIsStopped;
    }
}
