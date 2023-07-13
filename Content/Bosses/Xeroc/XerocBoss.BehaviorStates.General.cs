using System.Linq;
using System.Reflection;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Noxus;
using NoxusBoss.Content.MainMenuThemes;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core;
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
        public void DoBehavior_Awaken()
        {
            int starRecedeDelay = 120;
            int starRecedeTime = 60;
            int eyeAppearTime = 56;
            int eyeObserveTime = 108;
            int pupilContractDelay = 64;
            int seamAppearTime = 5;
            int seamGrowTime = 120;

            // NO. You do NOT get adrenaline for sitting around and doing nothing.
            Target.Calamity().adrenaline = 0f;

            if (WorldSaveSystem.HasMetXeroc)
            {
                starRecedeDelay = 8;
                starRecedeTime = 45;
                eyeAppearTime = 30;
                eyeObserveTime = 96;
                pupilContractDelay = 12;
                seamGrowTime = 30;
            }

            // Close the HP bar.
            NPC.Calamity().ShouldCloseHPBar = true;
            NPC.Calamity().ProvidesProximityRage = false;

            // Disable music.
            Music = 0;

            // Make some screen shake effects happen.
            if (AttackTimer < starRecedeDelay)
            {
                float screenShakeIntensityInterpolant = Pow(AttackTimer / starRecedeDelay, 1.84f);
                Target.Calamity().GeneralScreenShakePower = Lerp(2f, 11.5f, screenShakeIntensityInterpolant);
                return;
            }

            // Make the stars recede away in fear.
            StarRecedeInterpolant = GetLerpValue(starRecedeDelay, starRecedeDelay + starRecedeTime, AttackTimer, true);
            if (StarRecedeInterpolant < 1f)
                return;

            // Make the eye appear.
            SkyEyeOpacity = GetLerpValue(starRecedeDelay + starRecedeTime, starRecedeDelay + starRecedeTime + eyeAppearTime, AttackTimer, true);
            if (AttackTimer == starRecedeDelay + starRecedeTime + eyeAppearTime - 30f)
                SoundEngine.PlaySound(BossRushEvent.TeleportSound);

            float pupilScaleInterpolant = GetLerpValue(0f, 10f, AttackTimer - starRecedeDelay - starRecedeTime - eyeAppearTime + 20f, true);
            float pupilContractInterpolant = Pow(GetLerpValue(18f, pupilContractDelay, AttackTimer - starRecedeDelay - starRecedeTime - eyeAppearTime - eyeObserveTime, true), 0.25f);

            // Make the eye look at the player.
            SkyEyeDirection = (Target.Center - Main.screenPosition - EyeDrawPosition).ToRotation();

            SkyPupilOffset = Vector2.Lerp(SkyPupilOffset, SkyEyeDirection.ToRotationVector2() * (pupilContractInterpolant * 28f + 50f), 0.3f);
            SkyPupilScale = Pow(pupilScaleInterpolant, 1.7f) - pupilContractInterpolant * 0.5f;

            // Make the eye disappear before the seam appears.
            if (AttackTimer >= starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay - 48f)
            {
                if (AttackTimer == starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay - 35f)
                    EntropicGod.CreateTwinkle(EyeDrawPosition + Main.screenPosition, Vector2.One * 2f);

                SkyEyeScale *= 0.7f;
                if (SkyEyeScale <= 0.15f)
                    SkyEyeScale = 0f;
            }

            // Make the screen tear seam appear.
            if (AttackTimer == starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay - 4f)
            {
                SoundEngine.PlaySound(BossRushEvent.Tier2TransitionSound with { Pitch = 0.4f });
                Target.Calamity().GeneralScreenShakePower = 16f;
            }

            // Make the seam appear suddenly.
            SeamScale = GetLerpValue(0f, seamAppearTime, AttackTimer - starRecedeDelay - starRecedeTime - eyeAppearTime - eyeObserveTime - pupilContractDelay, true);

            // Make the seam grow after appearing.
            SeamScale += Pow(GetLerpValue(0f, seamGrowTime, AttackTimer - starRecedeDelay - starRecedeTime - eyeAppearTime - eyeObserveTime - pupilContractDelay - seamAppearTime, true), 1.9f) * 4f;

            // Open the seam in the next state.
            // This is done in a separate state because honestly these GetLerpValue lines are getting ridiculous.
            if (AttackTimer >= starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay + seamAppearTime + seamGrowTime + 30f)
                SelectNextAttack();
        }

        public void DoBehavior_OpenScreenTear()
        {
            int gripTime = 60;
            int ripOpenTime = 18;
            int backgroundAppearDelay = 240;
            int backgroundAppearTime = 180;

            // NO. You do NOT get adrenaline for sitting around and doing nothing.
            Target.Calamity().adrenaline = 0f;

            if (WorldSaveSystem.HasMetXeroc)
            {
                gripTime = 36;
                backgroundAppearDelay = 120;
                backgroundAppearTime = 30;
            }

            // Close the HP bar.
            NPC.Calamity().ShouldCloseHPBar = true;
            NPC.Calamity().ProvidesProximityRage = false;

            // Disable music.
            Music = 0;

            // Keep the seam scale at its minimum at first.
            SeamScale = 5f;

            // Stay invisible.
            NPC.Opacity = 0f;

            // Create many hands that will tear apart the screen on the first few frames.
            if (AttackTimer <= 16f && AttackTimer % 2f == 0f)
            {
                int handIndex = (int)AttackTimer / 2;
                float verticalOffset = handIndex * 40f + 250f;
                if (handIndex % 2 == 0)
                    verticalOffset *= -1f;

                Hands.Add(new()
                {
                    Center = Target.Center - Vector2.UnitX.RotatedBy(-SeamAngle) * verticalOffset,
                    Velocity = Main.rand.NextVector2CircularEdge(17f, 17f),
                    Opacity = 0f
                });
                return;
            }

            // Make chromatic aberration effects happen periodically.
            if (AttackTimer % 20f == 19f && HeavenlyBackgroundIntensity <= 0.1f)
            {
                float aberrationIntensity = Remap(AttackTimer, 0f, 120f, 0.4f, 1.6f);
                ScreenEffectSystem.SetChromaticAberrationEffect(Target.Center, aberrationIntensity, 10);
            }

            // Have the hands move above and below the player, on the seam.
            float handMoveInterpolant = Pow(GetLerpValue(0f, gripTime, AttackTimer, true), 3.2f) * 0.5f;

            Vector2 verticalOffsetDirection = Vector2.UnitX.RotatedBy(-SeamAngle - 0.045f);
            for (int i = 0; i < Hands.Count; i++)
            {
                bool left = i % 2 == 0;
                Vector2 handDestination = Target.Center + verticalOffsetDirection * -left.ToDirectionInt() * (i * 50f + 150f);
                Hands[i].Center = Vector2.Lerp(Hands[i].Center, handDestination, handMoveInterpolant);
                if (Hands[i].Center.WithinRange(handDestination, 60f))
                {
                    Hands[i].Rotation = Hands[i].Rotation.AngleLerp(PiOver2 * left.ToDirectionInt(), 0.3f);
                    Hands[i].ShouldOpen = false;
                }
                else
                    Hands[i].Rotation = (handDestination - Hands[i].Center).ToRotation();
            }

            // Rip open the seam.
            SeamScale += Pow(GetLerpValue(0f, ripOpenTime, AttackTimer - gripTime - 30f, true), 2f) * 250f;
            if (SeamScale >= 4f && HeavenlyBackgroundIntensity <= 0.3f)
                Target.Calamity().GeneralScreenShakePower = 20f;

            if (AttackTimer == gripTime + 30f)
                SoundEngine.PlaySound(BossRushEvent.Tier5TransitionSound);

            // Delete the hands once the seam is fully opened.
            if (AttackTimer == gripTime + ripOpenTime + 60f)
            {
                DestroyAllHands();
                NPC.netUpdate = true;
            }

            // Make the natural background appear.
            HeavenlyBackgroundIntensity = GetLerpValue(0f, backgroundAppearTime, AttackTimer - gripTime - ripOpenTime - backgroundAppearDelay, true);

            if (AttackTimer >= gripTime + ripOpenTime + backgroundAppearDelay + backgroundAppearTime)
            {
                SkyEyeScale *= 0.7f;
                if (SkyEyeScale <= 0.15f)
                    SkyEyeScale = 0f;

                if (AttackTimer >= gripTime + ripOpenTime + backgroundAppearDelay + backgroundAppearTime + 75f)
                {
                    // Mark Xeroc as having been met for next time, so that the player doesn't have to wait as long.
                    if (!WorldSaveSystem.HasMetXeroc && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        WorldSaveSystem.HasMetXeroc = true;
                        CalamityNetcode.SyncWorld();
                    }

                    SelectNextAttack();
                }
            }
        }

        public void DoBehavior_RoarAnimation()
        {
            int screamTime = 210;

            // Appear on the foreground.
            if (AttackTimer == 1f)
            {
                NPC.Center = Target.Center - Vector2.UnitX * 300f;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;

                SoundEngine.PlaySound(ExplosionTeleportSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
            }

            // Bring the music.
            Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Xeroc");

            // Flap wings.
            UpdateWings(AttackTimer / 54f % 1f);

            // Jitter in place and scream.
            if (AttackTimer % 10f == 0f && AttackTimer <= screamTime - 75f)
            {
                if (AttackTimer % 30f == 0f || AttackTimer <= 4f)
                    SoundEngine.PlaySound(ScreamSound with { Volume = 1.2f });
                Color burstColor = Main.rand.NextBool() ? Color.LightGoldenrodYellow : Color.Lerp(Color.White, Color.IndianRed, 0.7f);

                // Create blur and burst particle effects.
                ExpandingChromaticBurstParticle burst = new(EyePosition, Vector2.Zero, burstColor, 16, 0.1f);
                GeneralParticleHandler.SpawnParticle(burst);
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);
                Target.Calamity().GeneralScreenShakePower = 15f;
            }
            NPC.Center += Main.rand.NextVector2Circular(12.5f, 12.5f);

            NPC.Opacity = 1f;

            if (AttackTimer >= screamTime)
                SelectNextAttack();
        }

        public void DoBehavior_EnterPhase2()
        {
            // TODO -- Implement this.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitY * 350f);
                Target.Calamity().GeneralScreenShakePower = 12f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                SelectNextAttack();
            }
        }

        public void DoBehavior_EnterPhase3()
        {
            // TODO -- Implement this.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitY * 350f);
                Target.Calamity().GeneralScreenShakePower = 12f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                SelectNextAttack();
            }
        }

        public void DoBehavior_DeathAnimation()
        {
            int handCount = 222;
            int handReleaseRate = 1;
            int blackDelay = 210;
            int riseDelay = 60;
            int riseTime = 210;
            int chargeLineUpTime = 45;
            int screenShatterDelay = 14;
            int crashDelay = 120;
            ref float screenShattered = ref NPC.ai[2];

            // Stay at 1 HP.
            NPC.life = 1;
            NPC.dontTakeDamage = true;

            // Flap wings.
            UpdateWings(AttackTimer / 42f % 1f);

            Vector2 hoverDestinationForHand(int handIndex)
            {
                float goldenRatio = 1.618033f;
                return NPC.Center + (TwoPi * goldenRatio * handIndex).ToRotationVector2() * (handIndex * NPC.scale * 12f + 40f);
            }

            if (AttackTimer == 1f)
                DestroyAllHands();

            // Slow down and make the background go pitch black again while screaming at first.
            if (AttackTimer <= blackDelay)
            {
                if (AttackTimer == 1f)
                    TeleportTo(Target.Center + Vector2.UnitX * Target.direction * 500f);

                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.02f, 0f, 1f);
                SeamScale = 0f;
            }
            else
                UniversalBlackOverlayInterpolant = Clamp(UniversalBlackOverlayInterpolant + 0.06f, 0f, 1f);

            if (AttackTimer % 8f == 0f && AttackTimer <= blackDelay + riseDelay - 75f)
            {
                if (AttackTimer % 16f == 0f || AttackTimer <= 4f)
                    SoundEngine.PlaySound(ScreamSound with { Volume = 1.2f });
                Color burstColor = Main.rand.NextBool() ? Color.LightGoldenrodYellow : Color.Lerp(Color.White, Color.IndianRed, 0.7f);

                // Create blur and burst particle effects.
                ExpandingChromaticBurstParticle burst = new(EyePosition, Vector2.Zero, burstColor, 16, 0.1f);
                GeneralParticleHandler.SpawnParticle(burst);
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);
                Target.Calamity().GeneralScreenShakePower = 15f;
            }
            if (AttackTimer >= blackDelay + riseDelay - 75f)
                Main.hideUI = true;

            // Move into the background.
            if (AttackTimer <= blackDelay + riseDelay + riseTime && !Hands.Any())
                ZPosition = Pow(GetLerpValue(blackDelay + riseDelay, blackDelay + riseDelay + riseTime, AttackTimer, true), 1.6f) * 3.4f;

            // Display congratualtory text.
            if (AttackTimer == blackDelay + riseDelay + 60f)
            {
                DrawCongratulatoryText = true;
                SoundEngine.PlaySound(BossRushEvent.TeleportSound);
            }

            if (AttackTimer == blackDelay + riseDelay + riseTime - 1f)
            {
                DrawCongratulatoryText = false;
                SoundEngine.PlaySound(SoundID.MenuClose);
            }

            // Calculate the background hover position.
            Vector2 hoverDestination = Target.Center + Vector2.UnitY * (ZPosition * -30f - 200f);

            // Stay above the target while in the background.
            if (AttackTimer >= blackDelay + riseDelay)
            {
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.03f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, (hoverDestination - NPC.Center) * 0.07f, 0.06f);
            }

            // Cast a ridiculous quantity of arms outward.
            if (AttackTimer >= blackDelay + riseDelay + riseTime && AttackTimer % handReleaseRate == 0f && Hands.Count < handCount)
                ConjureHandsAtPosition(hoverDestinationForHand(Hands.Count), Vector2.Zero, false);

            // Move further into the background.
            if (AttackTimer >= blackDelay + riseDelay + riseTime + handCount * handReleaseRate && AttackTimer < blackDelay + riseDelay + riseTime + handCount * handReleaseRate + chargeLineUpTime)
            {
                ZPosition = Lerp(ZPosition, 7f, 0.08f);

                float hoverSpeedInterpolant = Remap(ZPosition, 3f, 7f, 0.03f, 0.8f);
                NPC.velocity *= 0.8f;
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * 100f, hoverSpeedInterpolant);
            }

            // Charge forward into the screen.
            if (AttackTimer >= blackDelay + riseDelay + riseTime + handCount * handReleaseRate + chargeLineUpTime)
            {
                ZPosition = Clamp(ZPosition - 0.6f, -0.94f, 10f);
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center, 0.08f);
                if (screenShattered == 0f && ZPosition <= -0.94f)
                {
                    SoundEngine.PlaySound(SupernovaSound with { Volume = 4f });
                    SoundEngine.PlaySound(ExplosionTeleportSound with { Volume = 4f });
                    Target.Calamity().GeneralScreenShakePower = 30f;
                    DestroyAllHands();
                    ScreenShatterSystem.CreateShatterEffect(NPC.Center - Main.screenPosition);
                    ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 3f, 90);

                    screenShattered = 1f;
                    NPC.netUpdate = true;
                }

                if (screenShattered == 1f)
                {
                    Music = 0;
                    Main.hideUI = true;
                }

                if (AttackTimer >= blackDelay + riseDelay + riseTime + handCount * handReleaseRate + chargeLineUpTime + screenShatterDelay + crashDelay)
                {
                    // Completely disappear in multiplayer.
                    NPC.active = false;

                    if (Main.netMode != NetmodeID.Server)
                    {
                        Main.menuMode = 10;
                        Main.gameMenu = true;
                        Main.hideUI = false;
                        XerocTipsOverrideSystem.UseDeathAnimationText = true;
                        WorldGen.SaveAndQuit();

                        // Forcefully change the menu theme to the Xeroc one if he was defeated for the first time.
                        // This is done half as an indicator that the world exist isn't a bug and half as a reveal that the option is unlocked.
                        if (!WorldSaveSystem.HasDefeatedXerocInAnyWorld)
                        {
                            WorldSaveSystem.HasDefeatedXerocInAnyWorld = true;

                            do
                                typeof(MenuLoader).GetMethod("OffsetModMenu", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { Main.rand.Next(-2, 3) });
                            while (((ModMenu)typeof(MenuLoader).GetField("switchToMenu", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).FullName != XerocDimensionMainMenu.Instance.FullName);
                        }
                    }
                }
            }

            // Keep hands moving.
            for (int i = 0; i < Hands.Count; i++)
            {
                DefaultHandDrift(Hands[i], hoverDestinationForHand(i), 5.6f);
                Hands[i].UsePalmForm = true;
            }
        }
    }
}
