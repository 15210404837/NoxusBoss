using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.ShapeCurves;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class ClockConstellation : BaseXerocConstellationProjectile
    {
        public override int ConvergeTime => 300;

        public override int StarDrawIncrement => 2;

        public override float StarConvergenceSpeed => 0.00036f;

        public override float StarRandomOffsetFactor => 1f;

        protected override ShapeCurve constellationShape
        {
            get
            {
                ShapeCurveManager.TryFind("Clock", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * Projectile.scale * 1.414f);
            }
        }

        public override Color DecidePrimaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Red, Color.Yellow, Pow(colorVariantInterpolant, 2f)) * 0.33f;
        }

        public override Color DecideSecondaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Orange, Color.White, colorVariantInterpolant) * 0.4f;
        }

        public SlotId TickSound;

        public int TimeRestartDelay;

        public int TollCounter;

        public float PreviousHourRotation = -10f;

        public static float StarburstEjectDistance => 560f;

        public static bool TimeIsStopped
        {
            get;
            set;
        }

        public ref float HourHandRotation => ref Projectile.ai[0];

        public ref float MinuteHandRotation => ref Projectile.ai[1];

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

        public override void PostAI()
        {
            // Fade in at first. If the final toll has happened, fade out.
            if (TollCounter >= 2)
                Projectile.Opacity = Clamp(Projectile.Opacity - 0.01f, 0f, 1f);
            else
                Projectile.Opacity = GetLerpValue(0f, 45f, Time, true);

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
                if (TimeIsStopped || Time <= ConvergeTime * 0.65f || TollCounter >= 2)
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
        }

        public void DrawBloom()
        {
            // Calculate colors.
            Color bloomCircleColor = Projectile.GetAlpha(Color.Orange) * 0.3f;
            Color bloomFlareColor = Projectile.GetAlpha(Color.LightCoral) * 0.64f;

            // Draw the bloom backglow.
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(bloomCircle, drawPosition, null, bloomCircleColor, 0f, bloomCircle.Size() * 0.5f, 5f, 0, 0f);

            // Draw bloom flares that go in opposite rotations.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * -0.4f;
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation * -0.7f, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
        }

        public void DrawClockHands()
        {
            // Acquire clock hand texture.
            Texture2D minuteHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Projectiles/ClockMinuteHand").Value;
            Texture2D hourHandTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Projectiles/ClockHourHand").Value;

            // Calculate clock hand colors.
            float handOpacity = GetLerpValue(ConvergeTime - 150f, ConvergeTime + 54f, Time, true);
            Color generalHandColor = Color.Lerp(Color.OrangeRed, Color.Coral, 0.24f) with { A = 20 };
            Color minuteHandColor = Projectile.GetAlpha(generalHandColor) * handOpacity;
            Color hourHandColor = Projectile.GetAlpha(generalHandColor) * handOpacity;

            // Calculate the clock hand scale and draw positions. The scale is relative to the hitbox of the projectile so that the clock can be arbitrarily sized without issue.
            float handScale = Projectile.width / (float)hourHandTexture.Width * 0.52f;
            Vector2 handBaseDrawPosition = Projectile.Center - Main.screenPosition;
            Vector2 minuteHandDrawPosition = handBaseDrawPosition - MinuteHandRotation.ToRotationVector2() * handScale * 26f;
            Vector2 hourHandDrawPosition = handBaseDrawPosition - HourHandRotation.ToRotationVector2() * handScale * 26f;

            // Draw the hands with afterimages.
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
            // Draw bloom behind the clock to give a nice ambient glow.
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawBloom();
            Main.spriteBatch.ResetBlendState();

            // Draw the clock.
            base.PreDraw(ref lightColor);

            // Draw clock hands.
            DrawClockHands();

            return true;
        }

        public override void Kill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(TickSound, out ActiveSound s))
                s.Stop();
            TimeIsStopped = false;
        }
    }
}
