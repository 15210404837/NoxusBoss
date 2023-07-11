using System.Linq;
using System.Reflection;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.Bosses.Xeroc.XerocSky;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        public void DoBehavior_LightBeamTransformation()
        {
            int redirectTime = 45;
            int upwardRiseTime = 30;
            int chaseTime = SuperLightBeam.LaserLifetime;
            int laserTelegraphTime = 49;

            // Redirect above the target.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 600f, -300f);
                NPC.Opacity = 1f;

                // Add some momentum, shove the screen, and play violent sounds on the first frame.
                if (AttackTimer == 1f)
                {
                    SoundEngine.PlaySound(BossRushEvent.Tier4TransitionSound);
                    RadialScreenShoveSystem.Start(NPC.Center, 16);
                    NPC.velocity = (hoverDestination - Target.Center) * 0.075f;
                    NPC.netUpdate = true;
                }
                else
                    NPC.velocity *= 0.8f;

                UpdateWings(AttackTimer / redirectTime);

                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.16f);
            }

            // Fade away as the light beam appears.
            else if (AttackTimer <= redirectTime + upwardRiseTime)
            {
                NPC.Opacity = Clamp(NPC.Opacity - 0.09f, 0f, 1f);
                NPC.velocity.X *= 0.9f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, -Vector2.UnitY * 35f, 0.1f);

                // Create the light beam at the end.
                if (AttackTimer == redirectTime + upwardRiseTime - 1f)
                {
                    LocalScreenSplitSystem.Start(NPC.Center, 20, PiOver2 * 0.9999f, 500f);
                    SoundEngine.PlaySound(ScreamSound with { Volume = 3f });
                    SoundEngine.PlaySound(SupernovaSound with { Volume = 8f });
                    ScreenEffectSystem.SetFlashEffect(NPC.Center, 8f, 60);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.UnitY, ModContent.ProjectileType<SuperLightBeam>(), SuperLaserbeamDamage, 0f);
                }
            }

            // Chase the target.
            else if (AttackTimer <= redirectTime + upwardRiseTime + chaseTime)
            {
                NPC.Opacity = 0f;
                if (NPC.velocity.Y <= -20f)
                    NPC.velocity.Y += 4f;

                NPC.position.Y = Target.position.Y;
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(Target.Center) * 16f, 0.19f);

                // Periodically release fire beams.
                if (AttackTimer % 22f == 0f && AttackTimer <= redirectTime + upwardRiseTime + chaseTime - 60f)
                {
                    ScreenEffectSystem.SetFlashEffect(NPC.Center, 0.9f, 90);
                    ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 2f, 18);
                    SoundEngine.PlaySound(SunFireballShootSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 laserSpawnPosition = Target.Center - Target.velocity.SafeNormalize((TwoPi * AttackTimer / 60f).ToRotationVector2()) * 300f;
                        Vector2 laserDirection = (Target.Center - laserSpawnPosition).SafeNormalize(Vector2.UnitY);
                        NewProjectileBetter(laserSpawnPosition, laserDirection, ModContent.ProjectileType<TelegraphedLightLaserbeam>(), LightLaserbeamDamage, 0f, -1, laserTelegraphTime, 26f);
                    }
                }
            }
            else if (AttackTimer >= redirectTime + upwardRiseTime + chaseTime + 60f)
                SelectNextAttack();
        }

        public void DoBehavior_HandScreenShatter()
        {
            int backgroundDimTime = 30;
            int handFireRate = 23;
            int handShootTime = 16;
            int regularHandFireCount = 8;
            int shoveBackgroundEnterDelay = 120;
            int shoveTime = 11;
            int sliceTelegraphTime = 48;
            float sliceLength = 4500f;
            Vector2 leftHandHoverDestination = NPC.Center + new Vector2(-330f, 100f) * TeleportVisualsAdjustedScale;
            Vector2 rightHandHoverDestination = NPC.Center + new Vector2(330f, 100f) * TeleportVisualsAdjustedScale;

            ref float handFireCounter = ref NPC.ai[2];
            ref float shoveTimer = ref NPC.ai[3];
            bool leftHandIsAffected = handFireCounter % 2f == 0f;

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Make the background dim and have Xeroc go into the background at first.
            if (AttackTimer <= backgroundDimTime)
            {
                // Conjure a two hands on the first frame. It will be used later to bring the stars forward.
                if (AttackTimer == 1f)
                {
                    ConjureHandsAtPosition(NPC.Center - Vector2.UnitX * 50f, Vector2.Zero, true);
                    ConjureHandsAtPosition(NPC.Center + Vector2.UnitX * 50f, Vector2.Zero, true);
                }

                HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 0.5f, 0.09f);
                ZPosition = Pow(AttackTimer / backgroundDimTime, 1.74f) * 3f;
                HandFireDestination = Vector2.Lerp(NPC.Center, Target.Center, 0.7f);
            }

            // Make hands go towards the screen.
            else if (handFireCounter < regularHandFireCount)
            {
                // Decide where the hand should fly towards.
                float wrappedAttackTimer = AttackTimer % handFireRate;
                if (wrappedAttackTimer <= handFireRate - handShootTime)
                    HandFireDestination = Vector2.Lerp(NPC.Center, Target.Center, 0.7f);

                // Hover above the target.
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 350f;
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 20f, 0.3f);

                // Release hands.
                if (wrappedAttackTimer >= handFireRate - handShootTime + 1f)
                {
                    // Prepare for the next hand movement.
                    if (wrappedAttackTimer == handFireRate - handShootTime + 1f)
                    {
                        SoundEngine.PlaySound(FastHandMovementSound, NPC.Center);
                        handFireCounter++;
                        NPC.netUpdate = true;
                    }

                    // Create impact effects once the hand has gotten big enough.
                    XerocHand affectedHand = leftHandIsAffected ? Hands[0] : Hands[1];
                    if (affectedHand.ScaleFactor >= 5.5f)
                    {
                        RadialScreenShoveSystem.Start(affectedHand.Center, 20);
                        SoundEngine.PlaySound(SunFireballShootSound);
                        if (Main.netMode != NetmodeID.MultiplayerClient && handFireCounter >= 2f)
                        {
                            for (int i = 0; i < 9; i++)
                            {
                                int starburstID = i % 3 != 0 ? ModContent.ProjectileType<Starburst>() : ModContent.ProjectileType<ArcingStarburst>();
                                NewProjectileBetter(affectedHand.Center, (TwoPi * i / 9f).ToRotationVector2() * 8.25f, starburstID, StarburstDamage, 0f);
                            }
                        }

                        affectedHand.CanDoDamage = false;
                        affectedHand.ScaleFactor = 1f;
                        affectedHand.Center = leftHandIsAffected ? leftHandHoverDestination : rightHandHoverDestination;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        if (leftHandIsAffected)
                            leftHandHoverDestination = HandFireDestination;
                        else
                            rightHandHoverDestination = HandFireDestination;

                        affectedHand.CanDoDamage = handFireCounter >= 2f;
                        affectedHand.ScaleFactor = Remap(wrappedAttackTimer, handFireRate - handShootTime + 1f, handFireRate - 2f, 1f, 6f);
                    }
                }
            }
            else if (shoveTimer < shoveBackgroundEnterDelay + shoveTime + 16f)
            {
                // Go further into the background at first and make the background get really bright.
                float backgroundEnterInterpolant = GetLerpValue(0f, shoveBackgroundEnterDelay, shoveTimer, true);
                if (backgroundEnterInterpolant < 1f)
                {
                    HeavenlyBackgroundIntensity = Lerp(0.5f, 6f, Pow(backgroundEnterInterpolant, 1.75f));
                    ZPosition = Lerp(3f, 7.2f, backgroundEnterInterpolant);

                    // Hover above the target.
                    Vector2 hoverDestination = Target.Center - Vector2.UnitY * Lerp(60f, 324f, backgroundEnterInterpolant);
                    Vector2 idealVelocity = (hoverDestination - NPC.Center) * 0.15f;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.16f);
                }

                // Charge at the target.
                if (shoveTimer == shoveBackgroundEnterDelay)
                {
                    SoundEngine.PlaySound(ScreamSound);
                    NPC.velocity = NPC.SafeDirectionTo(Target.Center) * 10f;

                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 15);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }
                if (shoveTimer >= shoveBackgroundEnterDelay && shoveTimer <= shoveBackgroundEnterDelay + shoveTime)
                {
                    ZPosition = Clamp(ZPosition - 10f / shoveTime, -0.9f, 10f);

                    // Creaate scream effects while charging.
                    if (shoveTimer % 2f == 0f)
                    {
                        Color burstColor = Main.rand.NextBool() ? Color.LightGoldenrodYellow : Color.Lerp(Color.White, Color.IndianRed, 0.7f);

                        // Create blur and burst particle effects.
                        ExpandingChromaticBurstParticle burst = new(EyePosition, Vector2.Zero, burstColor, 16, 0.1f);
                        GeneralParticleHandler.SpawnParticle(burst);
                        ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);
                        Target.Calamity().GeneralScreenShakePower = 15f;
                    }

                    // Create screen impact effects.
                    if (shoveTimer == shoveBackgroundEnterDelay + shoveTime - 1f)
                    {
                        MoonlordDeathDrama.RequestLight(1f, NPC.Center);
                        typeof(MoonlordDeathDrama).GetField("whitening", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 1f);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 sliceCenter = (Hands[0].Center + Hands[1].Center) * 0.5f;
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 sliceDirection = (Pi * i / 4f).ToRotationVector2();
                                Vector2 sliceSpawnPosition = sliceCenter - sliceDirection * sliceLength * 0.5f;
                                NewProjectileBetter(sliceSpawnPosition, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, sliceTelegraphTime, sliceLength);
                            }
                        }

                        ScreenShatterSystem.CreateShatterEffect(NPC.Center - Main.screenPosition);
                        ZPosition = 0f;
                        NPC.Center = Target.Center - Vector2.UnitY * 1100f;
                        NPC.netUpdate = true;
                    }
                }

                shoveTimer++;
            }
            else
            {
                ZPosition = 0f;
                HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 1f, 0.25f);

                if (shoveTimer >= shoveBackgroundEnterDelay + shoveTime + 108f)
                {
                    DestroyAllHands();
                    SelectNextAttack();
                }
                shoveTimer++;
            }

            // Update hands.
            if (Hands.Count >= 2)
            {
                Hands[0].ScaleFactor = Clamp(Hands[0].ScaleFactor - 0.5f, 1f, 16f);
                Hands[1].ScaleFactor = Clamp(Hands[1].ScaleFactor - 0.5f, 1f, 16f);
                Hands[0].UsePalmForm = true;
                Hands[1].UsePalmForm = true;
                if (shoveTimer >= 1f)
                    Hands[0].CanDoDamage = Hands[1].CanDoDamage = ZPosition < 1f && ZPosition >= -0.8f;

                DefaultHandDrift(Hands[0], leftHandHoverDestination, 4f);
                DefaultHandDrift(Hands[1], rightHandHoverDestination, 4f);
            }
        }

        public void DoBehavior_TimeManipulation()
        {
            int redirectTime = 35;
            int attackDuration = 1160;
            var clocks = AllProjectilesByID(ModContent.ProjectileType<ClockConstellation>());
            ref float attackHasConcluded = ref NPC.ai[2];

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Hover near the target at first.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 400f, -250f);
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.29f);
                NPC.Opacity = 1f;

                // Make the background dark.
                attackHasConcluded = 0f;
                HeavenlyBackgroundIntensity = Lerp(1f, 0.18f, AttackTimer / redirectTime);
                SeamScale = 0f;
            }

            // Teleport away after redirecting and create a clock constellation on top of the target.
            if (AttackTimer == redirectTime)
            {
                SoundEngine.PlaySound(SupernovaSound);
                SoundEngine.PlaySound(ScreamSound);
                SoundEngine.PlaySound(ExplosionTeleportSound);
                Target.Calamity().GeneralScreenShakePower = 12f;

                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<ClockConstellation>(), 0, 0f);
                }

                TeleportTo(Target.Center + Vector2.UnitY * 2000f);
            }

            // Stay below the target, invisible, after redirecting.
            if (AttackTimer >= redirectTime && attackHasConcluded == 0f)
            {
                NPC.Opacity = 0f;
                NPC.Center = Target.Center + Vector2.UnitY * 2000f;

                // Burn the target if they try to leave the clock.
                if (clocks.Any() && !Target.WithinRange(clocks.First().Center, 1040f))
                    Target.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), Main.rand.Next(900, 950), 0);
            }

            if (AttackTimer >= attackDuration && attackHasConcluded == 0f)
            {
                foreach (var clock in clocks)
                {
                    clock.Kill();
                    TeleportTo(clock.Center);
                }

                SoundEngine.PlaySound(SupernovaSound);
                SoundEngine.PlaySound(ScreamSound);
                SoundEngine.PlaySound(ExplosionTeleportSound);
                Target.Calamity().GeneralScreenShakePower = 12f;

                ScreenEffectSystem.SetFlashEffect(NPC.Center, 5f, 90);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    attackHasConcluded = 1f;
                    NPC.Opacity = 1f;
                    NPC.netUpdate = true;

                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }
            }

            // Make the background return.
            if (attackHasConcluded == 1f)
            {
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity + 0.05f, 0f, 1f);
                if (HeavenlyBackgroundIntensity >= 1f)
                    SelectNextAttack();
            }
        }
    }
}
