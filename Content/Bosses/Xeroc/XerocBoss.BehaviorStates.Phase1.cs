﻿using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc.Projectiles;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        public void DoBehavior_ConjureExplodingStars()
        {
            int redirectTime = 15;
            int hoverTime = 22;
            int starShootCount = 6;
            int starCreateRate = 4;
            int starTelegraphTime = starShootCount * starCreateRate;
            int starBlastDelay = 5;
            int attackTransitionDelay = 72;
            int explosionCount = 1;
            float starOffsetRadius = 480f;
            ref float explosionCounter = ref NPC.ai[2];

            // Make the robe's eyes stare at the target while hovering.
            if (AttackTimer >= redirectTime)
                RobeEyesShouldStareAtTarget = true;

            Vector2 hoverDestination = Target.Center - Vector2.UnitY * 380f;
            Vector2 idealVelocity = NPC.SafeDirectionTo(hoverDestination) * MathF.Max(0f, NPC.Distance(hoverDestination) - 230f) * 0.17f;
            if (AttackTimer >= redirectTime + hoverTime)
                idealVelocity = Vector2.Zero;

            NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.11f);

            // Slow down rapidly if flying past the hover destination. If this happens when Xeroc is moving really, really fast a sonic boom of sorts is creating.
            if (Vector2.Dot(NPC.velocity, NPC.SafeDirectionTo(hoverDestination)) < 0f)
            {
                // Create the sonic boom if necessary.
                if (NPC.velocity.Length() >= 75f)
                {
                    NPC.velocity = NPC.velocity.ClampMagnitude(0f, 74f);
                    SoundEngine.PlaySound(SunFireballShootSound with { Pitch = 0.6f }, Target.Center);
                    ScreenEffectSystem.SetFlashEffect(NPC.Center, 4f, 54);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }

                NPC.velocity *= 0.67f;
            }

            // Flap wings.
            UpdateWings(AttackTimer / 54f % 1f);

            // Update teeth.
            PerformTeethChomp(AttackTimer / 84f % 1f, 0.84f);

            // Update hands.
            if (Hands.Count >= 2)
            {
                DefaultHandDrift(Hands[0], NPC.Center + new Vector2(-400f, 100f) * TeleportVisualsAdjustedScale, 2.5f);
                DefaultHandDrift(Hands[1], NPC.Center + new Vector2(400f, 100f) * TeleportVisualsAdjustedScale, 2.5f);
                Hands[0].Rotation = Hands[0].Rotation.AngleLerp(PiOver2, 0.1f);
                Hands[1].Rotation = Hands[1].Rotation.AngleLerp(-PiOver2, 0.1f);
                Hands[0].ShouldOpen = AttackTimer >= redirectTime + hoverTime - 7f;
                Hands[1].ShouldOpen = AttackTimer >= redirectTime + hoverTime - 7f;
                Hands[0].RobeDirection = -1;
                Hands[1].RobeDirection = 1;

                // Snap fingers and make the screen shake.
                if (AttackTimer == redirectTime + hoverTime - 10f)
                {
                    SoundEngine.PlaySound(FingerSnapSound with { Volume = 4f });
                    Target.Calamity().GeneralScreenShakePower = 11f;
                }
            }

            // Create star telegraphs.
            if (AttackTimer >= redirectTime + hoverTime && AttackTimer <= redirectTime + hoverTime + starTelegraphTime && AttackTimer % starCreateRate == 1f)
            {
                float starSpawnOffsetAngle = TwoPi * (AttackTimer - redirectTime - hoverTime) / starTelegraphTime - PiOver2;
                Vector2 starSpawnOffset = starSpawnOffsetAngle.ToRotationVector2() * starOffsetRadius;
                StarSpawnOffsets.Add(starSpawnOffset);
                CreateTwinkle(Target.Center + starSpawnOffset, Vector2.One * 1.7f);

                // Create bloom at the position of the star, to help it stand out more.
                Color bloomColor = Color.Lerp(Color.LightCoral, Color.Yellow, Main.rand.NextFloat(0.6f));
                StrongBloom bloom = new(Target.Center + starSpawnOffset, Vector2.Zero, bloomColor, 1.9f, 20);
                GeneralParticleHandler.SpawnParticle(bloom);

                NPC.netSpam = 0;
                NPC.netUpdate = true;
            }

            // Release stars.
            if (AttackTimer == redirectTime + hoverTime + starTelegraphTime + starBlastDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    foreach (Vector2 starSpawnOffset in StarSpawnOffsets)
                        NewProjectileBetter(Target.Center + starSpawnOffset, Vector2.Zero, ModContent.ProjectileType<ExplodingStar>(), StarDamage, 0f, -1, 0f, 0.6f);

                    StarSpawnOffsets.Clear();
                    NPC.netSpam = 0;
                    NPC.netUpdate = true;
                }
            }

            if (AttackTimer >= redirectTime + hoverTime + starTelegraphTime * 2f + starBlastDelay + attackTransitionDelay)
            {
                explosionCounter++;
                if (explosionCounter >= explosionCount)
                    SelectNextAttack();
                else
                {
                    AttackTimer = 0f;
                    NPC.netUpdate = true;
                }
            }
        }

        public void DoBehavior_ShootArcingStarburstsFromEye()
        {
            int squintTime = 32;
            int shootDelay = 70;
            int starburstCount = 27;
            int attackTransitionDelay = 78;
            int teleportDelay = DefaultTeleportDelay;
            float starburstArc = ToRadians(396f);
            float starburstShootSpeed = 24f;

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Update teeth.
            TopTeethOffset *= 0.9f;

            // Make the robe's eyes stare at the target.
            RobeEyesShouldStareAtTarget = true;

            // Teleport above the target at first.
            if (AttackTimer <= teleportDelay * 2f)
            {
                NPC.velocity *= 0.8f;
                TeleportVisualsInterpolant = AttackTimer / teleportDelay * 0.5f;
                if (AttackTimer == teleportDelay)
                    TeleportTo(Target.Center - Vector2.UnitY * 600f);
            }

            // Squint briefly and look at the target before attacking.
            float postTeleportAttackTimer = AttackTimer - teleportDelay * 2f;
            float lookAtTargetInterpolant = GetLerpValue(shootDelay * 0.5f, shootDelay - 6f, postTeleportAttackTimer, true);
            PupilScale = Remap(postTeleportAttackTimer, 0f, squintTime, 1f, 0.5f);
            PupilOffset = Vector2.Lerp(PupilOffset, (Target.Center - EyePosition).SafeNormalize(Vector2.UnitY) * 50f, lookAtTargetInterpolant * 0.12f);

            // Make the pupil line telegraph appear as Xeroc looks intently at the player.
            float telegraphDissipateInterpolant = GetLerpValue(shootDelay + starburstCount + 10f, shootDelay + starburstCount, postTeleportAttackTimer, true);
            PupilTelegraphOpacity = lookAtTargetInterpolant * telegraphDissipateInterpolant * 0.5f;
            PupilTelegraphArc = Pow(PupilTelegraphOpacity, 0.6f) * starburstArc;

            // Quickly hover above the player, zipping back and forth, before firing.
            Vector2 sideOfPlayer = Target.Center + (Target.Center.X < NPC.Center.X).ToDirectionInt() * Vector2.UnitX * 800f;
            Vector2 hoverDestination = Vector2.Lerp(Target.Center + GeneralHoverOffset, sideOfPlayer, Pow(lookAtTargetInterpolant, 0.5f));
            Vector2 idealVelocity = (hoverDestination - NPC.Center) * Sqrt(1f - lookAtTargetInterpolant) * 0.14f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.17f);

            // Decide a hover offset if it's unitialized or has been reached.
            if (GeneralHoverOffset == Vector2.Zero || (NPC.WithinRange(Target.Center + GeneralHoverOffset, Target.velocity.Length() * 2f + 90f) && AttackTimer % 20f == 0f))
            {
                // Make the screen rumble a little bit.
                if (Target.Calamity().GeneralScreenShakePower <= 1f)
                {
                    Target.Calamity().GeneralScreenShakePower = 7.5f;
                    SoundEngine.PlaySound(SuddenMovementSound);
                }

                float horizontalOffsetSign = GeneralHoverOffset.X == 0f ? Main.rand.NextFromList(-1f, 1f) : -Sign(GeneralHoverOffset.X);
                GeneralHoverOffset = new Vector2(horizontalOffsetSign * Main.rand.NextFloat(500f, 700f), Main.rand.NextFloat(-550f, -340f));
                NPC.netUpdate = true;
            }

            // Shoot the redirecting starbursts.
            if (postTeleportAttackTimer >= shootDelay && postTeleportAttackTimer <= shootDelay + starburstCount)
            {
                // Create a light explosion and initial spread of starbursts on the first frame.
                if (postTeleportAttackTimer == shootDelay)
                {
                    Target.Calamity().GeneralScreenShakePower = 16f;
                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NewProjectileBetter(PupilPosition, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                        for (int i = 0; i < 21; i++)
                        {
                            Vector2 starburstVelocity = PupilOffset.SafeNormalize(Vector2.UnitY).RotatedBy(TwoPi * i / 21f) * starburstShootSpeed * 0.08f;
                            NewProjectileBetter(PupilPosition, starburstVelocity, ModContent.ProjectileType<Starburst>(), StarburstDamage, 0f);
                        }
                    }

                    XerocKeyboardShader.BrightnessIntensity += 0.67f;
                    ScreenEffectSystem.SetBlurEffect(EyePosition, 0.6f, 24);
                }

                // Release the projectiles.
                float starburstInterpolant = GetLerpValue(0f, starburstCount, postTeleportAttackTimer - shootDelay, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float starburstShootOffsetAngle = Lerp(-starburstArc, starburstArc, starburstInterpolant);
                    Vector2 starburstVelocity = PupilOffset.SafeNormalize(Vector2.UnitY).RotatedBy(starburstShootOffsetAngle) * starburstShootSpeed;
                    NewProjectileBetter(PupilPosition, starburstVelocity, ModContent.ProjectileType<ArcingStarburst>(), StarburstDamage, 0f);
                }

                // Create sound and screen effects.
                ScreenEffectSystem.SetChromaticAberrationEffect(EyePosition, starburstInterpolant * 3f, 10);

                if (Main.rand.NextBool(3))
                    SoundEngine.PlaySound(SunFireballShootSound with { MaxInstances = 100, Volume = 0.5f, Pitch = -0.2f });

                // Create bloom on Xeroc's pupil.
                StrongBloom bloom = new(PupilPosition, Vector2.Zero, Color.Red * starburstInterpolant, 0.8f, 32);
                GeneralParticleHandler.SpawnParticle(bloom);
            }

            // Update universal hands.
            DefaultUniversalHandMotion();

            if (postTeleportAttackTimer >= shootDelay + starburstCount + attackTransitionDelay)
                SelectNextAttack();
        }

        public void DoBehavior_RealityTearDaggers()
        {
            int riseTime = 14;
            int sliceTelegraphTime = 28;
            int screenSliceRate = sliceTelegraphTime + 11;
            int totalHorizontalSlices = 6;
            int totalRadialSlices = 6;
            int totalSlices = totalHorizontalSlices + totalRadialSlices;
            int handWaveTime = 27;
            float sliceTelegraphLength = 2800f;
            float wrappedAttackTimer = AttackTimer % screenSliceRate;
            ref float sliceCounter = ref NPC.ai[2];
            ref float attackTransitionCounter = ref NPC.ai[3];

            // Update teeth.
            PerformTeethChomp(AttackTimer / 45f % 1f);

            // Calculate slice information.
            Vector2 sliceDirection = Vector2.UnitX;
            Vector2 sliceSpawnOffset = Vector2.Zero;
            if (sliceCounter % totalSlices >= totalHorizontalSlices)
            {
                sliceDirection = sliceDirection.RotatedBy(TwoPi * (sliceCounter - totalHorizontalSlices) / totalRadialSlices);
                sliceSpawnOffset += sliceDirection.RotatedBy(PiOver2) * 400f;
            }

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Move into the background.
            if (AttackTimer <= riseTime)
                ZPosition = Pow(AttackTimer / riseTime, 1.6f) * 2.4f;

            // Calculate the background hover position.
            float hoverHorizontalWaveSine = Sin(TwoPi * AttackTimer / 96f);
            float hoverVerticalWaveSine = Sin(TwoPi * AttackTimer / 120f);
            Vector2 hoverDestination = Target.Center + new Vector2(Target.velocity.X * 14.5f, ZPosition * -40f - 200f);
            hoverDestination.X += hoverHorizontalWaveSine * ZPosition * 40f;
            hoverDestination.Y -= hoverVerticalWaveSine * ZPosition * 8f;

            // Stay above the target while in the background.
            NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.03f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hoverDestination - NPC.Center) * 0.07f, 0.06f);

            // Create hands.
            if (AttackTimer == 1f)
            {
                for (int i = TotalUniversalHands; i < totalRadialSlices; i++)
                {
                    Vector2 handOffset = (TwoPi * i / totalRadialSlices).ToRotationVector2() * NPC.scale * 400f;
                    if (Abs(handOffset.X) <= 0.001f)
                        handOffset.X = 0f;

                    ConjureHandsAtPosition(NPC.Center + handOffset, sliceDirection * 3f, false, Math.Sign(handOffset.X));
                }

                // Play mumble sounds.
                PerformMumble();
            }

            // Operate hands that move in the direction of the slice.
            if (Hands.Any())
            {
                // Calculate hand orientations.
                int handToMoveIndex = (int)sliceCounter % Hands.Count;
                Vector2 handMoveDirection = sliceDirection;
                if (sliceCounter < totalHorizontalSlices)
                {
                    handToMoveIndex = 0;
                    if (sliceCounter % 2 == 0f)
                    {
                        handToMoveIndex = 3;
                        handMoveDirection *= -1f;
                    }
                }
                else if (sliceCounter < totalSlices)
                {
                    handToMoveIndex++;
                    handMoveDirection = handMoveDirection.RotatedBy(TwoPi / totalRadialSlices);
                }
                else
                    handToMoveIndex = -1;

                // Update the hands.
                for (int i = 0; i < Hands.Count; i++)
                {
                    XerocHand hand = Hands[i];
                    Vector2 handDestination = NPC.Center + handMoveDirection * NPC.scale * 900f;
                    Vector2 hoverOffset = (TwoPi * i / Hands.Count).ToRotationVector2() * NPC.scale * new Vector2(500f, 350f);
                    if (i != handToMoveIndex)
                    {
                        if (Abs(hoverOffset.X) <= TeleportVisualsAdjustedScale.X * 400f)
                            hoverOffset.X = Sign(hoverOffset.X) * TeleportVisualsAdjustedScale.X * 400f;

                        handDestination = NPC.Center + hoverOffset;
                    }

                    hand.Center = Vector2.Lerp(hand.Center, handDestination, wrappedAttackTimer / handWaveTime * 0.3f + 0.02f);

                    DefaultHandDrift(hand, handDestination, 1.2f);
                    hand.Rotation = ((hand.Center - EyePosition) * new Vector2(1f, 0.1f)).ToRotation() - PiOver2;
                    hand.RobeDirection = (i % 2 == 1).ToDirectionInt();
                    if (sliceCounter >= totalSlices)
                        hand.RobeDirection *= -1;

                    hand.Frame = 2;
                    hand.ShouldOpen = false;
                }
            }

            // Create slices.
            if (wrappedAttackTimer == handWaveTime && AttackTimer >= riseTime + 1f && sliceCounter < totalSlices)
            {
                // Create a reality tear.
                SoundEngine.PlaySound(RealityTearSound with { Volume = 0.6f });

                if (sliceCounter >= totalHorizontalSlices)
                    sliceDirection = sliceDirection.RotatedBy(Pi / totalRadialSlices);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(Target.Center - sliceDirection * sliceTelegraphLength * 0.5f + sliceSpawnOffset, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), ScreenSliceDamage, 0f, -1, sliceTelegraphTime, sliceTelegraphLength);

                XerocKeyboardShader.BrightnessIntensity += 0.6f;

                sliceCounter++;
                NPC.netUpdate = true;
            }

            // Return to the foreground and destroy hands after doing the slices.
            if (sliceCounter >= totalSlices)
            {
                ZPosition = Lerp(ZPosition, 0f, 0.06f);

                // Destroy the hands after enough time has passed.
                if (attackTransitionCounter == 25f)
                    DestroyAllHands();

                attackTransitionCounter++;
                if (attackTransitionCounter >= 84f)
                    SelectNextAttack();
            }
        }

        public void DoBehavior_StarManagement()
        {
            int starGrowTime = ControlledStar.GrowToFullSizeTime;
            int attackDelay = starGrowTime + 40;
            int flareShootCount = 5;
            int shootTime = 450;
            int starburstReleaseRate = 35;
            int starburstCount = 16;
            float starburstStartingSpeed = 0.6f;
            ref float flareShootCounter = ref NPC.ai[2];

            // Make things faster in successive phases.
            if (CurrentPhase >= 1)
            {
                attackDelay -= 10;
                flareShootCount--;
                shootTime -= 90;
            }

            // Make the robe's eyes stare at the target.
            RobeEyesShouldStareAtTarget = true;

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Update teeth.
            PerformTeethChomp(AttackTimer / 45f % 1f);

            // Create a suitable star and two hands on the first frame.
            // The star will give the appearance of actually coming from the background.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitX * Target.direction * 400f);

                Vector2 starSpawnPosition = NPC.Center + new Vector2(300f, -350f) * TeleportVisualsAdjustedScale;
                CreateTwinkle(starSpawnPosition, Vector2.One * 1.3f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(starSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ControlledStar>(), 0, 0f);

                // Play mumble sounds.
                PerformMumble();
                return;
            }

            if (AttackTimer <= 48f)
            {
                for (int i = 0; i < Hands.Count; i++)
                    Hands[i].ShouldOpen = false;
            }
            if (AttackTimer == 52f)
            {
                SoundEngine.PlaySound(FingerSnapSound with { Volume = 4f });
                XerocKeyboardShader.BrightnessIntensity += 0.45f;
            }

            // Verify that a star actually exists. If not, terminate this attack immediately.
            List<Projectile> stars = AllProjectilesByID(ModContent.ProjectileType<ControlledStar>()).ToList();
            if (!stars.Any())
            {
                DestroyAllHands();
                flareShootCounter = 0f;
                AttackTimer = 0f;
                SelectNextAttack();
                return;
            }

            // Drift towards the target.
            if (AttackTimer >= attackDelay)
            {
                if (NPC.velocity.Length() > 4f)
                    NPC.velocity *= 0.8f;

                float hoverSpeedInterpolant = Remap(NPC.Distance(Target.Center), 750f, 1800f, 0.003f, 0.04f);
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center, hoverSpeedInterpolant);
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(Target.Center) * 4f, 0.1f);
            }
            else
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 700f, -250f);
                Vector2 idealVelocity = (hoverDestination - NPC.Center) * 0.12f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.13f);
            }

            // Find the star to control.
            Projectile star = stars.First();

            // Crate a light wave if the star released lasers.
            if (NPC.ai[3] == 1f)
            {
                SoundEngine.PlaySound(ExplosionTeleportSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(star.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                XerocKeyboardShader.BrightnessIntensity += 0.72f;
                NPC.ai[3] = 0f;
                NPC.netUpdate = true;
            }

            // Update hand positions.
            DefaultHandDrift(Hands[0], NPC.Center - Vector2.UnitX * TeleportVisualsAdjustedScale * 320f, 1.8f);
            DefaultHandDrift(Hands[1], NPC.Center + Vector2.UnitX * TeleportVisualsAdjustedScale * 320f, 1.8f);
            Hands[0].UsePalmForm = false;
            Hands[1].UsePalmForm = false;
            Hands[0].Rotation = Hands[0].Rotation.AngleLerp(PiOver2, 0.1f);
            Hands[1].Rotation = Hands[1].Rotation.AngleLerp(-0.67f - PiOver2, 0.1f);
            Hands[1].DirectionOverride = 1;
            Hands[0].RobeDirection = -1;
            Hands[1].RobeDirection = 1;

            // Hold the star in Xeroc's right hand.
            float verticalOffset = Convert01To010(GetLerpValue(0f, starGrowTime, AttackTimer, true)) * 175f;
            Vector2 starPosition = Hands[1].Center + Vector2.UnitY * (verticalOffset - 360f) * TeleportVisualsAdjustedScale;
            star.Center = starPosition;

            // Release accelerating bursts of starbursts over time.
            if (AttackTimer >= attackDelay && AttackTimer % starburstReleaseRate == 0f)
            {
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot with { Pitch = -0.4f, MaxInstances = 5 }, star.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int starburstID = ModContent.ProjectileType<Starburst>();
                    int starburstCounter = (int)Round(AttackTimer / starburstReleaseRate);
                    float shootOffsetAngle = starburstCounter % 2 == 0 ? Pi / starburstCount : 0f;
                    if (!NPC.WithinRange(Target.Center, 1450f))
                    {
                        if (Main.rand.NextBool())
                            starburstID = ModContent.ProjectileType<ArcingStarburst>();
                        starburstStartingSpeed *= 5.6f;
                    }

                    for (int i = 0; i < starburstCount; i++)
                    {
                        Vector2 starburstVelocity = star.SafeDirectionTo(Target.Center).RotatedBy(TwoPi * i / starburstCount + shootOffsetAngle) * starburstStartingSpeed;
                        NewProjectileBetter(star.Center + starburstVelocity * 8f, starburstVelocity, starburstID, StarburstDamage, 0f);
                    }
                    for (int i = 0; i < starburstCount / 2; i++)
                    {
                        Vector2 starburstVelocity = star.SafeDirectionTo(Target.Center).RotatedBy(TwoPi * i / starburstCount / 2f + shootOffsetAngle) * starburstStartingSpeed * 0.6f;
                        NewProjectileBetter(star.Center + starburstVelocity * 8f, starburstVelocity, starburstID, StarburstDamage, 0f);
                    }
                }
            }

            // Release telegraphed solar flare lasers over time. The quantity of lasers increases as time goes on.
            if (AttackTimer >= attackDelay && flareShootCounter < flareShootCount - 1f && !AllProjectilesByID(ModContent.ProjectileType<TelegraphedStarLaserbeam>()).Any())
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Create the flares.
                    int flareCount = (int)(flareShootCounter * 2f) + 2;
                    int flareTelegraphTime = (int)Remap(AttackTimer - attackDelay, 0f, 300f, 23f, 60f) + TelegraphedStarLaserbeam.LaserShootTime;
                    float flareSpinDirection = (flareShootCounter % 2f == 0f).ToDirectionInt();
                    float flareSpinCoverage = PiOver2 * flareSpinDirection;
                    Vector2 directionToTarget = star.SafeDirectionTo(Target.Center);
                    for (int i = 0; i < flareCount; i++)
                    {
                        Vector2 flareDirection = directionToTarget.RotatedBy(TwoPi * i / flareCount);
                        NewProjectileBetter(star.Center, flareDirection, ModContent.ProjectileType<TelegraphedStarLaserbeam>(), StarDamage, 0f, -1, flareTelegraphTime, flareSpinCoverage / flareTelegraphTime);
                    }

                    flareShootCounter++;
                    NPC.netUpdate = true;
                }
            }

            if (AttackTimer >= attackDelay + shootTime)
                SelectNextAttack();
        }

        public void DoBehavior_PortalLaserBarrages()
        {
            int closeRedirectTime = 25;
            int farRedirectTime = 16;
            int horizontalChargeTime = 50;
            int portalExistTime = 40;
            int chargeCount = 4;
            int laserShootTime = 32;
            ref float chargeDirection = ref NPC.ai[2];
            ref float chargeCounter = ref NPC.ai[3];

            bool verticalCharges = chargeCounter % 2f == 1f;
            float laserAngularVariance = verticalCharges ? 0.02f : 0.05f;
            float fastChargeSpeedInterpolant = verticalCharges ? 0.184f : 0.13f;
            int portalReleaseRate = verticalCharges ? 3 : 4;

            // Flap wings.
            UpdateWings(AttackTimer / 42f % 1f);

            // Update teeth.
            PerformTeethChomp(AttackTimer / 45f % 1f);

            // Update universal hands.
            DefaultUniversalHandMotion();

            // Look at the player.
            PupilOffset = Vector2.Lerp(PupilOffset, (Target.Center - EyePosition).SafeNormalize(Vector2.UnitY) * 50f, 0.2f);

            // Move to the side of the player.
            if (AttackTimer <= closeRedirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 350f, 300f);

                // Teleport to the hover destination on the first frame.
                if (AttackTimer == 1f)
                {
                    TeleportTo(hoverDestination);
                    ShakeScreen(NPC.Center, 8f);
                }

                // Fade in.
                NPC.Opacity = GetLerpValue(3f, 10f, AttackTimer, true);

                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.39f);
                NPC.velocity *= 0.85f;
                return;
            }

            // Move back a bit from the player.
            if (AttackTimer <= closeRedirectTime + farRedirectTime)
            {
                float flySpeed = Remap(AttackTimer - closeRedirectTime, 0f, farRedirectTime - 4f, 45f, 80f);
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 1450f, -Target.velocity.Y * 12f - 408f);
                if (verticalCharges)
                    hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 960f - Target.velocity.X * 12f, 850f);

                // Handle movement.
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.026f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(hoverDestination) * flySpeed, 0.15f);

                if (AttackTimer == closeRedirectTime + farRedirectTime - 1f)
                {
                    chargeDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                    if (verticalCharges)
                        chargeDirection = -1f;

                    NPC.velocity.Y *= 0.6f;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Perform the horizontal charge.
            if (AttackTimer <= closeRedirectTime + farRedirectTime + horizontalChargeTime)
            {
                // Release portals. If the charge is really close to the target they appear regardless of the timer, to ensure that they can't just stand still.
                bool forcefullySpawnPortal = (Distance(NPC.Center.X, Target.Center.X) <= 90f && !verticalCharges) || (Distance(NPC.Center.Y, Target.Center.Y) <= 55f && verticalCharges);
                if (Main.netMode != NetmodeID.MultiplayerClient && (AttackTimer % portalReleaseRate == 1f || forcefullySpawnPortal) && AttackTimer >= closeRedirectTime + farRedirectTime + 5f)
                {
                    int remainingChargeTime = horizontalChargeTime - (int)(AttackTimer - closeRedirectTime - farRedirectTime);
                    int fireDelay = remainingChargeTime + 14;
                    float portalScale = Main.rand.NextFloat(0.54f, 0.67f);

                    Vector2 portalDirection = ((verticalCharges ? Vector2.UnitX : Vector2.UnitY) * NPC.SafeDirectionTo(Target.Center)).SafeNormalize(Vector2.UnitY).RotatedByRandom(laserAngularVariance);

                    // Summon the portal and shoot the telegraph for the laser.
                    NewProjectileBetter(NPC.Center + portalDirection * Main.rand.NextFloatDirection() * 20f, portalDirection, ModContent.ProjectileType<LightPortal>(), 0, 0f, -1, portalScale, portalExistTime + remainingChargeTime + 15, fireDelay);
                    NewProjectileBetter(NPC.Center, portalDirection, ModContent.ProjectileType<TelegraphedPortalLaserbeam>(), LightLaserbeamDamage, 0f, -1, fireDelay, laserShootTime);

                    // Spawn a second telegraph laser in the opposite direction if a portal was summoned due to being close to the target.
                    // This is done to prevent just flying up/forward to negate the attack.
                    if (forcefullySpawnPortal)
                    {
                        portalDirection *= -1f;
                        NewProjectileBetter(NPC.Center, portalDirection, ModContent.ProjectileType<TelegraphedPortalLaserbeam>(), LightLaserbeamDamage, 0f, -1, fireDelay, laserShootTime);
                    }
                }

                // Go FAST.
                Vector2 chargeDirectionVector = verticalCharges ? Vector2.UnitY * chargeDirection : Vector2.UnitX * chargeDirection;
                NPC.velocity = Vector2.Lerp(NPC.velocity, chargeDirectionVector * 150f, fastChargeSpeedInterpolant);

                return;
            }

            chargeCounter++;
            if (chargeCounter >= chargeCount)
                SelectNextAttack();
            else
            {
                AttackTimer = 0f;
                NPC.netAlways = true;
            }
        }

        public void DoBehavior_StarManagement_CrushIntoQuasar()
        {
            int redirectTime = 45;
            int starPressureTime = 60;
            int supernovaDelay = 30;
            int pressureArmsCount = 9;
            int plasmaShootDelay = 60;
            int plasmaShootRate = 2;
            int plasmaSkipChance = 0;
            int plasmaShootTime = Supernova.Lifetime - 90;
            float plasmaShootSpeed = 9f;
            float handOrbitOffset = 100f;
            float pressureInterpolant = GetLerpValue(redirectTime, redirectTime + starPressureTime, AttackTimer, true);

            // Make things faster in successive phases.
            if (CurrentPhase >= 1)
            {
                plasmaShootRate = 1;
                plasmaSkipChance = 3;
            }

            Projectile star = null;
            Projectile quasar = null;
            List<Projectile> stars = AllProjectilesByID(ModContent.ProjectileType<ControlledStar>()).ToList();
            List<Projectile> quasars = AllProjectilesByID(ModContent.ProjectileType<Quasar>()).ToList();
            if (stars.Any())
                star = stars.First();
            if (quasars.Any())
                quasar = quasars.First();

            if (quasar is null && star is null)
            {
                SelectNextAttack();
                return;
            }

            // Conjure hands and destroy leftover starbursts on the first frame.
            if (AttackTimer == 1f)
            {
                int arcingStarburstID = ModContent.ProjectileType<ArcingStarburst>();
                int starburstID = ModContent.ProjectileType<Starburst>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || (p.type != arcingStarburstID && p.type != starburstID))
                        continue;

                    p.Kill();
                }

                for (int i = 0; i < pressureArmsCount; i++)
                    ConjureHandsAtPosition(star.Center, Vector2.Zero, false);
                return;
            }

            // Make the star slowly attempt to drift below the player.
            if (star is not null)
            {
                Vector2 starHoverDestination = Target.Center + Vector2.UnitY * 400f;
                star.velocity = Vector2.Lerp(star.velocity, star.SafeDirectionTo(starHoverDestination) * 8f, 0.04f);
            }

            // Have Xeroc rapidly attempt to hover above the player, with a bit of a horizontal offset at first.
            if (AttackTimer < redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 300f, -250f);
                Vector2 idealVelocity = (hoverDestination - NPC.Center) * 0.12f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.15f);

                if (star is not null)
                    star.Center = Vector2.Lerp(star.Center, NPC.Center + Vector2.UnitY * TeleportVisualsAdjustedScale * 350f, 0.15f);
            }
            else
            {
                // Teleport away from the player on the first frame.
                if (AttackTimer == redirectTime)
                {
                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                    XerocKeyboardShader.BrightnessIntensity += 0.6f;
                }
                NPC.Center = Target.Center + Vector2.UnitY * 1400f;

                // Make hands close in on the star, as though collapsing it.
                if (star is not null)
                {
                    float starScale = Remap(pressureInterpolant, 0.1f, 0.8f, ControlledStar.MaxScale, 0.8f);
                    star.scale = starScale;
                    star.ai[1] = pressureInterpolant;
                    handOrbitOffset += Sin(AttackTimer / 4f) * pressureInterpolant * 8f;
                }

                // Make remaining universal hands invisible.
                if (AttackTimer >= redirectTime + starPressureTime + supernovaDelay)
                {
                    for (int i = 0; i < Hands.Count; i++)
                        Hands[i].Opacity = 0f;
                }
            }

            // Make hands circle the star.
            for (int i = 0; i < Hands.Count; i++)
            {
                if (star is not null)
                {
                    DefaultHandDrift(Hands[i], star.Center + (TwoPi * i / Hands.Count + AttackTimer / 18f).ToRotationVector2() * (star.scale * handOrbitOffset + 50f), 1.4f); ;
                    Hands[i].Rotation = Hands[i].Center.AngleTo(star.Center) - PiOver2;
                }
                Hands[i].Center += Main.rand.NextVector2Circular(10f, 10f) * Pow(pressureInterpolant, 2f);
                Hands[i].UseRobe = false;
            }

            // Destroy the star and create a supernova and quasar.
            if (AttackTimer == redirectTime + starPressureTime + supernovaDelay)
            {
                // Apply sound and visual effects.
                SoundEngine.PlaySound(SupernovaSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(star.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                    NewProjectileBetter(star.Center, Vector2.Zero, ModContent.ProjectileType<Supernova>(), 0, 0f);
                    NewProjectileBetter(star.Center, Vector2.Zero, ModContent.ProjectileType<Quasar>(), QuasarDamage, 0f);
                }
                Target.Calamity().GeneralScreenShakePower = 18f;
                ScreenEffectSystem.SetChromaticAberrationEffect(star.Center, 1.5f, 54);
                XerocKeyboardShader.BrightnessIntensity = 1f;

                star.Kill();

                // Delete the hands.
                DestroyAllHands();

                NPC.netUpdate = true;
            }

            // Create plasma around the player that converges into the black quasar.
            if (quasar is not null && AttackTimer >= redirectTime + starPressureTime + supernovaDelay + plasmaShootDelay && AttackTimer <= redirectTime + starPressureTime + supernovaDelay + plasmaShootDelay + plasmaShootTime && AttackTimer % plasmaShootRate == 0f)
            {
                if (plasmaSkipChance >= 1 && Main.rand.NextBool(plasmaSkipChance))
                    return;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 plasmaSpawnPosition = quasar.Center + (TwoPi * AttackTimer / 30f).ToRotationVector2() * (quasar.Distance(Target.Center) + Main.rand.NextFloat(600f, 700f));
                    Vector2 plasmaVelocity = (quasar.Center - plasmaSpawnPosition).SafeNormalize(Vector2.UnitY) * plasmaShootSpeed;
                    while (Target.WithinRange(plasmaSpawnPosition, 1040f))
                        plasmaSpawnPosition -= plasmaVelocity;

                    NewProjectileBetter(plasmaSpawnPosition, plasmaVelocity, ModContent.ProjectileType<ConvergingSupernovaEnergy>(), SupernovaEnergyDamage, 0f);
                }
            }
        }
    }
}
