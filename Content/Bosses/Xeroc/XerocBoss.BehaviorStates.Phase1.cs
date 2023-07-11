using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Noxus;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;
using static NoxusBoss.Content.Bosses.Xeroc.XerocSky;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        public void DoBehavior_ConjureExplodingStars()
        {
            int redirectTime = 22;
            int hoverTime = 64;
            int starShootCount = 6;
            int starCreateRate = 4;
            int starTelegraphTime = starShootCount * starCreateRate;
            int starBlastDelay = 12;
            int attackTransitionDelay = 95;
            int explosionCount = 2;
            float starOffsetRadius = 600f;
            ref float explosionCounter = ref NPC.ai[2];

            // Redirect above the player.
            if (AttackTimer <= redirectTime)
            {
                float redirectInterpolant = Pow(AttackTimer / redirectTime, 0.66f) * 0.45f;
                float overshootInPixels = GetLerpValue(0.2f, 0.6f, redirectInterpolant, true) * GetLerpValue(0.96f, 0.75f, redirectInterpolant, true) * 50f;
                Vector2 overshootOffset = (NPC.position - NPC.oldPosition).SafeNormalize(Vector2.Zero) * overshootInPixels;
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * 480f + overshootOffset, redirectInterpolant);
            }
            else
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * 480f, 0.5f);

            // Flap wings.
            UpdateWings(AttackTimer / 54f % 1f);

            // Conjure two hands after the redirect.
            if (AttackTimer == redirectTime + 20f)
            {
                ConjureHandsAtPosition(NPC.Center - Vector2.UnitX * 100f, -Vector2.UnitX * 4f, true);
                ConjureHandsAtPosition(NPC.Center + Vector2.UnitX * 100f, Vector2.UnitX * 4f, true);
            }

            // Update hands.
            if (Hands.Count >= 2)
            {
                DefaultHandDrift(Hands[0], NPC.Center + new Vector2(-400f, 100f) * TeleportVisualsAdjustedScale);
                DefaultHandDrift(Hands[1], NPC.Center + new Vector2(400f, 100f) * TeleportVisualsAdjustedScale);
                Hands[0].Rotation = Hands[0].Rotation.AngleLerp(Pi, 0.1f);
                Hands[1].Rotation = Hands[1].Rotation.AngleLerp(Pi, 0.1f);
                Hands[0].ShouldOpen = AttackTimer >= redirectTime + hoverTime - 7f;
                Hands[1].ShouldOpen = AttackTimer >= redirectTime + hoverTime - 7f;

                // Snap fingers and make the screen shake.
                if (AttackTimer == redirectTime + hoverTime - 10f)
                {
                    SoundEngine.PlaySound(FingerSnapSound with { Volume = 4f });
                    Target.Calamity().GeneralScreenShakePower = 11f;
                }

                // Calculate the opacity of the hands. If they are invisible they are removed.
                float handOpacity = GetLerpValue(redirectTime + hoverTime + 15f, redirectTime + hoverTime, AttackTimer, true);
                for (int i = 0; i < Hands.Count; i++)
                {
                    Hands[i].Opacity = handOpacity;
                    if (handOpacity <= 0f)
                    {
                        DestroyAllHands();
                        NPC.netUpdate = true;
                        break;
                    }
                }
            }

            // Create star telegraphs.
            if (AttackTimer >= redirectTime + hoverTime && AttackTimer <= redirectTime + hoverTime + starTelegraphTime && AttackTimer % starCreateRate == 1f)
            {
                float starSpawnOffsetAngle = TwoPi * (AttackTimer - redirectTime - hoverTime) / starTelegraphTime - PiOver2;
                Vector2 starSpawnOffset = starSpawnOffsetAngle.ToRotationVector2() * starOffsetRadius;
                StarSpawnOffsets.Add(starSpawnOffset);
                CreateTwinkle(Target.Center + starSpawnOffset, Vector2.One * 1.7f);

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
            PupilTelegraphOpacity = lookAtTargetInterpolant * telegraphDissipateInterpolant;
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
                Target.Calamity().GeneralScreenShakePower = 6f;
                SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce with { Volume = 1.2f, MaxInstances = 10 });

                float horizontalOffsetSign = GeneralHoverOffset.X == 0f ? Main.rand.NextFromList(-1f, 1f) : -Sign(GeneralHoverOffset.X);
                GeneralHoverOffset = new Vector2(horizontalOffsetSign * Main.rand.NextFloat(500f, 700f), Main.rand.NextFloat(-550f, -340f));
                NPC.netUpdate = true;
            }

            // Shoot the redirecting starbursts.
            if (postTeleportAttackTimer >= shootDelay && postTeleportAttackTimer <= shootDelay + starburstCount)
            {
                // Create a light explosion on the first frame.
                if (postTeleportAttackTimer == shootDelay)
                {
                    Target.Calamity().GeneralScreenShakePower = 16f;
                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(PupilPosition, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

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

            if (postTeleportAttackTimer >= shootDelay + starburstCount + attackTransitionDelay)
                SelectNextAttack();
        }

        public void DoBehavior_RealityTearDaggers()
        {
            int riseTime = 30;
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
                ZPosition = Pow(AttackTimer / riseTime, 1.6f) * 2.8f;

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
                for (int i = 0; i < totalRadialSlices; i++)
                {
                    Vector2 handOffset = (TwoPi * i / totalRadialSlices).ToRotationVector2() * NPC.scale * 400f;
                    ConjureHandsAtPosition(NPC.Center + handOffset, sliceDirection * 3f, false);
                }
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
                    if (i != handToMoveIndex)
                        handDestination = NPC.Center + (TwoPi * i / Hands.Count).ToRotationVector2() * NPC.scale * 400f;

                    hand.Center = Vector2.Lerp(hand.Center, handDestination, wrappedAttackTimer / handWaveTime * 0.3f + 0.02f);

                    DefaultHandDrift(hand, handDestination, 1.2f);
                    hand.Rotation = (hand.Center - EyePosition).ToRotation() - PiOver2;
                    hand.Frame = 2;
                    hand.ShouldOpen = false;
                }
            }

            // Create slices.
            if (wrappedAttackTimer == handWaveTime && AttackTimer >= riseTime + 1f && sliceCounter < totalSlices)
            {
                // Create a slice sound.
                SoundEngine.PlaySound(Exoblade.BeamHitSound with { MaxInstances = 8, Volume = 5f });

                if (sliceCounter >= totalHorizontalSlices)
                    sliceDirection = sliceDirection.RotatedBy(Pi / totalRadialSlices);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(Target.Center - sliceDirection * sliceTelegraphLength * 0.5f + sliceSpawnOffset, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), ScreenSliceDamage, 0f, -1, sliceTelegraphTime, sliceTelegraphLength);

                sliceCounter++;
                NPC.netUpdate = true;
            }

            // Return to the foreground and destroy hands after doing the slices.
            if (sliceCounter >= totalSlices)
            {
                ZPosition = Lerp(ZPosition, 0f, 0.06f);

                // Destroy the hands after enough time has passed.
                if (attackTransitionCounter == 60f)
                    DestroyAllHands();

                attackTransitionCounter++;
                if (attackTransitionCounter >= 150f)
                    SelectNextAttack();
            }
        }

        public void DoBehavior_StarManagement()
        {
            int starGrowTime = ControlledStar.GrowToFullSizeTime;
            int attackDelay = starGrowTime + 40;
            int flareShootCount = 5;
            int shootTime = 450;
            int starburstReleaseRate = 30;
            int starburstCount = 9;
            float starburstStartingSpeed = 1.3f;

            // Make things faster in successive phases.
            if (CurrentPhase >= 1)
            {
                attackDelay -= 10;
                flareShootCount--;
                shootTime -= 90;
            }

            ref float flareShootCounter = ref NPC.ai[2];

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Create a suitable star and two hands on the first frame.
            // The star will give the appearance of actually coming from the background.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitX * Target.direction * 400f);

                Vector2 starSpawnPosition = NPC.Center + new Vector2(300f, -350f) * TeleportVisualsAdjustedScale;
                CreateTwinkle(starSpawnPosition, Vector2.One * 1.3f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(starSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ControlledStar>(), 0, 0f);

                    ConjureHandsAtPosition(NPC.Center - Vector2.UnitX * TeleportVisualsAdjustedScale * 300f, Vector2.UnitY * -4f, false);
                    ConjureHandsAtPosition(NPC.Center + Vector2.UnitX * TeleportVisualsAdjustedScale * 300f, Vector2.UnitY * -4f, false);
                }
                return;
            }

            if (AttackTimer <= 48f)
            {
                for (int i = 0; i < Hands.Count; i++)
                    Hands[i].ShouldOpen = false;
            }
            if (AttackTimer == 52f)
                SoundEngine.PlaySound(FingerSnapSound with { Volume = 4f });

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

                NPC.ai[3] = 0f;
                NPC.netUpdate = true;
            }

            // Update hand positions.
            DefaultHandDrift(Hands[0], NPC.Center - Vector2.UnitX * TeleportVisualsAdjustedScale * 320f, 1.8f);
            DefaultHandDrift(Hands[1], NPC.Center + Vector2.UnitX * TeleportVisualsAdjustedScale * 320f, 1.8f);
            Hands[0].Rotation = Hands[0].Rotation.AngleLerp(Pi, 0.1f);
            Hands[1].Rotation = Hands[1].Rotation.AngleLerp(Pi, 0.1f);

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
            {
                DestroyAllHands();
                SelectNextAttack();
            }
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
            float fastChargeSpeedInterpolahnt = verticalCharges ? 0.184f : 0.13f;
            int portalReleaseRate = verticalCharges ? 3 : 4;

            // Flap wings.
            UpdateWings(AttackTimer / 42f % 1f);

            // Look at the player.
            PupilOffset = Vector2.Lerp(PupilOffset, (Target.Center - EyePosition).SafeNormalize(Vector2.UnitY) * 50f, 0.2f);

            // Move to the side of the player.
            if (AttackTimer <= closeRedirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 350f, 300f);
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
                    NewProjectileBetter(NPC.Center + portalDirection * Main.rand.NextFloatDirection() * 20f, portalDirection, ModContent.ProjectileType<LightPortal>(), 0, 0f, -1, portalScale, portalExistTime + remainingChargeTime + 15);
                    NewProjectileBetter(NPC.Center, portalDirection, ModContent.ProjectileType<TelegraphedLightLaserbeam>(), LightLaserbeamDamage, 0f, -1, fireDelay, laserShootTime);

                    // Spawn a second telegraph laser in the opposite direction if a portal was summoned due to being close to the target.
                    // This is done to prevent just flying up/forward to negate the attack.
                    if (forcefullySpawnPortal)
                    {
                        portalDirection *= -1f;
                        NewProjectileBetter(NPC.Center, portalDirection, ModContent.ProjectileType<TelegraphedLightLaserbeam>(), LightLaserbeamDamage, 0f, -1, fireDelay, laserShootTime);
                    }
                }

                // Go FAST.
                Vector2 chargeDirectionVector = verticalCharges ? Vector2.UnitY * chargeDirection : Vector2.UnitX * chargeDirection;
                NPC.velocity = Vector2.Lerp(NPC.velocity, chargeDirectionVector * 150f, fastChargeSpeedInterpolahnt);
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

        public void DoBehavior_StealSun()
        {
            int sunDescentTime = 90;
            int sunGrowTime = 80;
            int fireballReleaseRate = 30;
            int laserbeamShootDelay = (int)Remap(AttackTimer - sunDescentTime - sunGrowTime, 90f, 360f, 150f, 36f);
            int laserbeamTelegraphTime = 40;
            int laserbeamCount = 8;
            int shootTime = 510;
            float hoverFlySpeedInterpolant = 0.1f;

            // Make things faster in successive phases.
            if (CurrentPhase >= 1)
            {
                fireballReleaseRate -= 6;
                laserbeamCount += 2;
                shootTime -= 90;
            }

            ref float laserbeamShootTimer = ref NPC.ai[2];

            // Flap wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Move into the background and make the sun come down.
            if (AttackTimer <= sunDescentTime)
            {
                // Initialize the sun position.
                if (AttackTimer == 1f)
                {
                    ManualSunDrawPosition = new Vector2(Main.screenWidth * 0.5f, -900f);
                    SoundEngine.PlaySound(BossRushEvent.Tier2TransitionSound);
                }

                ManualSunScale = 1f;
                ManualSunDrawPosition = Vector2.Lerp(ManualSunDrawPosition, new Vector2(Main.screenWidth * 0.5f, -60f), 0.094f);
                ManualSunOpacity = Clamp(ManualSunOpacity + 0.07f, 0f, 1f);
                OriginalScreenSizeForSun = new(Main.screenWidth, Main.screenHeight);

                // Hover in the background.
                ZPosition = Pow(AttackTimer / sunDescentTime, 2f) * 5f;

                // Look upward.
                PupilOffset = -Vector2.UnitY * Pow(AttackTimer / sunDescentTime, 1.67f) * 36f;
            }

            // Make the sun grow.
            else if (AttackTimer <= sunDescentTime + sunGrowTime)
            {
                ManualSunScale = Lerp(ManualSunScale, 16f, 0.009f);
                PupilOffset = (ManualSunDrawPosition + Main.screenPosition - EyePosition).SafeNormalize(Vector2.UnitY) * 36f;
                PupilTelegraphArc = ToRadians(84f);
                PupilTelegraphOpacity = Clamp(PupilTelegraphOpacity + 0.03f, 0f, 0.3f);

                // Create a wave effect before the sun does things.
                if (AttackTimer == sunDescentTime + sunGrowTime - 1f)
                {
                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(ManualSunDrawPosition + Main.screenPosition, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }
            }

            // Release an idle stream of slithering solar fireballs, and occasionally create bright flashes that release fire lasers.
            else if (AttackTimer <= sunDescentTime + sunGrowTime + shootTime)
            {
                // Slow down before firing.
                laserbeamShootTimer++;
                hoverFlySpeedInterpolant *= GetLerpValue(laserbeamShootDelay - 1f, laserbeamShootDelay * 0.6f, laserbeamShootTimer, true);

                // Make the sun stay above Xeroc.
                Vector2 idealSunPosition = new(NPC.Center.X - Main.screenPosition.X, ManualSunDrawPosition.Y);
                ManualSunDrawPosition = Vector2.Lerp(ManualSunDrawPosition, idealSunPosition, 1f);

                // Periodically release the bursts of fireballs.
                // TODO -- This is probably not going to work in multiplayer.
                Vector2 sunPositionWorld = ManualSunDrawPosition + Main.screenPosition;
                if (AttackTimer % fireballReleaseRate == 1f)
                {
                    Target.Calamity().GeneralScreenShakePower = 8f;
                    SoundEngine.PlaySound(SunFireballShootSound, NPC.Center);

                    float fireballShootSpeed = 50f;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 fireballShootVelocity = (Target.Center - sunPositionWorld).SafeNormalize(Vector2.UnitY).RotatedBy(Lerp(-0.14f, 0.14f, i / 2f)) * fireballShootSpeed + Main.rand.NextVector2Circular(1f, 1f);
                        Vector2 fireballSpawnPosition = sunPositionWorld + fireballShootVelocity.SafeNormalize(Vector2.UnitY) * ManualSunScale * 4.75f;
                        NewProjectileBetter(fireballSpawnPosition, fireballShootVelocity, ModContent.ProjectileType<SunFireball>(), FireballDamage, 0f);
                    }
                }

                // Periodically release the telegraphed lasers.
                if (AnyProjectiles(ModContent.ProjectileType<TelegraphedStarLaserbeam>()))
                {
                    hoverFlySpeedInterpolant = 0f;
                    laserbeamShootTimer = 0f;
                    NPC.velocity = Vector2.Zero;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && laserbeamShootTimer >= laserbeamShootDelay)
                {
                    float beamShootAngularOffset = Main.rand.NextFloat(TwoPi);
                    for (int i = 0; i < laserbeamCount; i++)
                    {
                        Vector2 laserbeamShootDirection = Vector2.UnitY.RotatedBy(TwoPi * i / laserbeamCount + beamShootAngularOffset);
                        NewProjectileBetter(sunPositionWorld, laserbeamShootDirection, ModContent.ProjectileType<TelegraphedStarLaserbeam>(), LightLaserbeamDamage, 0f, -1, laserbeamTelegraphTime, 0f);
                    }
                }
            }

            else
            {
                if (AttackTimer == sunDescentTime + sunGrowTime + shootTime)
                {
                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(ManualSunDrawPosition + Main.screenPosition, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }

                ZPosition = Lerp(ZPosition, 0f, 0.07f);
                if (!AnyProjectiles(ModContent.ProjectileType<TelegraphedStarLaserbeam>()))
                {
                    ManualSunScale = Clamp(ManualSunScale * 0.97f - 0.05f, 1f, 20f);
                    ManualSunDrawPosition = Vector2.Lerp(ManualSunDrawPosition, new Vector2(ManualSunDrawPosition.X, -1200f), 0.05f);

                    if (ManualSunDrawPosition.Y <= -1198f)
                        SelectNextAttack();
                }
            }

            // Calculate the background hover position.
            float hoverHorizontalWaveSine = Sin(TwoPi * AttackTimer / 96f);
            float hoverVerticalWaveSine = Sin(TwoPi * AttackTimer / 120f);
            Vector2 hoverDestination = Target.Center + new Vector2(Target.velocity.X * -4f, ZPosition * 30f - 100f);
            hoverDestination.X += hoverHorizontalWaveSine * ZPosition * 10f;
            hoverDestination.Y -= hoverVerticalWaveSine * ZPosition * 8f;

            // Hover behind the target. This is slowed down when the laser is being fired.
            Vector2 idealVelocity = (hoverDestination - NPC.Center) * hoverFlySpeedInterpolant;
            NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.1f);
        }

        public void DoBehavior_StarManagement_CrushIntoQuasar()
        {
            int redirectTime = 45;
            int starPressureTime = 60;
            int supernovaDelay = 30;
            int pressureArmsCount = 9;
            int plasmaShootDelay = 60;
            int plasmaShootRate = 3;
            int plasmaShootTime = Supernova.Lifetime - 90;
            float plasmaShootSpeed = 9f;
            float handOrbitOffset = 100f;
            float pressureInterpolant = GetLerpValue(redirectTime, redirectTime + starPressureTime, AttackTimer, true);

            // Make things faster in successive phases.
            if (CurrentPhase >= 1)
                plasmaShootRate--;

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

            // Conjure hands on the first frame.
            if (AttackTimer == 1f)
            {
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
            }

            // Make hands circle the star.
            for (int i = 0; i < Hands.Count; i++)
            {
                DefaultHandDrift(Hands[i], star.Center + (TwoPi * i / Hands.Count + AttackTimer / 18f).ToRotationVector2() * (star.scale * handOrbitOffset + 50f), 1.4f);
                Hands[i].Rotation = Hands[i].Center.AngleTo(star.Center) - PiOver2;
                Hands[i].Center += Main.rand.NextVector2Circular(10f, 10f) * Pow(pressureInterpolant, 2f);
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

                star.Kill();

                // Delete the hands.
                DestroyAllHands();

                NPC.netUpdate = true;
            }

            // Create plasma around the player that converges into the black quasar.
            if (quasar is not null && AttackTimer >= redirectTime + starPressureTime + supernovaDelay + plasmaShootDelay && AttackTimer <= redirectTime + starPressureTime + supernovaDelay + plasmaShootDelay + plasmaShootTime && AttackTimer % plasmaShootRate == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 plasmaSpawnPosition = quasar.Center + Main.rand.NextVector2Unit() * (quasar.Distance(Target.Center) + Main.rand.NextFloat(250f, 700f));
                    Vector2 plasmaVelocity = (quasar.Center - plasmaSpawnPosition).SafeNormalize(Vector2.UnitY) * plasmaShootSpeed;
                    while (Target.WithinRange(plasmaSpawnPosition, 880f))
                        plasmaSpawnPosition -= plasmaVelocity;

                    NewProjectileBetter(plasmaSpawnPosition, plasmaVelocity, ModContent.ProjectileType<ConvergingSupernovaEnergy>(), SupernovaEnergyDamage, 0f);
                }
            }
        }
    }
}
