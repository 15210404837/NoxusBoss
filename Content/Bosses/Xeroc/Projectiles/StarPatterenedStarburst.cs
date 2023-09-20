using System.IO;
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
    public class StarPatterenedStarburst : ModProjectile, IDrawPixelated
    {
        public PrimitiveTrailCopy TrailDrawer
        {
            get;
            private set;
        }

        public Vector2 ClosestPlayerHoverDestination
        {
            get;
            set;
        }

        public float RadiusOffset;

        public float ConvergenceAngleOffset;

        public ref float Time => ref Projectile.ai[0];

        public ref float DelayUntilFreeMovement => ref Projectile.ai[1];

        public ref float OffsetAngle => ref Projectile.localAI[0];

        public static int StarPointCount => 6;

        public static float MaxSpeedFactor => 1.05f;

        public override string Texture => "NoxusBoss/Content/Bosses/Xeroc/Projectiles/Starburst";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
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
            Projectile.timeLeft = Projectile.MaxUpdates * 210;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(RadiusOffset);

        public override void ReceiveExtraAI(BinaryReader reader) => RadiusOffset = reader.ReadSingle();

        public override void AI()
        {
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

            // Stick in place at first.
            float hoverSnapInterpolant = GetLerpValue(11f, 0f, Time - DelayUntilFreeMovement, true);
            if (hoverSnapInterpolant > 0f)
            {
                float radius = Pow(GetLerpValue(0f, 12f, Time, true), 2.3f) * (RadiusOffset + 700f) + 50f;
                float angle = Projectile.velocity.ToRotation();
                if (angle < 0f)
                    angle += TwoPi;

                float stickInPerfectCircleInterpolant = Sqrt(1f - hoverSnapInterpolant);
                if (stickInPerfectCircleInterpolant >= 0.8f)
                    stickInPerfectCircleInterpolant = 1f;

                // Calculate the angle to snap to before moving forward.
                float angleSnapOffset = TwoPi / StarPointCount;
                float snapAngle = Round(angle / angleSnapOffset) * angleSnapOffset;

                Vector2 closestPlayerCenter = Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center;
                Vector2 hoverOffset = Vector2.Lerp(StarPolarEquation(StarPointCount, angle) * radius, (snapAngle + Pi / StarPointCount + ConvergenceAngleOffset).ToRotationVector2() * radius * 1.1f, stickInPerfectCircleInterpolant);
                Vector2 hoverDestination = closestPlayerCenter + hoverOffset;
                if (stickInPerfectCircleInterpolant >= 0.9f)
                {
                    Projectile.Center = hoverDestination;
                    ClosestPlayerHoverDestination = closestPlayerCenter;
                }

                ClosestPlayerHoverDestination = Vector2.Lerp(ClosestPlayerHoverDestination, closestPlayerCenter, hoverSnapInterpolant);
                Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestination, hoverSnapInterpolant);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 4f;
                OffsetAngle = Projectile.velocity.ToRotation();
            }

            // Collapse inward.
            else
            {
                // Perform the convergence behavior.
                float idealDirection = Projectile.AngleTo(ClosestPlayerHoverDestination);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleLerp(idealDirection, 0.075f).ToRotationVector2() * Projectile.velocity.Length();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(ClosestPlayerHoverDestination) * 17.5f, 0.017f);

                if (Projectile.WithinRange(ClosestPlayerHoverDestination, 35f))
                {
                    if (!AnyProjectiles(ModContent.ProjectileType<ExplodingStar>()))
                    {
                        NewProjectileBetter(ClosestPlayerHoverDestination, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                        NewProjectileBetter(ClosestPlayerHoverDestination, Vector2.Zero, ModContent.ProjectileType<ExplodingStar>(), XerocBoss.StarDamage, 0f, -1, 1.112f);

                        float angularOffset = OffsetAngle + Main.rand.NextFloatDirection() * 0.6f;
                        for (int i = 0; i < 11; i++)
                        {
                            Vector2 sparkVelocity = (TwoPi * i / 11f + angularOffset).ToRotationVector2() * 12f;
                            NewProjectileBetter(ClosestPlayerHoverDestination, sparkVelocity, ModContent.ProjectileType<SlowSolarSpark>(), XerocBoss.StarburstDamage, 0f);
                        }
                    }

                    Projectile.Kill();
                }
            }

            // Fade in and out based on how long the starburst has existed.
            Projectile.Opacity = GetLerpValue(0f, 24f, Projectile.timeLeft, true) * GetLerpValue(3f, 19f, Time, true);
            Projectile.scale = GetLerpValue(2f, 18f, Time, true) * 1.5f;

            if (Projectile.FinalExtraUpdate())
                Time++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(25f, 5f, completionRatio) * Projectile.scale * Projectile.Opacity;
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

            color.A = 0;
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

            // Draw a bloom flare behind the starburst.
            Starburst.DrawStarburstBloomFlare(Projectile, 1.6f);

            // Draw afterimages that trail closely behind the star core.
            int afterimageCount = 3;
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

        public void DrawWithPixelation()
        {
            var fireTrailShader = ShaderManager.GetShader("GenericFlameTrail");
            TrailDrawer ??= new(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, fireTrailShader);

            // Draw a flame trail.
            fireTrailShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/TrailStreaks/StreakMagma"), 1);
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 7);
        }

        public override bool ShouldUpdatePosition() => Time >= DelayUntilFreeMovement;

        public override bool? CanDamage() => Time >= DelayUntilFreeMovement + 16f;
    }
}
