using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc.Projectiles;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;
using static NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers.XerocSky;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        public void DoBehavior_VergilScreenSlices()
        {
            int sliceShootDelay = 40;
            int sliceReleaseRate = 4;
            int sliceReleaseCount = 9;
            int fireDelay = 10;
            float sliceLength = 3200f;

            // Make the attack faster in successive phases.
            if (CurrentPhase >= 2)
            {
                sliceShootDelay -= 7;
                sliceReleaseCount--;
            }

            int sliceReleaseTime = sliceReleaseRate * sliceReleaseCount + 25;
            ref float sliceCounter = ref NPC.ai[2];

            // Flap wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Update universal hands.
            DefaultUniversalHandMotion();

            if (AttackTimer <= sliceShootDelay)
            {
                // Play mumble sounds.
                if (AttackTimer == 1f)
                    PerformMumble();

                // Hover above the target at first.
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 442f;
                NPC.velocity = (hoverDestination - NPC.Center) * 0.1f;

                // Teleport away after hovering.
                if (AttackTimer >= sliceShootDelay - 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                    TeleportTo(Target.Center + Vector2.UnitY * 2000f);
                }

                // Dim the background for suspense.
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.032f, 0.5f, 1f);

                return;
            }

            // Stay invisible.
            NPC.Opacity = 0f;

            // Release slice telegraphs around the player.
            if (AttackTimer % sliceReleaseRate == 0f && sliceCounter < sliceReleaseCount)
            {
                SoundEngine.PlaySound(SliceSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int telegraphTime = sliceReleaseTime - (int)(AttackTimer - sliceShootDelay) + (int)sliceCounter * 2 + fireDelay;
                    Vector2 sliceSpawnCenter = Target.Center + Main.rand.NextVector2Unit() * (sliceCounter + 35f + Main.rand.NextFloat(600f)) + Target.velocity * 8f;
                    if (sliceCounter == 0f)
                        sliceSpawnCenter = Target.Center + Main.rand.NextVector2Circular(10f, 10f);

                    Vector2 sliceDirection = new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-6f, 6f)).SafeNormalize(Vector2.UnitX);
                    NewProjectileBetter(sliceSpawnCenter - sliceDirection * sliceLength * 0.5f, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice2>(), ScreenSliceDamage, 0f, -1, telegraphTime, sliceLength);
                }

                sliceCounter++;
            }

            // Slice the screen.
            if (AttackTimer == sliceShootDelay + sliceReleaseTime + fireDelay)
            {
                // Calculate the center of the slices.
                List<LineSegment> lineSegments = new();
                List<Projectile> slices = AllProjectilesByID(ModContent.ProjectileType<TelegraphedScreenSlice2>()).ToList();
                for (int i = 0; i < slices.Count; i++)
                {
                    Vector2 start = slices[i].Center;
                    Vector2 end = start + slices[i].velocity * slices[i].ModProjectile<TelegraphedScreenSlice2>().LineLength;
                    lineSegments.Add(new(start, end));
                }

                ScreenShatterSystem.CreateShatterEffect(lineSegments.ToArray(), 2);
                SoundEngine.PlaySound(ExplosionTeleportSound);
                XerocKeyboardShader.BrightnessIntensity = 1f;
                Target.Calamity().GeneralScreenShakePower = 15f;
            }

            // Make the background come back.
            if (AttackTimer >= sliceShootDelay + sliceReleaseTime + fireDelay)
            {
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity + 0.08f, 0f, 1f);
                RadialScreenShoveSystem.Start(Target.Center - Vector2.UnitY * 400f, 20);
                NPC.Opacity = 1f;
            }

            // Create some screen burn marks.
            if (AttackTimer == sliceShootDelay + sliceReleaseTime + fireDelay + 16f)
                LocalScreenSplitBurnAfterimageSystem.TakeSnapshot(180);

            if (AttackTimer >= sliceShootDelay + sliceReleaseTime + fireDelay + 32f)
                SelectNextAttack();

            // Stay invisible.
            NPC.Center = Target.Center + Vector2.UnitY * 2000f;
        }

        public void DoBehavior_PunchesWithScreenSlices()
        {
            int redirectTime = 11;
            int handCount = 10;
            int handSummonRate = 4;
            int handSummonTime = handCount * handSummonRate;
            float maxHandSpinSpeed = ToRadians(0.36f);
            float handMoveSpeedFactor = 2.8f;
            Vector2 handOffsetRadius = new(332f, 396f);
            Vector2 idealHandOffsetFromPlayer1 = Vector2.Zero;
            Vector2 idealHandOffsetFromPlayer2 = Vector2.Zero;
            ref float handSpinAngularOffset = ref NPC.ai[2];
            ref float usedHandIndex = ref NPC.ai[3];

            // Flap wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Update teeth.
            PerformTeethChomp(AttackTimer / 45f % 1f);

            // Hover to the top left/right of the target at first.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 450f, -360f);
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.27f);
                NPC.velocity *= 0.75f;
                usedHandIndex = -1f;

                // Initialize the punch offset angle.
                if (AttackTimer == 1f)
                {
                    PunchOffsetAngle = Main.rand.NextFloat(-0.81f, 0.81f);
                    NPC.netUpdate = true;
                }
            }

            // Hover near the target and conjure hands.
            else if (AttackTimer <= redirectTime + handSummonTime)
            {
                // Summon hands.
                if (AttackTimer % handSummonRate == 0f)
                {
                    Vector2 handOffset = (TwoPi * usedHandIndex / handCount + handSpinAngularOffset).ToRotationVector2() * handOffsetRadius;
                    ConjureHandsAtPosition(NPC.Center + handOffset, Vector2.Zero, true);
                }

                // Hover near the target.
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 450f, -360f);
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 23f, 0.6f);

                // Prepare the hands for attacking.
                if (AttackTimer == redirectTime + handSummonTime)
                {
                    usedHandIndex = 2f;
                    NPC.netUpdate = true;
                }
            }

            // Hover above the player while the hands attack.
            else
            {
                int handEnergyChargeUpTime = 45;

                int handArcSpinTime = 18;
                int handArcTangentMovementTime = 10;
                int handArcPunchTime = handArcSpinTime + handArcTangentMovementTime;

                int handRepositionTime = 30;
                int handCollideTime = 14;
                int handCollideStunTime = 88;
                int wrappedHandAttackTimer = (int)(AttackTimer - redirectTime - handSummonTime) % (handEnergyChargeUpTime + handArcPunchTime + handRepositionTime + handCollideTime + handCollideStunTime);
                int screenSliceCount = 6;
                int sliceTelegraphDelay = 36;
                bool doArcPunch = true;

                if (!doArcPunch)
                {
                    handEnergyChargeUpTime = 0;
                    handArcPunchTime = 0;
                }

                XerocHand leftHand = Hands[(int)usedHandIndex];
                XerocHand rightHand = Hands[(int)((usedHandIndex + Hands.Count / 2) % Hands.Count)];

                // Hover above the target.
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 580f, -360f);
                Vector2 idealVelocity = (hoverDestination - NPC.Center) * 0.06f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.06f);

                // Make the first hand wait and reel back in anticipation of the arc punch, while the other one silently waits to the side.
                if (wrappedHandAttackTimer <= handEnergyChargeUpTime && doArcPunch)
                {
                    float anticipationReelBackDistance = Pow(GetLerpValue(-15f, -3f, wrappedHandAttackTimer - handEnergyChargeUpTime, true), 1.7f) * 200f;
                    float handHoverOffsetAngle = Sin(Pi * wrappedHandAttackTimer / handEnergyChargeUpTime) * 0.51f;
                    Vector2 generalHandOffset = Vector2.UnitX.RotatedBy(handHoverOffsetAngle + PunchOffsetAngle) * 320f;
                    Vector2 punchingHandOffset = generalHandOffset + generalHandOffset.SafeNormalize(Vector2.UnitY) * anticipationReelBackDistance - Vector2.UnitY * anticipationReelBackDistance * 0.5f;

                    idealHandOffsetFromPlayer1 = punchingHandOffset;
                    idealHandOffsetFromPlayer2 = -generalHandOffset;

                    // Create charge animation particles from the punching hand.
                    Vector2 punchingHandCenter = leftHand.Center;
                    if (wrappedHandAttackTimer % 10f == 0f)
                    {
                        SoundEngine.PlaySound(SunFireballShootSound, punchingHandCenter);
                        Color energyColor = Color.Lerp(Color.Yellow, Color.IndianRed, Main.rand.NextFloat(0.25f, 0.825f));
                        PulseRing ring = new(punchingHandCenter, Vector2.Zero, energyColor, 3.2f, 0f, 30);
                        GeneralParticleHandler.SpawnParticle(ring);

                        StrongBloom bloom = new(punchingHandCenter, Vector2.Zero, energyColor, 1f, 15);
                        GeneralParticleHandler.SpawnParticle(bloom);
                    }

                    leftHand.Rotation = (Target.Center - leftHand.Center).ToRotation() - PiOver2;
                    rightHand.Rotation = (Target.Center - rightHand.Center).ToRotation() - PiOver2;

                    // Decide the punch destination right before it happens.
                    if (wrappedHandAttackTimer == handEnergyChargeUpTime - 1f)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce with
                        {
                            Volume = 8f,
                            MaxInstances = 10,
                            Pitch = -0.5f
                        });

                        ScreenEffectSystem.SetBlurEffect(punchingHandCenter, 0.67f, 10);
                        RadialScreenShoveSystem.Start(punchingHandCenter, 20);
                        PunchDestination = Vector2.Lerp(leftHand.Center, Target.Center, 0.89f);
                        leftHand.ClearPositionCache();
                        leftHand.TrailOpacity = 1f;

                        NPC.netUpdate = true;
                    }
                }

                // Perform the arc punch.
                else if (wrappedHandAttackTimer <= handEnergyChargeUpTime + handArcPunchTime && doArcPunch)
                {
                    // Perform arc movements before moving in a straight, tangent line.
                    // The arc works by performing slight rotations on offsets from the hand's current position.
                    // Left unaltered, the rotations result in the effect changing nothing, but when angles are not zero, the effect
                    // angularly approaches the ideal destination over time.
                    float punchAngularVelocity = Pi / handArcPunchTime * 6f;
                    Vector2 rotationalOffset = (PunchDestination - leftHand.Center).RotatedBy(punchAngularVelocity);
                    idealHandOffsetFromPlayer1 = -Target.Center + PunchDestination + rotationalOffset;
                    leftHand.Rotation = rotationalOffset.ToRotation() - PiOver2;

                    // Go in a tangent line after enough time has passed.
                    if (wrappedHandAttackTimer >= handEnergyChargeUpTime + handArcSpinTime)
                    {
                        idealHandOffsetFromPlayer1 = leftHand.Center + leftHand.Velocity;
                        leftHand.Rotation = leftHand.Velocity.ToRotation() - PiOver2;
                        leftHand.TrailOpacity = Clamp(leftHand.TrailOpacity - 0.3f, 0f, 1f);
                        leftHand.CanDoDamage = true;
                        NPC.netUpdate = true;
                    }

                    // Make the punching hand release fire behind it.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 fireVelocity = leftHand.Velocity * -0.15f + Main.rand.NextVector2Circular(4f, 4f);
                        Color fireColor = Color.Lerp(Color.IndianRed, Color.Yellow, Main.rand.NextFloat(0.75f)) * 0.4f;
                        HeavySmokeParticle fire = new(leftHand.Center + Main.rand.NextVector2Circular(12f, 12f), fireVelocity, fireColor, 12, 0.9f, 1f, 0f, true);
                        GeneralParticleHandler.SpawnParticle(fire);
                    }

                    // Have the other hand go away.
                    idealHandOffsetFromPlayer2 = -Vector2.UnitX * Remap(wrappedHandAttackTimer - handEnergyChargeUpTime, 0f, 16f, 320f, 1050f);

                    // Make hands not move as insanely quick.
                    handMoveSpeedFactor = 1.55f;
                }

                // Reposition in preparation of the collision punch.
                else if (wrappedHandAttackTimer <= handEnergyChargeUpTime + handArcPunchTime + handRepositionTime)
                {
                    // Calculate hand offset information.
                    ulong offsetAngleSeed = (ulong)(usedHandIndex + 74f);
                    float handHoverOffsetAngle = Lerp(-0.72f, 0.72f, RandomFloat(ref offsetAngleSeed)) + PunchOffsetAngle;
                    float handHoverOffsetDistance = Lerp(330f, 700f, Pow(GetLerpValue(0f, handRepositionTime, wrappedHandAttackTimer - handEnergyChargeUpTime - handArcPunchTime, true), 2.3f));
                    Vector2 handOffset = Vector2.UnitX.RotatedBy(handHoverOffsetAngle) * handHoverOffsetDistance;
                    idealHandOffsetFromPlayer1 = handOffset;
                    idealHandOffsetFromPlayer2 = -handOffset;

                    // Completely disable the trail opacity and damage from the previous state.
                    leftHand.TrailOpacity = 0f;
                    leftHand.CanDoDamage = false;

                    // Make both hands look at the target.
                    leftHand.Rotation = (Target.Center - leftHand.Center).ToRotation() - PiOver2;
                    rightHand.Rotation = (Target.Center - rightHand.Center).ToRotation() - PiOver2;

                    handMoveSpeedFactor *= GetLerpValue(0f, 20f, wrappedHandAttackTimer - handEnergyChargeUpTime - handArcPunchTime, true);
                    PunchDestination = Target.Center;
                }

                // Make the hands punch each other and create slices.
                else if (wrappedHandAttackTimer <= handEnergyChargeUpTime + handArcPunchTime + handRepositionTime + handCollideTime)
                {
                    Vector2 handCenter = (leftHand.Center + rightHand.Center) * 0.5f;
                    idealHandOffsetFromPlayer1 = -Target.Center + PunchDestination + handCenter - leftHand.Center;
                    idealHandOffsetFromPlayer2 = -Target.Center + PunchDestination + handCenter - rightHand.Center;

                    // Reset the trail caches on the first frame.
                    if (wrappedHandAttackTimer == handEnergyChargeUpTime + handArcPunchTime + handRepositionTime + 1f)
                    {
                        leftHand.ClearPositionCache();
                        rightHand.ClearPositionCache();
                    }

                    // Create slices.
                    if (wrappedHandAttackTimer == handEnergyChargeUpTime + handArcPunchTime + handRepositionTime + 11f)
                    {
                        SoundEngine.PlaySound(SliceSound);
                        SoundEngine.PlaySound(ExplosionTeleportSound);
                        leftHand.Velocity *= -0.3f;
                        rightHand.Velocity *= -0.3f;
                        Target.Calamity().GeneralScreenShakePower = 11f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 impactPoint = (leftHand.Center + rightHand.Center) * 0.5f;
                            NewProjectileBetter(impactPoint, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                            float angleToTarget = Target.AngleFrom(impactPoint);
                            for (int i = 0; i < screenSliceCount; i++)
                            {
                                Vector2 screenSliceDirection = (TwoPi * i / screenSliceCount + angleToTarget).ToRotationVector2();
                                NewProjectileBetter(impactPoint - screenSliceDirection * 2000f, screenSliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, sliceTelegraphDelay, 4000f);
                            }
                            leftHand.TrailOpacity = 0f;
                            rightHand.TrailOpacity = 0f;
                        }
                    }

                    // Make both hands look into their general direction and use the trail while moving.
                    if (wrappedHandAttackTimer < handEnergyChargeUpTime + handArcPunchTime + handRepositionTime + 7f)
                    {
                        leftHand.Rotation = leftHand.Velocity.ToRotation() - PiOver2;
                        rightHand.Rotation = rightHand.Velocity.ToRotation() - PiOver2;

                        // Make the trails draw.
                        leftHand.TrailOpacity = 1f;
                        rightHand.TrailOpacity = 1f;
                    }
                }
                else
                {
                    leftHand.Velocity = leftHand.Velocity.ClampMagnitude(0f, 6f) * 0.96f;
                    rightHand.Velocity = rightHand.Velocity.ClampMagnitude(0f, 6f) * 0.96f;
                    handMoveSpeedFactor = 0.03f;
                }

                if (wrappedHandAttackTimer >= handEnergyChargeUpTime + handArcPunchTime + handRepositionTime + handCollideTime + handCollideStunTime - 1f)
                {
                    DestroyAllHands();
                    SelectNextAttack();
                    NPC.netUpdate = true;
                }
            }

            // Make hands idly move.
            float handSpinSpeed = maxHandSpinSpeed * GetLerpValue(0f, 30f, AttackTimer - redirectTime, true);
            handSpinAngularOffset += handSpinSpeed;

            // Update hands.
            for (int i = 0; i < Hands.Count; i++)
            {
                XerocHand hand = Hands[i];
                Vector2 handOffset = (TwoPi * i / Hands.Count + handSpinAngularOffset).ToRotationVector2();
                handOffset.X = Pow(handOffset.X, 3f);
                handOffset *= new Vector2(1.55f, 0.4f) * handOffsetRadius;

                Vector2 handDestination = NPC.Center + handOffset;

                hand.UseRobe = true;
                hand.RobeDirection = (handDestination.X >= NPC.Center.X).ToDirectionInt();
                hand.ShouldOpen = true;

                // Instruct the hands to move towards a preset offset from the target if this hand is the one being used.
                float localHandMoveSpeedFactor = 5f;
                if (i == usedHandIndex)
                {
                    if (idealHandOffsetFromPlayer1 != Vector2.Zero)
                        handDestination = Target.Center + idealHandOffsetFromPlayer1;
                    localHandMoveSpeedFactor = handMoveSpeedFactor;
                    hand.UseRobe = false;
                    hand.ShouldOpen = false;
                }
                if ((i + Hands.Count / 2) % Hands.Count == usedHandIndex)
                {
                    if (idealHandOffsetFromPlayer2 != Vector2.Zero)
                        handDestination = Target.Center + idealHandOffsetFromPlayer2;
                    localHandMoveSpeedFactor = handMoveSpeedFactor;
                    hand.UseRobe = false;
                    hand.ShouldOpen = false;
                }

                DefaultHandDrift(hand, handDestination, localHandMoveSpeedFactor);
                hand.ScaleFactor = 1f;
            }
        }

        public void DoBehavior_StarConvergenceAndRedirecting()
        {
            int starCreationDelay = 21;
            int starCreationTime = 68;
            int attackTransitionDelay = 240;
            float spinRadius = 220f;
            float handMoveSpeedFactor = 3.7f;
            ref float spinDirection = ref NPC.ai[2];
            ref float starOffset = ref NPC.ai[3];
            Vector2 leftHandHoverDestination = NPC.Center + new Vector2(-300f, 118f);
            Vector2 rightHandHoverDestination = NPC.Center + new Vector2(300f, 118f);

            // Flap wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Update teeth.
            PerformTeethChomp(AttackTimer / 45f % 1f);

            // Hover above the player at first.
            if (AttackTimer <= starCreationDelay)
            {
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * spinRadius;
                NPC.velocity = Vector2.Lerp(NPC.velocity, (hoverDestination - NPC.Center) * 0.16f, 0.12f);

                // Decide the spin direction on the first frame, based on which side of the player Xeroc is.
                // This is done so that the spin continues moving in the direction the hover made Xeroc move.
                if (AttackTimer == 1f)
                {
                    spinDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                    NPC.netUpdate = true;
                }
            }

            // Spin around the player and conjure the star.
            else if (AttackTimer <= starCreationDelay + starCreationTime)
            {
                int movementDelay = starCreationDelay + starCreationTime - (int)AttackTimer + 17;
                float spinCompletionRatio = GetLerpValue(0f, starCreationTime, AttackTimer - starCreationDelay, true);
                float spinOffsetAngle = MathHelper.SmoothStep(0f, Pi, GetLerpValue(0f, starCreationTime, AttackTimer - starCreationDelay, true)) * spinDirection;
                float hoverSnapInterpolant = GetLerpValue(0f, 5f, AttackTimer - starCreationDelay, true) * 0.48f;
                Vector2 spinOffset = -Vector2.UnitY.RotatedBy(spinOffsetAngle) * spinRadius;
                Vector2 spinDestination = Target.Center + spinOffset;

                // Spin around the target.
                NPC.Center = Vector2.Lerp(NPC.Center, spinDestination, hoverSnapInterpolant);
                NPC.velocity = spinOffset.SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2 * spinDirection) * 25f;

                // Make things get brighter.
                PupilTelegraphArc = 10f;
                PupilTelegraphOpacity = Clamp(PupilTelegraphOpacity + 0.045f, 0f, 0.25f);

                // Look at the star position.
                PupilOffset = Vector2.Lerp(PupilOffset, spinOffset.SafeNormalize(Vector2.UnitY) * -37.5f, 0.15f);
                PupilScale = Lerp(PupilScale, 0.5f, 0.15f);

                if (AttackTimer % 2f == 0f)
                {
                    if (AttackTimer % 4f == 0f)
                        SoundEngine.PlaySound(SunFireballShootSound);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NewProjectileBetter(Target.Center, (TwoPi * spinCompletionRatio).ToRotationVector2() * 8f, ModContent.ProjectileType<StarPatterenedStarburst>(), StarburstDamage, 0f, -1, 0f, movementDelay + 5);

                        int star = NewProjectileBetter(Target.Center, (TwoPi * spinCompletionRatio + Pi / 5f).ToRotationVector2() * 8f, ModContent.ProjectileType<StarPatterenedStarburst>(), StarburstDamage, 0f, -1, 0f, movementDelay + 9);
                        if (Main.projectile.IndexInRange(star))
                        {
                            Main.projectile[star].ModProjectile<StarPatterenedStarburst>().RadiusOffset = 400f;
                            Main.projectile[star].ModProjectile<StarPatterenedStarburst>().ConvergenceAngleOffset = Pi / 5f;
                        }

                        star = NewProjectileBetter(Target.Center, (TwoPi * spinCompletionRatio + TwoPi / 5f).ToRotationVector2() * 8f, ModContent.ProjectileType<StarPatterenedStarburst>(), StarburstDamage, 0f, -1, 0f, movementDelay + 16);
                        if (Main.projectile.IndexInRange(star))
                        {
                            Main.projectile[star].ModProjectile<StarPatterenedStarburst>().RadiusOffset = 900f;
                            Main.projectile[star].ModProjectile<StarPatterenedStarburst>().ConvergenceAngleOffset = TwoPi / 5f;
                        }
                    }
                }
            }

            // Continue arcing as the stars do their thing.
            else
            {
                if (AttackTimer == starCreationDelay + starCreationTime + 16f)
                {
                    SoundEngine.PlaySound(SupernovaSound);
                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }

                if (NPC.velocity.Length() <= 105f)
                    NPC.velocity = NPC.velocity.RotatedBy(TwoPi * spinDirection / 210f) * 1.08f;
                NPC.Opacity = Clamp(NPC.Opacity - 0.02f, 0f, 1f);

                PupilTelegraphArc = 10f;
                PupilTelegraphOpacity = Clamp(PupilTelegraphOpacity + 0.045f, 0f, NPC.Opacity * 0.25f + 0.001f);

                // Silently hover above the player when completely invisible.
                if (NPC.Opacity <= 0f)
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.Center = Target.Center - Vector2.UnitY * 560f;
                }

                if (AttackTimer >= starCreationDelay + starCreationTime + attackTransitionDelay)
                {
                    DestroyAllHands();
                    SelectNextAttack();
                }
            }

            if (Hands.Count >= 2)
            {
                Hands[0].Rotation = Pi + PiOver2;
                Hands[1].Rotation = Pi - PiOver2;
                DefaultHandDrift(Hands[0], rightHandHoverDestination, handMoveSpeedFactor);
                DefaultHandDrift(Hands[1], leftHandHoverDestination, handMoveSpeedFactor);
            }
        }

        public void DoBehavior_BrightStarJumpscares()
        {
            int backgroundDimTime = 32;
            int starCreationRate = 9;
            int starCreationCountPerSide = 6;
            int starFireDelay = 56;
            int starCreationTime = starCreationRate * starCreationCountPerSide + starFireDelay;
            int starShoveRate = 20;
            float defaultStarZPosition = 5.9f;
            float defaultHandHoverOffset = 334f;
            Vector2 leftHandHoverPosition = NPC.Center - Vector2.UnitX * TeleportVisualsAdjustedScale * defaultHandHoverOffset;
            Vector2 rightHandHoverPosition = NPC.Center + Vector2.UnitX * TeleportVisualsAdjustedScale * defaultHandHoverOffset;
            ref float starCreationCounter = ref NPC.ai[2];
            ref float starShoveIndex = ref NPC.ai[3];

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Update teeth.
            TopTeethOffset = Lerp(TopTeethOffset, -15f, 0.1f);

            // Get rid of the seam if it's still there but hidden.
            SeamScale = 0f;

            // Make the background dim and have Xeroc go into the background at first.
            if (AttackTimer <= backgroundDimTime)
            {
                HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 0.5f, 0.09f);
                ZPosition = Pow(AttackTimer / backgroundDimTime, 1.74f) * 4.5f;
            }

            // Move behind the player and cast stars.
            else if (AttackTimer <= backgroundDimTime + starCreationTime)
            {
                // Make the hands suddenly move outward. They return to Xeroc shortly before the stars being being shoved.
                float handHoverOffset = Remap(AttackTimer - backgroundDimTime, starCreationTime - 12f, starCreationTime - 4f, 720f, defaultHandHoverOffset);
                if (AttackTimer == backgroundDimTime + 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                    SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce with
                    {
                        Volume = 8f,
                        MaxInstances = 10,
                        Pitch = -0.5f
                    });
                }
                leftHandHoverPosition = NPC.Center - Vector2.UnitX * TeleportVisualsAdjustedScale * handHoverOffset;
                rightHandHoverPosition = NPC.Center + Vector2.UnitX * TeleportVisualsAdjustedScale * handHoverOffset;

                // Create stars.
                if (AttackTimer % starCreationRate == 1f && AttackTimer < backgroundDimTime + starCreationRate * starCreationCountPerSide)
                {
                    SoundEngine.PlaySound(SoundID.Item100 with
                    {
                        Pitch = 0.2f,
                        Volume = 0.6f,
                        MaxInstances = 8
                    });
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        starCreationCounter++;
                        NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<BackgroundStar>(), BackgroundStarDamage, 0f, -1, defaultStarZPosition + Main.rand.NextFloat(3.7f), -starCreationCounter);
                        NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<BackgroundStar>(), BackgroundStarDamage, 0f, -1, defaultStarZPosition + Main.rand.NextFloat(3.7f), starCreationCounter);
                        NPC.netSpam = 0;
                        NPC.netUpdate = true;
                    }
                }

                // Hold all stars in place near the target.
                var stars = AllProjectilesByID(ModContent.ProjectileType<BackgroundStar>());
                foreach (Projectile star in stars)
                {
                    if (!star.ModProjectile<BackgroundStar>().ApproachingScreen)
                    {
                        float starIndex = star.ModProjectile<BackgroundStar>().Index;
                        float parallaxSpeed = Lerp(0.4f, 0.9f, star.identity * 13.584f % 1f);
                        Vector2 starHoverOffset = (star.identity * 98.157f).ToRotationVector2() * Abs(starIndex) * 12f;
                        Vector2 parallaxOffset = Vector2.One * (AttackTimer - backgroundDimTime) * -parallaxSpeed;
                        star.Center = Target.Center + new Vector2(starIndex * 154f, Abs(starIndex) * -20f) + starHoverOffset + parallaxOffset;
                        star.velocity = -Vector2.One * parallaxSpeed;
                    }
                }

                // Make the background dim even more for a bit of extra suspense before the stars are fired.
                if (AttackTimer >= backgroundDimTime + starCreationRate * starCreationCountPerSide)
                    HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 0.35f, 0.11f);

                // Play mumble sounds.
                if (AttackTimer == backgroundDimTime + starCreationTime - 30f)
                    PerformMumble();
            }

            // Shove hands towards the screen.
            else if (starShoveIndex <= starCreationCountPerSide * 2f && AnyProjectiles(ModContent.ProjectileType<BackgroundStar>()))
            {
                // Shove stars forward.
                if (AttackTimer % starShoveRate == starShoveRate - 1f)
                {
                    starShoveIndex++;
                    int modifiedStarIndex = (int)(starShoveIndex / 2f);
                    bool left = starShoveIndex % 2f == 1f;
                    if (left)
                        modifiedStarIndex *= -1;

                    // Make hands go forward suddenly.
                    Vector2 handPosition;
                    if (left)
                    {
                        Hands[0].ScaleFactor = 4f;
                        handPosition = Hands[0].Center;
                    }
                    else
                    {
                        Hands[1].ScaleFactor = 4f;
                        handPosition = Hands[1].Center;
                    }

                    var stars = AllProjectilesByID(ModContent.ProjectileType<BackgroundStar>()).Where(s => s.ModProjectile<BackgroundStar>().Index == modifiedStarIndex);
                    foreach (Projectile star in stars)
                    {
                        star.ModProjectile<BackgroundStar>().ApproachingScreen = true;
                        star.velocity = Vector2.Zero;
                        star.netUpdate = true;
                    }

                    // Play sounds and create visuals.
                    SoundEngine.PlaySound(SunFireballShootSound);
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.6f, 12);
                    Target.Calamity().GeneralScreenShakePower = 5f;
                }
            }

            else
            {
                ZPosition *= 0.9f;
                HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 1f, 0.12f);
                if (HeavenlyBackgroundIntensity >= 0.999f)
                {
                    ZPosition = 0f;
                    HeavenlyBackgroundIntensity = 1f;
                    DestroyAllHands();
                    SelectNextAttack();
                    ClearAllProjectiles();
                }
            }

            // Calculate the background hover position.
            float hoverHorizontalWaveSine = Sin(TwoPi * AttackTimer / 102f);
            float hoverVerticalWaveSine = Sin(TwoPi * AttackTimer / 132f);
            Vector2 hoverDestination = Target.Center + new Vector2(Target.velocity.X * 14.5f, ZPosition * -40f - 180f);
            hoverDestination.X += hoverHorizontalWaveSine * ZPosition * 36f;
            hoverDestination.Y -= hoverVerticalWaveSine * ZPosition * 8f;

            // Stay above the target while in the background.
            NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.03f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hoverDestination - NPC.Center) * 0.07f, 0.06f);

            // Move hands.
            if (Hands.Count >= 2)
            {
                Hands[0].UsePalmForm = Hands[1].UsePalmForm = true;
                Hands[0].ScaleFactor = Lerp(Hands[0].ScaleFactor, 1f, 0.09f);
                Hands[1].ScaleFactor = Lerp(Hands[1].ScaleFactor, 1f, 0.09f);
                Hands[0].Rotation = 0f;
                Hands[1].Rotation = 0f;
                Hands[0].RobeDirection = 1;
                Hands[1].RobeDirection = -1;
                Hands[0].DirectionOverride = 0;
                Hands[1].DirectionOverride = 0;
                Hands[0].UseRobe = true;
                Hands[1].UseRobe = true;

                DefaultHandDrift(Hands[0], rightHandHoverPosition, 4f);
                DefaultHandDrift(Hands[1], leftHandHoverPosition, 4f);
            }
        }

        public void DoBehavior_SwordConstellation()
        {
            int constellationConvergeTime = SwordConstellation.ConvergeTime;
            int animationTime = 72;
            int impactFireballCount = 11;
            int slashCount = 5;
            float wrappedAttackTimer = (AttackTimer - constellationConvergeTime) % animationTime;
            float handSpeedFactor = 300f;
            float impactFireballShootSpeed = 30f;
            float maxHoverSpeed = 19.5f;
            float hoverAcceleration = 0.135f;
            Vector2 handHoverOffset = NPC.Center;
            ref float swordRotation = ref NPC.ai[2];
            ref float slashOpacity = ref NPC.ai[3];

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Easing time!
            CurveSegment slowStart = new(PolyOutEasing, 0f, Pi, -Pi, 2);
            CurveSegment swingFast = new(PolyOutEasing, 0.5f, slowStart.EndingHeight, Pi + 0.8f, 3);
            CurveSegment endSwing = new(PolyInEasing, 0.8f, swingFast.EndingHeight, Pi - swingFast.EndingHeight, 5);

            // Summon the sword constellation, along with a single hand to wield it on the first frame.
            // Also teleport above the target.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitY * 300f);

                // Apply visual and sound effects.
                Target.Calamity().GeneralScreenShakePower = 9f;
                SoundEngine.PlaySound(ExplosionTeleportSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<SwordConstellation>(), SwordConstellationDamage, 0f, -1, 0f, -1f);
                    NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<SwordConstellation>(), SwordConstellationDamage, 0f, -1, 0f, 1f);
                }

                // Play mumble sounds.
                PerformMumble();
            }

            // Increment the slash counter.
            if (wrappedAttackTimer == 1f)
            {
                SwordSlashCounter++;
                NPC.netUpdate = true;
            }

            if (SwordSlashCounter >= slashCount + 1f)
            {
                // Destroy the swords.
                var swords = AllProjectilesByID(ModContent.ProjectileType<SwordConstellation>());
                foreach (Projectile sword in swords)
                {
                    for (int i = 0; i < 19; i++)
                    {
                        int gasLifetime = Main.rand.Next(20, 24);
                        float scale = 2.3f;
                        Vector2 gasSpawnPosition = sword.Center + Main.rand.NextVector2Circular(150f, 150f) * NPC.scale;
                        Vector2 gasVelocity = Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 7.25f;
                        Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));
                        Particle gas = new HeavySmokeParticle(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                        GeneralParticleHandler.SpawnParticle(gas);
                    }
                    sword.Kill();
                }

                SelectNextAttack();
                DestroyAllHands();
                return;
            }

            // Move into the background somewhat.
            ZPosition = Lerp(ZPosition, 1.3f, 0.1f);

            // Animate the slash.
            float animationCompletion = wrappedAttackTimer / animationTime;
            if (AttackTimer <= constellationConvergeTime)
                animationCompletion = 0f;

            // Attempt to hover above the target.
            float hoverSpeed = Remap(AttackTimer, constellationConvergeTime - 100f, constellationConvergeTime, 2f, maxHoverSpeed);
            Vector2 hoverDestination = Target.Center - Vector2.UnitY * 300f;
            NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.004f);
            NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverAcceleration);

            // Calculate sword direction values.
            float anticipationAngle = PiecewiseAnimation(animationCompletion, slowStart, swingFast, endSwing) - PiOver2;
            handHoverOffset = anticipationAngle.ToRotationVector2() * TeleportVisualsAdjustedScale * new Vector2(1050f, 450f);
            swordRotation = handHoverOffset.ToRotation() + PiOver2;

            // Update teeth in accordance with the sword animation completion.
            PerformTeethChomp(animationCompletion, 0.55f);

            // Create some impact effects when the stars finish congregating.
            if (AttackTimer == constellationConvergeTime)
            {
                SoundEngine.PlaySound(ExplosionTeleportSound);
                SoundEngine.PlaySound(SupernovaSound);
                Target.Calamity().GeneralScreenShakePower = 9.25f;
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 15);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center + handHoverOffset, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
            }

            // Make the slash trail appear if necessay.
            bool slashing = animationCompletion >= 0.5f && animationCompletion < 0.68f;
            slashOpacity = Clamp(slashOpacity + slashing.ToDirectionInt() * 0.25f, 0f, 1f);

            // Play slash sounds.
            if (animationCompletion >= 0.54f && animationCompletion <= 0.55f)
            {
                SoundEngine.PlaySound(SwordSlashSound);
                Target.Calamity().GeneralScreenShakePower = 8.5f;
                ScreenEffectSystem.SetFlashEffect(NPC.Center, 1f, 30);
            }

            // Create impact projectiles when the two swords collide.
            if (animationCompletion >= 0.62f && animationCompletion <= 0.63f)
            {
                SoundEngine.PlaySound(SunFireballShootSound, NPC.Center);
                Vector2 impactPoint = NPC.Center + Vector2.UnitY * handHoverOffset.Length() * 1.5f;

                RadialScreenShoveSystem.Start(impactPoint, 30);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < impactFireballCount; i++)
                    {
                        Vector2 fireballShootVelocity = (TwoPi * i / impactFireballCount).ToRotationVector2() * impactFireballShootSpeed + Main.rand.NextVector2Circular(4f, 4f);
                        fireballShootVelocity = Vector2.Lerp(fireballShootVelocity, (Target.Center - impactPoint) * 0.01f, 0.6f);

                        NewProjectileBetter(impactPoint, fireballShootVelocity, ModContent.ProjectileType<Starburst>(), StarburstDamage, 0f);
                    }
                }
            }

            // Move the hands, keeping the sword attached to it.
            if (Hands.Count >= 2)
            {
                Hands[0].ShouldOpen = false;
                Hands[0].ScaleFactor = 2f;
                DefaultHandDrift(Hands[0], NPC.Center + handHoverOffset * new Vector2(-1f, 1f), handSpeedFactor);

                Hands[1].ShouldOpen = false;
                Hands[1].ScaleFactor = 2f;
                DefaultHandDrift(Hands[1], NPC.Center + handHoverOffset, handSpeedFactor);

                var swords = AllProjectilesByID(ModContent.ProjectileType<SwordConstellation>());
                foreach (Projectile sword in swords)
                {
                    float swordSide = sword.ModProjectile<SwordConstellation>().SwordSide;
                    int handIndex = swordSide == 1f ? 1 : 0;

                    // The swordRotation variable is used here instead of the rotation value stored in the sword's AI because this would have a one-frame discrepancy and
                    // cause the sword to look like it's dragging behind a bit during slashes.
                    sword.Center = Hands[handIndex].Center + (swordRotation * swordSide - PiOver2).ToRotationVector2() * sword.scale * (sword.width * 0.5f + 20f);
                }
            }
        }

        public void DoBehavior_SwordConstellation2()
        {
            int constellationConvergeTime = SwordConstellation.ConvergeTime;
            int animationTime = 58 - SwordSlashCounter * 5;
            int slashCount = 5;
            float anticipationAnimationPercentage = 0.5f;

            // Make the attack faster in successive phases.
            if (CurrentPhase >= 2)
            {
                animationTime--;
                slashCount++;
            }

            ref float swordRotation = ref NPC.ai[2];
            ref float slashOpacity = ref NPC.ai[3];

            if (SwordSlashCounter <= 0)
                animationTime += 39;

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Easing time!
            CurveSegment slowStart = new(PolyOutEasing, 0f, Pi, -Pi, 2);
            CurveSegment swingFast = new(PolyOutEasing, 0.5f, slowStart.EndingHeight, Pi + 0.54f, 5);
            CurveSegment endSwing = new(PolyInEasing, 0.8f, swingFast.EndingHeight, Pi - swingFast.EndingHeight, 5);

            // Summon the sword constellation, along with a single hand to wield it on the first frame.
            // Also teleport above the target.
            if (AttackTimer == 1f)
            {
                // Delete leftover starbursts on the first frame.
                int starburstID = ModContent.ProjectileType<ArcingStarburst>();
                int starburstID2 = ModContent.ProjectileType<ArcingStarburst>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if ((p.type == starburstID || p.type == starburstID2) && p.active)
                        p.Kill();
                }

                ZPosition = 1f;
                SwordAnimationTimer = 0;
                XerocKeyboardShader.BrightnessIntensity = 1f;
                TeleportTo(Target.Center + Main.rand.NextVector2CircularEdge(340f, 340f));

                // Apply visual and sound effects.
                Target.Calamity().GeneralScreenShakePower = 9f;
                SoundEngine.PlaySound(ExplosionTeleportSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<SwordConstellation>(), SwordConstellationDamage, 0f, -1, 1f);
            }

            // Play mumble sounds.
            if (AttackTimer == constellationConvergeTime - 32f)
                PerformMumble();

            if (SwordSlashCounter >= slashCount + 1f)
            {
                // Destroy the swords.
                var swords = AllProjectilesByID(ModContent.ProjectileType<SwordConstellation>());
                foreach (Projectile sword in swords)
                {
                    for (int i = 0; i < 19; i++)
                    {
                        int gasLifetime = Main.rand.Next(20, 24);
                        float scale = 2.3f;
                        Vector2 gasSpawnPosition = sword.Center + Main.rand.NextVector2Circular(150f, 150f) * NPC.scale;
                        Vector2 gasVelocity = Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 7.25f;
                        Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));
                        Particle gas = new HeavySmokeParticle(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                        GeneralParticleHandler.SpawnParticle(gas);
                    }
                    sword.Kill();
                }

                SwordAnimationTimer = 0;
                SelectNextAttack();
                DestroyAllHands();
                return;
            }

            // Increment the slash counter.
            if (AttackTimer >= constellationConvergeTime)
                SwordAnimationTimer++;
            if (SwordAnimationTimer >= animationTime)
                SwordAnimationTimer = 0;

            // Hover to the top left/right of the target in anticipation of the slash at first.
            if (SwordAnimationTimer <= animationTime * anticipationAnimationPercentage || AttackTimer <= constellationConvergeTime)
            {
                if (SwordAnimationTimer == 2f)
                {
                    DestroyAllHands();
                    SwordSlashCounter++;
                    NPC.netUpdate = true;
                }

                // Decide the charge destination right before the charge.
                if (SwordAnimationTimer <= animationTime * anticipationAnimationPercentage - 13f)
                {
                    float distanceToTarget = NPC.Distance(Target.Center);
                    float dashDistance = MathF.Max(distanceToTarget + 250f, 850f);
                    if (Target.velocity.Length() <= 2f)
                        dashDistance -= 200f;

                    SwordChargeDestination = NPC.Center + NPC.SafeDirectionTo(Target.Center) * dashDistance;

                    Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 700f, -100f);
                    Vector2 idealVelocity = (hoverDestination - NPC.Center) * 0.14f;
                    if (SwordSlashCounter <= 0f)
                        idealVelocity *= 0.067f;

                    NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.14f);

                    // Decide which side Xeroc is hovering on.
                    SwordSlashDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                    PupilScale = Lerp(PupilScale, 0.2f, 0.25f);
                }

                // Slow down once the destination has been chosen.
                else
                    NPC.velocity *= 0.93f;

                // Charge at the target once ready.
                if (SwordAnimationTimer == (int)(animationTime * anticipationAnimationPercentage) - 1f && AttackTimer >= constellationConvergeTime + 1f)
                {
                    NPC.velocity = (SwordChargeDestination - Target.Center) * 0.075f;
                    NPC.netUpdate = true;

                    SoundEngine.PlaySound(SwordSlashSound);
                    RadialScreenShoveSystem.Start(NPC.Center, 20);
                    XerocKeyboardShader.BrightnessIntensity += 0.6f;

                    // Reset the trail cache for all swords.
                    var swords = AllProjectilesByID(ModContent.ProjectileType<SwordConstellation>());
                    foreach (Projectile sword in swords)
                    {
                        sword.oldRot = new float[sword.oldRot.Length];
                        sword.oldPos = new Vector2[sword.oldPos.Length];
                    }
                }

                // Look at the charge destination.
                PupilOffset = Vector2.Lerp(PupilOffset, (SwordChargeDestination - PupilPosition).SafeNormalize(Vector2.UnitY) * 40f, 0.13f);

                slashOpacity = Clamp(slashOpacity - 0.25f, 0f, 1f);
            }

            // Perform dash behaviors.
            else
            {
                NPC.Center = Vector2.Lerp(NPC.Center, SwordChargeDestination, 0.14f);
                NPC.velocity *= 0.81f;

                // Look at the target.
                PupilOffset = Vector2.Lerp(PupilOffset, (Target.Center - PupilPosition).SafeNormalize(Vector2.UnitY) * 40f, 0.12f);

                bool doingEndSwing = SwordAnimationTimer >= animationTime * 0.65f;
                slashOpacity = Clamp(slashOpacity - doingEndSwing.ToDirectionInt() * 0.5f, 0f, 1f);
            }

            // Calculate sword direction values.
            float animationCompletion = SwordAnimationTimer / (float)animationTime;
            float anticipationAngle = PiecewiseAnimation(animationCompletion, slowStart, swingFast, endSwing) - PiOver2;
            Vector2 handHoverOffset = anticipationAngle.ToRotationVector2() * TeleportVisualsAdjustedScale * new Vector2(SwordSlashDirection * 800f, 450f);
            swordRotation = handHoverOffset.ToRotation() + SwordSlashDirection * PiOver2;
            if (SwordSlashDirection == -1)
                swordRotation += Pi;

            // Update teeth in accordance with the sword animation completion.
            PerformTeethChomp(animationCompletion, 0.55f);

            // Cast a cone telegraph past the player.
            if (animationCompletion >= 0.11f)
            {
                float telegraphOpacity = GetLerpValue(0.11f, 0.26f, animationCompletion, true) * GetLerpValue(0.54f, 0.51f, animationCompletion, true) * 0.6f;
                PupilTelegraphOpacity = telegraphOpacity;
                PupilTelegraphArc = ToRadians(138f);
            }

            // Move the hands, keeping the sword attached to it.
            if (Hands.Count >= 2)
            {
                int handIndex = SwordSlashDirection == 0 ? 1 : 0;
                Hands[handIndex].ShouldOpen = false;
                Hands[handIndex].ScaleFactor = 1.5f;
                Hands[handIndex].Rotation = NPC.AngleTo(Hands[handIndex].Center) - PiOver2;
                DefaultHandDrift(Hands[handIndex], NPC.Center + handHoverOffset + Vector2.UnitX * SwordSlashDirection * 2f, 300f);

                Hands[1 - handIndex].ShouldOpen = true;
                Hands[1 - handIndex].ScaleFactor = 1.5f;
                Hands[1 - handIndex].Rotation = 0f;
                Hands[1 - handIndex].DirectionOverride = SwordSlashDirection;
                Hands[0].UsePalmForm = false;
                Hands[1].UsePalmForm = false;
                Hands[0].RobeDirection = 1;
                Hands[1].RobeDirection = -1;
                DefaultHandDrift(Hands[1 - handIndex], NPC.Center + new Vector2(SwordSlashDirection * -400f, 140f) * TeleportVisualsAdjustedScale, 300f);

                var swords = AllProjectilesByID(ModContent.ProjectileType<SwordConstellation>());
                foreach (Projectile sword in swords)
                {
                    sword.ModProjectile<SwordConstellation>().SwordSide = SwordSlashDirection;

                    // The swordRotation variable is used here instead of the rotation value stored in the sword's AI because this would have a one-frame discrepancy and
                    // cause the sword to look like it's dragging behind a bit during slashes.
                    sword.Center = Hands[handIndex].Center + (swordRotation - PiOver2).ToRotationVector2() * sword.scale * (sword.width * 0.5f + 20f);
                }
            }
        }

        public void DoBehavior_CircularPortalLaserBarrages()
        {
            int redirectTime = 30;
            int portalCount = 13;
            int portalSummonRate = 2;
            int portalSummonTime = portalCount * portalSummonRate;
            int attackTransitionDelay = 84;
            int laserShootTime = 38;
            int portalExistTime = 40;
            float handOffsetAngle = TwoPi - 0.8f;
            float laserAngularVariance = 0.008f;

            // Flap wings.
            UpdateWings(AttackTimer / 42f % 1f);

            // Update teeth.
            TopTeethOffset *= 0.9f;

            // Look at the player.
            PupilOffset = Vector2.Lerp(PupilOffset, (Target.Center - EyePosition).SafeNormalize(Vector2.UnitY) * 50f, 0.2f);

            // Redirect above the target.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 300f;

                // Add some momentum, shove the screen, and summon two hands on the first frame.
                if (AttackTimer == 1f)
                {
                    RadialScreenShoveSystem.Start(NPC.Center, 16);
                    NPC.velocity = (hoverDestination - Target.Center) * 0.075f;
                    NPC.netUpdate = true;
                }
                else
                    NPC.velocity *= 0.8f;

                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.16f);
            }

            // Summon portals.
            else if (AttackTimer <= redirectTime + portalSummonTime)
            {
                float portalSummonCompletion = GetLerpValue(0f, portalSummonTime - 1f, AttackTimer - redirectTime, true);
                float portalSummonAngle = handOffsetAngle * (1f - portalSummonCompletion);

                // Slow down to a halt.
                NPC.velocity = Vector2.Zero;

                // Play a portal sound.
                if (AttackTimer == redirectTime - 1f)
                    SoundEngine.PlaySound(PortalCastSound, NPC.Center);

                // Summon portals.
                if (AttackTimer % portalSummonRate == 0f)
                {
                    Vector2 portalSummonPosition = NPC.Center + portalSummonAngle.ToRotationVector2() * new Vector2(1650f, 1275f);

                    int remainingChargeTime = portalSummonTime - (int)(AttackTimer - redirectTime);
                    int fireDelay = remainingChargeTime + 30;
                    float portalScale = Main.rand.NextFloat(0.57f, 0.67f);

                    Vector2 portalDirection = -NPC.SafeDirectionTo(portalSummonPosition).RotatedByRandom(laserAngularVariance);

                    // Summon the portal and shoot the telegraph for the laser.
                    NewProjectileBetter(portalSummonPosition + portalDirection * Main.rand.NextFloatDirection() * 20f, portalDirection, ModContent.ProjectileType<LightPortal>(), 0, 0f, -1, portalScale, portalExistTime + remainingChargeTime + 15);
                    NewProjectileBetter(portalSummonPosition, portalDirection, ModContent.ProjectileType<TelegraphedLightLaserbeam>(), LightLaserbeamDamage, 0f, -1, fireDelay, laserShootTime);
                }
            }

            if (AttackTimer >= redirectTime + portalSummonTime + attackTransitionDelay)
            {
                DestroyAllHands();
                SelectNextAttack();
            }

            // Update hands.
            if (Hands.Count >= 2)
            {
                Vector2 handOffset = handOffsetAngle.ToRotationVector2() * TeleportVisualsAdjustedScale * new Vector2(500f, 160f);

                Hands[0].Rotation = PiOver2;
                Hands[1].Rotation = -PiOver2;
                Hands[0].UseRobe = true;
                Hands[1].UseRobe = true;
                Hands[0].RobeDirection = -1;
                Hands[1].RobeDirection = 1;
                DefaultHandDrift(Hands[0], NPC.Center + handOffset * new Vector2(-1f, 1f), 2f);
                DefaultHandDrift(Hands[1], NPC.Center + handOffset, 2f);
            }
        }
    }
}
