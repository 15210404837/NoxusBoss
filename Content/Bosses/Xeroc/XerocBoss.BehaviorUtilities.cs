using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Noxus;
using NoxusBoss.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        public static int TotalUniversalHands => 2;

        public void PerformZPositionEffects()
        {
            // Give the illusion of being in 3D space by shrinking. This is also followed by darkening effects in the draw code, to make it look like he's fading into the dark clouds.
            // The DrawBehind section of code causes Xeroc to layer being things like trees to better sell the illusion.
            NPC.scale = 1f / (ZPosition + 1f);
            if (Math.Abs(ZPosition) >= 2.03f)
                NPC.ShowNameOnHover = false;

            if (ZPosition <= -0.96f)
                NPC.scale = 0f;

            // Resize the hitbox based on scale.
            int oldWidth = NPC.width;
            int idealWidth = (int)(NPC.scale * 440f);
            int idealHeight = (int)(NPC.scale * 700f);
            if (idealWidth != oldWidth)
            {
                NPC.position.X += NPC.width / 2;
                NPC.position.Y += NPC.height / 2;
                NPC.width = idealWidth;
                NPC.height = idealHeight;
                NPC.position.X -= NPC.width / 2;
                NPC.position.Y -= NPC.height / 2;
            }
        }

        public void UpdateIdleSound()
        {
            // Start the loop sound on the first frame.
            bool canPlaySound = NPC.active && CurrentAttack != XerocAttackType.Awaken && CurrentAttack != XerocAttackType.OpenScreenTear && CurrentAttack != XerocAttackType.DeathAnimation;
            if (canPlaySound && (!SoundEngine.TryGetActiveSound(IdleSoundSlot, out ActiveSound s) || !s.IsPlaying))
            {
                s?.Stop();
                IdleSoundSlot = SoundEngine.PlaySound(HummSound with { PlayOnlyIfFocused = true }, NPC.Center);
            }

            if (!SoundEngine.TryGetActiveSound(IdleSoundSlot, out ActiveSound idleSound))
                return;

            // Stop the sound if it can't be currently played.
            if (!canPlaySound)
            {
                idleSound.Volume *= 0.9f;
                if (idleSound.Volume <= 0.001f || !NPC.active)
                    idleSound.Stop();
                return;
            }

            // Make the idle sound's pitch go up the faster Xeroc is movement.
            float movementSpeed = NPC.position.Distance(NPC.oldPosition);
            bool probablyTeleported = movementSpeed >= 160f;
            if (!probablyTeleported)
                idleSound.Sound.Pitch = Remap(movementSpeed, 5f, 35f, 0f, -0.03f);

            // Make the idle sound's volume depend on how opaque and close to the foreground in Xeroc is.
            float backgroundSoundFade = 1f / (ZPosition + 1f);
            if (ZPosition < 0f)
                backgroundSoundFade = 1f;

            idleSound.Volume = NPC.Opacity * backgroundSoundFade * 0.32f;
            idleSound.Position = NPC.Center;
        }

        public void HandlePhaseTransitions()
        {
            if (CurrentAttack == XerocAttackType.DeathAnimation)
                return;

            // Enter phase 2.
            if (LifeRatio <= Phase2LifeRatio && CurrentPhase == 0)
            {
                ClearAllProjectiles();
                SelectNextAttack();
                XerocSky.ManualSunDrawPosition = Vector2.UnitY * -3000f;
                CurrentAttack = XerocAttackType.EnterPhase2;
                PhaseCycleIndex = 0;
                TopTeethOffset = 0f;
                CurrentPhase++;
                NPC.netUpdate = true;
            }

            // Enter phase 3.
            if (LifeRatio <= Phase3LifeRatio && CurrentPhase == 1)
            {
                ClearAllProjectiles();
                SelectNextAttack();
                CurrentAttack = XerocAttackType.EnterPhase3;
                XerocSky.ManualSunDrawPosition = Vector2.UnitY * -3000f;
                PhaseCycleIndex = 0;
                TopTeethOffset = 0f;
                CurrentPhase++;
                NPC.netUpdate = true;
            }
        }

        public void ConjureHandsAtPosition(Vector2 position, Vector2 velocity, bool hasSigil, int robeDirection = 0)
        {
            SoundEngine.PlaySound(SoundID.Item100 with { MaxInstances = 100, Volume = 0.4f }, position);

            Hands.Add(new(position, robeDirection != 0, robeDirection)
            {
                Velocity = velocity,
                HasSigil = hasSigil
            });

            // Create particles.
            for (int i = 0; i < 12; i++)
            {
                int gasLifetime = Main.rand.Next(20, 24);
                float scale = NPC.scale * 2.3f;
                Vector2 gasSpawnPosition = position + Main.rand.NextVector2Circular(75f, 75f) * NPC.scale;
                Vector2 gasVelocity = Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 7.25f + velocity;
                Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));
                Particle gas = new HeavySmokeParticle(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                GeneralParticleHandler.SpawnParticle(gas);
            }
        }

        public void CreateHandVanishVisuals(XerocHand hand)
        {
            // Create particles.
            for (int i = 0; i < 10; i++)
            {
                int gasLifetime = Main.rand.Next(20, 24);
                float scale = 2.3f;
                Vector2 gasSpawnPosition = hand.Center + Main.rand.NextVector2Circular(75f, 75f) * NPC.scale;
                Vector2 gasVelocity = Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 7.25f;
                Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));
                Particle gas = new HeavySmokeParticle(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                GeneralParticleHandler.SpawnParticle(gas);
            }
        }

        public void DestroyAllHands(bool includeUniversalHands = false)
        {
            for (int i = includeUniversalHands ? 0 : TotalUniversalHands; i < Hands.Count; i++)
                CreateHandVanishVisuals(Hands[i]);

            while (Hands.Count > (includeUniversalHands ? 0 : TotalUniversalHands))
                Hands.Remove(Hands.Last());

            NPC.netUpdate = true;
        }

        public void DefaultHandDrift(XerocHand hand, Vector2 hoverDestination, float speedFactor = 1f)
        {
            float maxFlySpeed = NPC.velocity.Length() + 33f;
            Vector2 idealVelocity = (hoverDestination - hand.Center) * 0.2f;

            if (idealVelocity.Length() >= maxFlySpeed)
                idealVelocity = idealVelocity.SafeNormalize(Vector2.UnitY) * maxFlySpeed;
            if (hand.Velocity.Length() <= maxFlySpeed * 0.7f)
                hand.Velocity *= 1.056f;

            hand.Velocity = Vector2.Lerp(hand.Velocity, idealVelocity * speedFactor, 0.27f);

            // If the speed factor is high enough just stick to the ideal position.
            if (speedFactor >= 20f)
            {
                hand.Velocity = Vector2.Zero;
                hand.Center = hoverDestination;
            }
        }

        public void DefaultUniversalHandMotion()
        {
            while (Hands.Count(h => h.UseRobe) < TotalUniversalHands)
                Hands.Insert(0, new(NPC.Center, true, (Hands.Count % 2 == 0).ToDirectionInt()));

            float verticalOffset = Sin(TwoPi * AttackTimer / 120f) * 35f;
            DefaultHandDrift(Hands[0], NPC.Center + new Vector2(-340f, -verticalOffset + 80f) * TeleportVisualsAdjustedScale, 300f);
            DefaultHandDrift(Hands[1], NPC.Center + new Vector2(340f, verticalOffset + 80f) * TeleportVisualsAdjustedScale, 300f);

            Hands[0].DirectionOverride = 0;
            Hands[1].DirectionOverride = 0;
            Hands[0].Rotation = 0f;
            Hands[1].Rotation = 0f;

            Hands[0].UseRobe = true;
            Hands[1].UseRobe = true;
            Hands[0].ShouldOpen = true;
            Hands[1].ShouldOpen = true;
            Hands[0].UsePalmForm = false;
            Hands[1].UsePalmForm = false;
        }

        public void UpdateWings(float animationCompletion)
        {
            for (int i = 0; i < Wings.Length; i++)
            {
                float instanceRatio = i / (float)Wings.Length;
                if (Wings.Length <= 1)
                    instanceRatio = 0f;

                Wings[i].Update(WingsMotionState, animationCompletion, instanceRatio);
            }

            // Play wing flap sounds.
            if (WingsMotionState == WingMotionState.Flap && Distance(animationCompletion, 0.5f) <= 0.03f && NPC.soundDelay <= 0)
            {
                float volume = 1.5f;
                if (ZPosition >= 0.01f)
                    volume /= ZPosition + 1f;

                SoundEngine.PlaySound(WingFlapSound with { Volume = volume }, NPC.Center);
                NPC.soundDelay = 10;
            }
        }

        public void PerformTeethChomp(float animationCompletion, float chompAnimationDelay = 0.7f, int risePower = 4, int chompPower = 24)
        {
            CurveSegment rise = new(EasingType.PolyOut, 0f, 2f, -32f, risePower);
            CurveSegment chomp = new(EasingType.PolyOut, chompAnimationDelay, rise.EndingHeight, 6f - rise.EndingHeight, chompPower);
            TopTeethOffset = PiecewiseAnimation(animationCompletion, rise, chomp);
        }

        public void TeleportTo(Vector2 teleportPosition)
        {
            NPC.Center = teleportPosition;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;

            // Reorient hands to account for the sudden change in position.
            foreach (XerocHand hand in Hands)
                hand.Center = NPC.Center + (hand.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 100f;

            // Reset the oldPos array, so that afterimages don't suddenly "jump" due to the positional change.
            for (int i = 0; i < NPC.oldPos.Length; i++)
                NPC.oldPos[i] = NPC.position;

            SoundEngine.PlaySound(Providence.NearBurnSound with
            {
                Pitch = 0.5f,
                Volume = 2f,
                MaxInstances = 8
            }, NPC.Center);

            // Create teleport particle effects.
            ExpandingGreyscaleCircleParticle circle = new(NPC.Center, Vector2.Zero, new(219, 194, 229), 19, 0.28f);
            VerticalLightStreakParticle bigLightStreak = new(NPC.Center, Vector2.Zero, new(228, 215, 239), 18, new(2.7f, 3.45f));
            MagicBurstParticle magicBurst = new(NPC.Center, Vector2.Zero, new(150, 109, 219), 12, 0.1f, 0.8f);
            for (int i = 0; i < 30; i++)
            {
                Vector2 smallLightStreakSpawnPosition = NPC.Center + Main.rand.NextVector2Square(-NPC.width, NPC.width) * new Vector2(0.4f, 0.2f);
                Vector2 smallLightStreakVelocity = Vector2.UnitY * Main.rand.NextFloat(-3f, 3f);
                VerticalLightStreakParticle smallLightStreak = new(smallLightStreakSpawnPosition, smallLightStreakVelocity, Color.White, 10, new(0.1f, 0.3f));
                GeneralParticleHandler.SpawnParticle(smallLightStreak);
            }

            GeneralParticleHandler.SpawnParticle(circle);
            GeneralParticleHandler.SpawnParticle(bigLightStreak);
            GeneralParticleHandler.SpawnParticle(magicBurst);
        }

        public void SelectNextAttack()
        {
            switch (CurrentAttack)
            {
                case XerocAttackType.Awaken:
                    CurrentAttack = XerocAttackType.OpenScreenTear;
                    break;
                case XerocAttackType.OpenScreenTear:
                    CurrentAttack = XerocAttackType.RoarAnimation;
                    break;
                default:
                    NPC.Opacity = 1f;

                    XerocAttackType[] phaseCycle = Phase1Cycle;
                    if (CurrentPhase == 1)
                        phaseCycle = Phase2Cycle;
                    if (CurrentPhase == 2)
                        phaseCycle = Phase3Cycle;

                    for (int i = 0; i < Hands.Count; i++)
                    {
                        Hands[i].UsePalmForm = false;
                        Hands[i].ScaleFactor = 1f;
                        Hands[i].Opacity = 1f;
                        Hands[i].UseRobe = true;
                    }

                    CurrentAttack = phaseCycle[PhaseCycleIndex % phaseCycle.Length];
                    PhaseCycleIndex++;
                    break;
            }

            SwordSlashCounter = 0;

            ZPosition = 0f;
            GeneralHoverOffset = Vector2.Zero;

            NPC.ai[2] = NPC.ai[3] = 0f;
            AttackTimer = 0f;
            NPC.netUpdate = true;
        }

        public void TriggerDeathAnimation()
        {
            SelectNextAttack();
            ClearAllProjectiles();
            NPC.dontTakeDamage = true;
            CurrentAttack = XerocAttackType.DeathAnimation;
            NPC.netUpdate = true;
        }

        public void PerformMumble()
        {
            if (MumbleTimer <= 0)
                MumbleTimer = 1;
        }

        public static TwinkleParticle CreateTwinkle(Vector2 spawnPosition, Vector2 scaleFactor)
        {
            Color twinkleColor = Color.Lerp(Color.Goldenrod, Color.IndianRed, Main.rand.NextFloat(0.15f, 0.67f));
            TwinkleParticle twinkle = new(spawnPosition, Vector2.Zero, twinkleColor, 24, 8, scaleFactor);
            GeneralParticleHandler.SpawnParticle(twinkle);

            SoundEngine.PlaySound(EntropicGod.TwinkleSound);
            return twinkle;
        }

        public void ClearAllProjectiles()
        {
            List<int> projectilesToDelete = new()
            {
                ModContent.ProjectileType<ArcingStarburst>(),
                ModContent.ProjectileType<BackgroundStar>(),
                ModContent.ProjectileType<BookConstellation>(),
                ModContent.ProjectileType<ClockConstellation>(),
                ModContent.ProjectileType<ControlledStar>(),
                ModContent.ProjectileType<ConvergingSupernovaEnergy>(),
                ModContent.ProjectileType<ExplodingStar>(),
                ModContent.ProjectileType<LightDagger>(),
                ModContent.ProjectileType<LightPortal>(),
                ModContent.ProjectileType<Quasar>(),
                ModContent.ProjectileType<Starburst>(),
                ModContent.ProjectileType<StarPatterenedStarburst>(),
                ModContent.ProjectileType<SunFireball>(),
                ModContent.ProjectileType<SuperCosmicBeam>(),
                ModContent.ProjectileType<SuperLightBeam>(),
                ModContent.ProjectileType<SwordConstellation>(),
                ModContent.ProjectileType<TelegraphedLightLaserbeam>(),
                ModContent.ProjectileType<TelegraphedScreenSlice>(),
                ModContent.ProjectileType<TelegraphedScreenSlice2>(),
                ModContent.ProjectileType<TelegraphedStarLaserbeam>()
            };
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && projectilesToDelete.Contains(p.type))
                {
                    if (p.type == ModContent.ProjectileType<ClockConstellation>())
                        p.Kill();
                    else
                        p.active = false;
                }
            }

            DestroyAllHands();
        }
    }
}
