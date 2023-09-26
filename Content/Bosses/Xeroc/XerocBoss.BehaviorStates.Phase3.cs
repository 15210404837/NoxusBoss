using System.Linq;
using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc.Projectiles;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Music;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers.XerocSky;

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

                    // Play mumble sounds.
                    PerformMumble();
                }
                else
                    NPC.velocity *= 0.8f;

                // Update wings and teeth.
                UpdateWings(AttackTimer / redirectTime);
                PerformTeethChomp(AttackTimer / redirectTime);

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
                    NPC.Center = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 450f, -100f);
                    NPC.netUpdate = true;

                    LocalScreenSplitSystem.Start(NPC.Center, 20, PiOver2 * 0.9999f, 500f);
                    SoundEngine.PlaySound(ScreamSound with { Volume = 2f });
                    SoundEngine.PlaySound(SupernovaSound with { Volume = 8f });
                    ScreenEffectSystem.SetFlashEffect(NPC.Center, 8f, 60);
                    XerocKeyboardShader.BrightnessIntensity = 1f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, Vector2.UnitY, ModContent.ProjectileType<SuperLightBeam>(), SuperLaserbeamDamage * 5, 0f);
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

            // Update universal hands.
            DefaultUniversalHandMotion();
        }

        public void DoBehavior_SuperCosmicLaserbeam()
        {
            int attackDelay = BookConstellation.ConvergeTimeConst + 150;
            int shootTime = SuperCosmicBeam.DefaultLifetime;
            int realityTearReleaseRate = 75;
            ref float laserDirection = ref NPC.ai[2];

            Vector2 laserStart = NPC.Center + laserDirection.ToRotationVector2() * 300f;

            // Flap wings.
            UpdateWings(AttackTimer / 54f % 1f);

            // Teleport above the player and cast the book on the first frame.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center + new Vector2(-480f, -250f));
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.UnitX, ModContent.ProjectileType<BookConstellation>(), 0, 0f);
            }

            // Make the sky more pale.
            if (AttackTimer <= 75f)
                DifferentStarsInterpolant = GetLerpValue(0f, 60f, AttackTimer, true);
            HeavenlyBackgroundIntensity = Lerp(1f, 0.5f, DifferentStarsInterpolant);

            // Look at the target.
            PupilOffset = Vector2.Lerp(PupilOffset, (Target.Center - PupilPosition).SafeNormalize(Vector2.UnitY) * 40f, 0.12f);
            PupilScale = Lerp(PupilScale, 0.425f, 0.15f);

            // Periodically fire reality tears at the starting point of the laser.
            if (AttackTimer >= attackDelay && AttackTimer <= attackDelay + shootTime - 60f && AttackTimer % realityTearReleaseRate == 0f)
            {
                SoundEngine.PlaySound(SliceSound, PupilPosition);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float sliceAngle = Pi * i / 3f + laserDirection + PiOver2;
                        Vector2 sliceDirection = sliceAngle.ToRotationVector2();
                        NewProjectileBetter(laserStart - sliceDirection * 2000f, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, 30f, 4000f);
                    }

                    if (Target.WithinRange(NPC.Center, 335f))
                    {
                        for (int i = 0; i < 3; i++)
                            NewProjectileBetter(PupilPosition, (Target.Center - PupilPosition).SafeNormalize(Vector2.UnitY) * 5.6f + Main.rand.NextVector2Circular(0.9f, 0.9f), ModContent.ProjectileType<Starburst>(), StarburstDamage, 0f);
                    }
                }
            }

            // Periodically create screen pulse effects.
            if (AttackTimer >= attackDelay && AttackTimer % 30f == 0f)
            {
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 0.2f, 15);
                RadialScreenShoveSystem.Start(Vector2.Lerp(laserStart, Target.Center, 0.9f), 20);
            }

            // Play mumble sounds.
            if (AttackTimer == attackDelay - 40f)
                PerformMumble();

            // Create the super laser.
            if (AttackTimer == attackDelay)
            {
                CosmicLaserSound?.Stop();
                CosmicLaserSound = LoopedSoundManager.CreateNew(CosmicLaserStartSound, CosmicLaserLoopSound, () => !NPC.active || CurrentAttack != XerocAttackType.SuperCosmicLaserbeam);

                // Shake the screen.
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, NPC.SafeDirectionTo(Target.Center), 42f, 2.75f, 112));
                HighContrastScreenShakeShaderData.ContrastIntensity = 14.5f;

                ScreenEffectSystem.SetFlashEffect(NPC.Center, 2f, 60);
                RadialScreenShoveSystem.Start(NPC.Center, 54);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                    NewProjectileBetter(NPC.Center, laserDirection.ToRotationVector2(), ModContent.ProjectileType<SuperCosmicBeam>(), SuperLaserbeamDamage, 0f, -1, 0f, SuperCosmicBeam.DefaultLifetime);
                }
            }

            // Update the laser sound.
            if (AttackTimer >= attackDelay)
            {
                // Make the color contrast dissipate after the initial explosion.
                HighContrastScreenShakeShaderData.ContrastIntensity = Clamp(HighContrastScreenShakeShaderData.ContrastIntensity - 0.24f, 0f, 20f);

                // Make all other sounds rapidly fade out.
                float attackCompletion = GetLerpValue(0f, shootTime - 30f, AttackTimer - attackDelay, true);
                float muffleInterpolant = GetLerpValue(attackDelay, attackDelay + 17f, AttackTimer, true) * GetLerpValue(attackDelay + shootTime + 32f, attackDelay + shootTime - 40f, AttackTimer, true);
                SoundMufflingSystem.MuffleFactor = Lerp(1f, 0.009f, muffleInterpolant);
                MusicVolumeManipulationSystem.MusicMuffleFactor = muffleInterpolant;

                if (AttackTimer % 5f == 0f)
                {
                    // For some reason the audio engine has a stroke near the end of the attack and makes this do a stutter-like glitch.
                    // This isn't anything I explicitly programmed and is effectively unintended behavior, but it fits Xeroc so I'll leave it alone for now.
                    CosmicLaserSound.Update(Main.LocalPlayer.Center, sound =>
                    {
                        float fadeOut = GetLerpValue(0.98f, 0.93f, attackCompletion, true);
                        float ringingInterpolant = GetLerpValue(0.98f, 0.93f, SoundMufflingSystem.EarRingingIntensity, true) * GetLerpValue(0.12f, 0.4f, SoundMufflingSystem.EarRingingIntensity, true);
                        sound.Sound.Volume = Main.soundVolume * (Lerp(0.05f, 1.5f, muffleInterpolant) * Lerp(1f, 0.04f, ringingInterpolant) + GetLerpValue(0.15f, 0.05f, attackCompletion, true) * 0.4f);
                        sound.Sound.Pitch = Lerp(0.01f, 0.6f, Pow(attackCompletion, 1.36f));
                    });
                }
                SoundMufflingSystem.EarRingingIntensity *= 0.995f;
                if (AttackTimer >= attackDelay + shootTime + 32f)
                    CosmicLaserSound.Stop();
            }

            // Keep the keyboard shader brightness at its maximum.
            if (AttackTimer >= attackDelay && AttackTimer < attackDelay + shootTime)
                XerocKeyboardShader.BrightnessIntensity = 1f;

            // Very slowly fly towards the target.
            if (NPC.WithinRange(Target.Center, 40f))
                NPC.velocity *= 0.92f;
            else
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(Target.Center) * 2f, 0.15f);

            // Spin the laser towards the target. If the player runs away it locks onto them.
            float laserAngularVelocity = Remap(NPC.Distance(Target.Center), 1150f, 1775f, 0.0161f, 0.074f);
            float idealLaserDirection = NPC.AngleTo(Target.Center);
            laserDirection = laserDirection.AngleLerp(idealLaserDirection, laserAngularVelocity);

            // Update universal hands.
            DefaultUniversalHandMotion();
            if (Hands.Count >= 2)
            {
                Hands[0].Rotation = Pi - PiOver2;
                Hands[1].Rotation = Pi + PiOver2;
            }

            if (AttackTimer >= attackDelay + shootTime)
            {
                DifferentStarsInterpolant = Clamp(DifferentStarsInterpolant - 0.05f, 0f, 1f);
                if (AttackTimer >= attackDelay + shootTime + 45f)
                    SelectNextAttack();
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

            // Update teeth.
            PerformTeethChomp(AttackTimer / 45f % 1f);

            // Make the robe's eyes stare at the target.
            RobeEyesShouldStareAtTarget = true;

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
                StartShake(12f);
                SoundEngine.PlaySound(SupernovaSound);
                SoundEngine.PlaySound(ScreamSound);
                SoundEngine.PlaySound(ExplosionTeleportSound);
                XerocKeyboardShader.BrightnessIntensity = 0.6f;

                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<ClockConstellation>(), 0, 0f);
                }

                TeleportTo(Target.Center + Vector2.UnitY * 2000f);

                // Play a sound to accompany the converging stars.
                SoundEngine.PlaySound(StarConvergenceSound);
            }

            // Stay at the top of the clock after redirecting.
            if (AttackTimer >= redirectTime && AttackTimer <= attackDuration - 90f && attackHasConcluded == 0f)
            {
                NPC.Opacity = 1f;

                if (clocks.Any())
                    NPC.Center = clocks.First().Center - Vector2.UnitY * (Cos(AttackTimer / 34.5f) * 50f + 900f);

                // Burn the target if they try to leave the clock.
                if (clocks.Any() && !Target.WithinRange(clocks.First().Center, 932f) && AttackTimer >= redirectTime + 60f)
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
                SoundEngine.PlaySound(ExplosionTeleportSound);
                StartShake(12f);

                ScreenEffectSystem.SetFlashEffect(NPC.Center, 5f, 90);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    attackHasConcluded = 1f;
                    NPC.Opacity = 1f;
                    NPC.netUpdate = true;

                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }
            }

            // Update universal hands.
            DefaultUniversalHandMotion();
            if (Hands.Count >= 2)
            {
                Hands[0].RobeDirection = -1;
                Hands[1].RobeDirection = 1;
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
