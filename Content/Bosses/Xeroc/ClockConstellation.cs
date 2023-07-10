using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.ShapeCurves;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class ClockConstellation : ModProjectile
    {
        private ShapeCurve clockShape
        {
            get
            {
                ShapeCurveManager.TryFind("Clock", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * Projectile.scale * 1.414f);
            }
        }

        // This stores the clockShape property in a field for performance reasons every frame, since the underlying getter method used there can be straining when done
        // many times per frame, due to looping.
        public ShapeCurve ClockShape;

        public float StarScaleFactor => Remap(Time, 150f, 300f, 1f, 2.6f);

        public static int ConvergeTime => 210;

        public ref float HourHandRotation => ref Projectile.ai[0];

        public ref float MinuteHandRotation => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => $"Terraria/Images/Extra_89";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Chronos' Hand");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 50;
        }

        public override void SetDefaults()
        {
            Projectile.width = 840;
            Projectile.height = 840;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 60000;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = GetLerpValue(ConvergeTime - 90f, ConvergeTime - 5f, Time, true);

            // Die if Xeroc is not present.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            Time++;

            // Store the clock shape.
            ClockShape = clockShape;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // TODO -- Make hands do damage.
            return false;
        }

        public float GetStarMovementInterpolant(int index)
        {
            int starPrepareStartTime = (int)(index * ConvergeTime / 180f) + 10;
            return Pow(GetLerpValue(starPrepareStartTime, starPrepareStartTime + 50f, Time, true), 0.65f);
        }

        public Vector2 GetStarPosition(int index)
        {
            // Calculate the seed for the starting spots of the clock's stars. This is randomized based on both projectile index and star index, so it should be
            // pretty unique across the fight.
            ulong starSeed = (ulong)Projectile.identity * 113uL + (ulong)index * 602uL + 54uL;

            // Orient the stars in such a way that they come from the background in random spots.
            Vector2 starDirectionFromCenter = (ClockShape.ShapePoints[index] - ClockShape.Center).SafeNormalize(Vector2.UnitY);
            Vector2 randomOffset = new(Lerp(-1350f, 1350f, RandomFloat(ref starSeed)), Lerp(-920f, 920f, RandomFloat(ref starSeed)));
            Vector2 startingSpot = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + starDirectionFromCenter * 500f + randomOffset;
            Vector2 clockPosition = ClockShape.ShapePoints[index] + Projectile.Center - Main.screenPosition;
            return Vector2.Lerp(startingSpot, clockPosition, GetStarMovementInterpolant(index));
        }

        public void DrawBloomFlare(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            Texture2D bloomFlare = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare").Value;
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;
            Color bloomFlareColor1 = Color.Lerp(Color.SkyBlue, Color.Orange, colorInterpolant);
            Color bloomFlareColor2 = Color.Lerp(Color.Cyan, Color.White, colorInterpolant);

            bloomFlareColor1 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);
            bloomFlareColor2 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);

            // Make the stars individually twinkle.
            float scaleFactorPhaseShift = index * 5.853567f * (index % 2 == 0).ToDirectionInt();
            float scaleFactor = Lerp(0.75f, 1.25f, Cos(Main.GlobalTimeWrappedHourly * 6f + scaleFactorPhaseShift) * 0.5f + 0.5f);
            scale *= scaleFactor;

            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor1 with { A = 0 } * Projectile.Opacity * 0.33f, bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.11f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor2 with { A = 0 } * Projectile.Opacity * 0.41f, -bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.08f, 0, 0f);
        }

        public void DrawStar(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            // Draw a bloom flare behind the star.
            DrawBloomFlare(drawPosition, colorInterpolant, scale * XerocBoss.Myself.scale, index);

            // Draw the star.
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color color = Projectile.GetAlpha(Color.Wheat) with { A = 0 } * Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.3f, 1f);

            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, scale * 0.5f, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation - Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation + Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
        }

        public void DrawClockHands()
        {
            Texture2D minuteHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/ClockMinuteHand").Value;
            Texture2D hourHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/ClockHourHand").Value;
            Color minuteHandColor = Projectile.GetAlpha(Color.White);
            Color hourHandColor = Projectile.GetAlpha(Color.White);
            float handScale = Projectile.width / (float)hourHandTexture.Width * 0.9f;
            Vector2 handDrawPosition = Projectile.Center - Main.screenPosition;

            // Draw the hands.
            Main.spriteBatch.Draw(minuteHandTexture, handDrawPosition, null, minuteHandColor, MinuteHandRotation, Vector2.UnitY * minuteHandTexture.Size() * 0.5f, handScale, 0, 0f);
            Main.spriteBatch.Draw(hourHandTexture, handDrawPosition, null, hourHandColor, MinuteHandRotation, Vector2.UnitY * hourHandTexture.Size() * 0.5f, handScale, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ulong starSeed = (ulong)Projectile.identity * 674uL + 25uL;

            // Draw the stars that compose the blade.
            for (int i = 0; i < ClockShape.ShapePoints.Count; i++)
            {
                float colorInterpolant = Sqrt(RandomFloat(ref starSeed));
                float scale = StarScaleFactor * Lerp(0.15f, 0.95f, RandomFloat(ref starSeed)) * Projectile.scale;

                // Make the scale more uniform as the star scale factor gets larger.
                scale = Remap(StarScaleFactor * 0.75f, scale, StarScaleFactor, 1f, 2.5f) * 0.4f;

                Vector2 shapeDrawPosition = GetStarPosition(i);
                DrawStar(shapeDrawPosition, colorInterpolant, scale * 0.4f, i);
            }

            // Draw clock hands.
            DrawClockHands();

            return false;
        }
    }
}
