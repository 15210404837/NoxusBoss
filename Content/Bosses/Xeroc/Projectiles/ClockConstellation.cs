using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.ShapeCurves;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
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

        private Texture2D bloomFlare;

        private Texture2D bloomCircle;

        private Texture2D starTexture;

        // This stores the clockShape property in a field for performance reasons every frame, since the underlying getter method used there can be straining when done
        // many times per frame, due to looping.
        public ShapeCurve ClockShape;

        public SlotId TickSound;

        public int TimeRestartDelay;

        public int TollCounter;

        public float PreviousHourRotation = -10f;

        public float StarScaleFactor => Remap(Time, 150f, 300f, 1f, 2.6f);

        public static int ConvergeTime => 300;

        public static float StarburstEjectDistance => 560f;

        public static bool TimeIsStopped
        {
            get;
            set;
        }

        public ref float HourHandRotation => ref Projectile.ai[0];

        public ref float MinuteHandRotation => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => $"Terraria/Images/Extra_89";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 840;
            Projectile.height = 840;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 60000;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(TimeRestartDelay);
            writer.Write(TollCounter);
            writer.Write(PreviousHourRotation);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            TimeRestartDelay = reader.ReadInt32();
            TollCounter = reader.ReadInt32();
            PreviousHourRotation = reader.ReadSingle();
        }

        public override void AI()
        {
            // Fade in at first. If the final toll has happened, fade out.
            if (TollCounter >= 2)
                Projectile.Opacity = Clamp(Projectile.Opacity - 0.01f, 0f, 1f);
            else
                Projectile.Opacity = GetLerpValue(0f, 45f, Time, true);

            // Die if Xeroc is not present.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Make the time restart delay go down.
            TimeIsStopped = false;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (TimeRestartDelay >= 1)
            {
                TimeIsStopped = true;
                TimeRestartDelay--;

                if (TimeRestartDelay <= 0)
                {
                    int starburstID = ModContent.ProjectileType<Starburst>();
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.type == starburstID && p.active)
                        {
                            p.timeLeft = 150;
                            p.ai[0] = 65f;

                            if (p.WithinRange(Projectile.Center, StarburstEjectDistance))
                                p.velocity = p.SafeDirectionTo(target.Center) * 14.5f;
                            p.netUpdate = true;
                        }
                    }
                }
            }

            // Approach the nearest player.
            if (Projectile.WithinRange(target.Center, 100f) || TimeIsStopped)
                Projectile.velocity *= 0.82f;
            else
            {
                float approachSpeed = Pow(GetLerpValue(ConvergeTime, 0f, Time, true), 2f) * 19f + 3f;
                Projectile.velocity = Projectile.SafeDirectionTo(target.Center) * approachSpeed;
            }

            // Store the clock shape.
            ClockShape = clockShape;

            // Make the hands move quickly as they fade in before moving more gradually.
            // This cause a time stop if the hour hand reaches a new hour and the clock has completely faded in.
            float handAppearInterpolant = GetLerpValue(0f, ConvergeTime, Time, true);
            float handMovementSpeed = Lerp(1f, 0.11f, Pow(handAppearInterpolant, 1.4f));
            float baseAngularVelocity = ToRadians(0.86f);

            // Make the time go on.
            HourHandRotation += baseAngularVelocity * handMovementSpeed * (1f - TimeIsStopped.ToInt());
            MinuteHandRotation = HourHandRotation * 12f - PiOver2;

            // Make the clock strike if it reaches a new hour.
            float hourArc = TwoPi / 12f;
            float closestHourRotation = Round(HourHandRotation / hourArc) * hourArc;
            if (Abs(HourHandRotation - closestHourRotation) <= 0.008f && handAppearInterpolant >= 1f && closestHourRotation != PreviousHourRotation)
            {
                // Create a clock strike sound and other visuals.
                StartShakeAtPoint(Projectile.Center, 11f);
                SoundEngine.PlaySound(XerocBoss.ClockStrikeSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                HourHandRotation = closestHourRotation;
                MinuteHandRotation = HourHandRotation * 12f - PiOver2;
                PreviousHourRotation = closestHourRotation;
                TimeRestartDelay = 150;
                Projectile.netUpdate = true;
                TollCounter++;
                XerocKeyboardShader.BrightnessIntensity += 0.8f;

                // Make the clock hands split the screen instead on the second toll.
                if (TollCounter >= 2)
                {
                    ScreenEffectSystem.SetFlashEffect(Projectile.Center, 3f, 60);
                    StartShakeAtPoint(Projectile.Center, 16f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int telegraphTime = 41;
                        float telegraphLineLength = 4500;
                        Vector2 hourHandDirection = closestHourRotation.ToRotationVector2();
                        Vector2 minuteHandDirection = MinuteHandRotation.ToRotationVector2();

                        foreach (var starburst in AllProjectilesByID(ModContent.ProjectileType<Starburst>()))
                            starburst.Kill();

                        NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                        NewProjectileBetter(Projectile.Center - minuteHandDirection * telegraphLineLength * 0.5f, minuteHandDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, telegraphTime, telegraphLineLength);
                        NewProjectileBetter(Projectile.Center - hourHandDirection * telegraphLineLength * 0.5f, hourHandDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, telegraphTime, telegraphLineLength);
                    }
                }
            }

            // Start the loop sound and initialize the starting hour configuration on the first frame.
            if (Projectile.localAI[0] == 0f || ((!SoundEngine.TryGetActiveSound(TickSound, out ActiveSound s2) || !s2.IsPlaying) && !TimeIsStopped))
            {
                if (Projectile.localAI[0] == 0f)
                {
                    HourHandRotation = Main.rand.Next(12) * TwoPi / 12f;
                    Projectile.netUpdate = true;
                }

                TickSound = SoundEngine.PlaySound(XerocBoss.ClockTickSound, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            // Update the ticking loop sound.
            if (SoundEngine.TryGetActiveSound(TickSound, out ActiveSound s))
            {
                s.Position = Projectile.Center;
                s.Volume = Projectile.Opacity * handAppearInterpolant * 1.4f;

                // Make the sound temporarily stop if time is stopped.
                if (TimeIsStopped || Time <= ConvergeTime - 135f || TollCounter >= 2)
                    s.Stop();
            }

            // Release starbursts in an even spread. This is made to roughly sync up with the clock ticks.
            int starburstReleaseRate = 18;
            int starburstCount = 9;
            float starburstShootSpeed = 2.05f;
            if (Main.netMode != NetmodeID.MultiplayerClient && handAppearInterpolant >= 0.75f && Time % starburstReleaseRate == 9f && !TimeIsStopped && TollCounter < 2)
            {
                StartShakeAtPoint(Projectile.Center, 3.6f);
                SoundEngine.PlaySound(XerocBoss.SunFireballShootSound, Projectile.Center);

                float shootOffsetAngle = Main.rand.NextBool() ? Pi / starburstCount : 0f;
                for (int i = 0; i < starburstCount; i++)
                {
                    Vector2 starburstVelocity = (TwoPi * i / starburstCount + shootOffsetAngle + Projectile.AngleTo(target.Center)).ToRotationVector2() * starburstShootSpeed;
                    NewProjectileBetter(Projectile.Center, starburstVelocity, ModContent.ProjectileType<Starburst>(), XerocBoss.StarburstDamage, 0f, -1, 0f, 2f);
                }
            }

            // Adjust the time. This process can technically make Main.time negative but that doesn't seem to cause any significant problems, and works fine with the watch UI.
            int hour = (int)((HourHandRotation + PiOver2 + 0.01f).Modulo(TwoPi) / TwoPi * 12f);
            int minute = (int)((MinuteHandRotation + PiOver2).Modulo(TwoPi) / TwoPi * 60f);
            int totalMinutes = hour * 60 + minute;
            Main.dayTime = true;
            Main.time = totalMinutes * 60 - 16200f;

            Time++;
        }

        public float GetStarMovementInterpolant(int index)
        {
            int starPrepareStartTime = (int)(index * ConvergeTime / 2780f) + 10;
            return Pow(GetLerpValue(starPrepareStartTime, starPrepareStartTime + 54f, Time, true), 0.68f);
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

            // Apply a tiny, random offset to the clock position.
            clockPosition += Lerp(-TwoPi, TwoPi, RandomFloat(ref starSeed)).ToRotationVector2() * Lerp(1.5f, 5.3f, RandomFloat(ref starSeed));

            return Vector2.Lerp(startingSpot, clockPosition, GetStarMovementInterpolant(index));
        }

        public void DrawBloom()
        {
            Color bloomCircleColor = Projectile.GetAlpha(Color.Orange) * 0.3f;
            Vector2 bloomDrawPosition = Projectile.Center - Main.screenPosition;

            // Draw the bloom circle.
            Main.spriteBatch.Draw(bloomCircle, bloomDrawPosition, null, bloomCircleColor, 0f, bloomCircle.Size() * 0.5f, 5f, 0, 0f);

            // Draw bloom flares that go in opposite rotations.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * -0.4f;
            Color bloomFlareColor = Projectile.GetAlpha(Color.LightCoral) * 0.64f;
            Main.spriteBatch.Draw(bloomFlare, bloomDrawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, bloomDrawPosition, null, bloomFlareColor, bloomFlareRotation * -0.7f, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
        }

        public void DrawBloomFlare(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;
            Color bloomFlareColor1 = Color.Lerp(Color.Red, Color.Yellow, Pow(colorInterpolant, 2f));
            Color bloomFlareColor2 = Color.Lerp(Color.Orange, Color.White, colorInterpolant);

            bloomFlareColor1 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);
            bloomFlareColor2 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);

            // Make the stars individually twinkle.
            float scaleFactorPhaseShift = index * 5.853567f * (index % 2 == 0).ToDirectionInt();
            float scaleFactor = Lerp(0.75f, 1.25f, Cos01(Main.GlobalTimeWrappedHourly * 6.4f + scaleFactorPhaseShift));
            scale *= scaleFactor;

            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor1 with { A = 0 } * Projectile.Opacity * 0.33f, bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.11f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor2 with { A = 0 } * Projectile.Opacity * 0.41f, -bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.08f, 0, 0f);
        }

        public void DrawStar(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            // Draw a bloom flare behind the star.
            DrawBloomFlare(drawPosition, colorInterpolant, scale * XerocBoss.Myself.scale, index);

            // Draw the star.
            Rectangle frame = starTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color color = Projectile.GetAlpha(Color.Wheat) with { A = 0 } * Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.3f, 1f);

            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, scale * 0.5f, 0, 0f);
            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation - Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation + Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
        }

        public void DrawClockHands()
        {
            Texture2D minuteHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Projectiles/ClockMinuteHand").Value;
            Texture2D hourHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Projectiles/ClockHourHand").Value;

            float handOpacity = GetLerpValue(ConvergeTime - 150f, ConvergeTime + 54f, Time, true);
            Color generalHandColor = Color.Lerp(Color.OrangeRed, Color.Coral, 0.24f) with { A = 20 };
            Color minuteHandColor = Projectile.GetAlpha(generalHandColor) * handOpacity;
            Color hourHandColor = Projectile.GetAlpha(generalHandColor) * handOpacity;

            float handScale = Projectile.width / (float)hourHandTexture.Width * 0.52f;
            Vector2 handBaseDrawPosition = Projectile.Center - Main.screenPosition;
            Vector2 minuteHandDrawPosition = handBaseDrawPosition - MinuteHandRotation.ToRotationVector2() * handScale * 26f;
            Vector2 hourHandDrawPosition = handBaseDrawPosition - HourHandRotation.ToRotationVector2() * handScale * 26f;

            // Draw the hands.
            for (int i = 0; i < 24; i++)
            {
                float afterimageOpacity = 1f - i / 24f;
                float minuteHandAfterimageRotation = MinuteHandRotation - i * (TimeIsStopped ? 0.002f : 0.008f);
                float hourHandAfterimageRotation = HourHandRotation - i * (TimeIsStopped ? 0.0013f : 0.005f);
                Main.spriteBatch.Draw(minuteHandTexture, minuteHandDrawPosition, null, minuteHandColor * afterimageOpacity, minuteHandAfterimageRotation, Vector2.UnitY * minuteHandTexture.Size() * 0.5f, handScale, 0, 0f);
                Main.spriteBatch.Draw(hourHandTexture, hourHandDrawPosition, null, hourHandColor * afterimageOpacity, hourHandAfterimageRotation, Vector2.UnitY * hourHandTexture.Size() * 0.5f, handScale, 0, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Store textures for efficiency.
            bloomCircle ??= ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight", AssetRequestMode.ImmediateLoad).Value;
            bloomFlare ??= ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare", AssetRequestMode.ImmediateLoad).Value;
            starTexture ??= ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

            ulong starSeed = (ulong)Projectile.identity * 674uL + 25uL;

            // Draw bloom behind everything.
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawBloom();
            Main.spriteBatch.ResetBlendState();

            // Draw the stars that compose the clock's outline.
            for (int i = 0; i < ClockShape.ShapePoints.Count; i += 2)
            {
                float colorInterpolant = Sqrt(RandomFloat(ref starSeed));
                float scale = StarScaleFactor * Lerp(0.3f, 0.95f, RandomFloat(ref starSeed)) * Projectile.scale;

                // Make the scale more uniform as the star scale factor gets larger.
                scale = Remap(StarScaleFactor * 0.75f, scale, StarScaleFactor, 1f, 2.5f) * 0.7f;

                Vector2 shapeDrawPosition = GetStarPosition(i);
                DrawStar(shapeDrawPosition, colorInterpolant, scale * 0.4f, i);
            }

            // Draw clock hands.
            DrawClockHands();

            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(TickSound, out ActiveSound s))
                s.Stop();
            TimeIsStopped = false;
        }
    }
}
