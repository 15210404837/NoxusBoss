using CalamityMod;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Particles;
using NoxusBoss.Content.Projectiles;
using NoxusBoss.Core;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.Subworlds.EternalGarden;

namespace NoxusBoss.Content.Subworlds
{
    public class EternalGardenUpdateSystem : ModSystem
    {
        public static bool WasInSubworldLastUpdateFrame
        {
            get;
            private set;
        }

        public static int TimeSpentInCenter
        {
            get;
            private set;
        }

        public override void PreUpdateEntities()
        {
            // Reset the text opacity when the game is being played. It will increase up to full opacity during subworld transition drawing.
            TextOpacity = 0f;

            // Verify whether things are in the subworld.
            // TODO -- This might need to be done in another hook. Need to check whether this one is serverside only.
            bool inGarden = SubworldSystem.IsActive<EternalGarden>();
            if (WasInSubworldLastUpdateFrame != inGarden)
            {
                // A major flaw with respect to subworld data transfer is the fact that Calamity's regular OnWorldLoad hooks clear everything.
                // This works well and good for Calamity's purposes, but it causes serious issues when going between subworlds. The result of this is
                // ordered as follows:

                // 1. Exit world. Store necessary data for subworld transfer.
                // 2. Load necessary stuff for subworld and wait.
                // 3. Enter subworld. Load data from step 1.
                // 4. Call OnWorldLoad, resetting everything from step 3.

                // In order to address this, a final step is introduced:
                // 5. Load data from step 3 again on the first frame of entity updating.
                if (inGarden)
                {
                    LoadWorldDataFromTag();

                    // Set the respawn point now that the player is at the the initial spawn point already.
                    EternalGardenWorldGen.SetPlayerRespawnPoint();

                    // Create light flash teleport visuals on the player.
                    CreatePlayerLightFlashEffects();
                }

                WasInSubworldLastUpdateFrame = inGarden;
            }

            // Everything beyond this point applies solely to the subworld.
            if (!WasInSubworldLastUpdateFrame)
            {
                TimeSpentInCenter = 0;
                return;
            }

            // Keep it perpetually night time if Xeroc is not present.
            if (XerocBoss.Myself is null)
            {
                Main.dayTime = false;
                Main.time = 16200f;
            }

            // Keep the wind strong, so that the plants sway around.
            // This swiftly ceases if Xeroc is present, as though nature is fearful of him.
            if (XerocBoss.Myself is null)
                Main.windSpeedTarget = Lerp(0.88f, 1.32f, CalamityUtils.AperiodicSin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f);
            else
                Main.windSpeedTarget = 0f;
            Main.windSpeedCurrent = Lerp(Main.windSpeedCurrent, Main.windSpeedTarget, 0.03f);

            // Create a god ray at the center of the garden if Xeroc isn't present.
            int godRayID = ModContent.ProjectileType<GodRayVisual>();
            if (Main.netMode != NetmodeID.MultiplayerClient && XerocBoss.Myself is null && !AnyProjectiles(godRayID))
            {
                Vector2 centerOfWorld = new Point(Main.maxTilesX / 2, EternalGardenWorldGen.SurfaceTilePoint).ToWorldCoordinates() + Vector2.UnitY * 320f;
                NewProjectileBetter(centerOfWorld, 0.07f.ToRotationVector2() * 0.00001f, godRayID, 0, 0f);
            }

            // Check if anyone is in the center of the garden.
            bool anyoneInCenter = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;

                if (Distance(p.Center.X, Main.maxTilesX * 8f) <= (EternalGardenWorldGen.TotalFlatTilesAtCenter + 8f) * 16f)
                {
                    anyoneInCenter = true;
                    break;
                }
            }

            // Spawn Xeroc if a player has spent a sufficient quantity of time in the center of the garden.
            TimeSpentInCenter = Clamp(TimeSpentInCenter + (anyoneInCenter && XerocBoss.Myself is null).ToDirectionInt(), 0, 600);
            if (Main.netMode != NetmodeID.MultiplayerClient && TimeSpentInCenter >= 240 && XerocBoss.Myself is null)
            {
                NPC.NewNPC(new EntitySource_WorldEvent(), Main.maxTilesX * 8, EternalGardenWorldGen.SurfaceTilePoint * 16 - 800, ModContent.NPCType<XerocBoss>(), 1);
                TimeSpentInCenter = 0;
            }

            // Disable typical weather things.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Sandstorm.Happening)
                    Sandstorm.StopSandstorm();
                CalamityMod.CalamityMod.StopRain();
            }
        }

        public static void CreatePlayerLightFlashEffects()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;

                // Play the flash sound.
                SoundEngine.PlaySound(Providence.NearBurnSound with
                {
                    Pitch = 0.5f,
                    Volume = 1.56f,
                    MaxInstances = 8
                }, p.Center);

                // Create the particle effects.
                ExpandingGreyscaleCircleParticle circle = new(p.Center, Vector2.Zero, new(219, 194, 229), 10, 0.28f);
                VerticalLightStreakParticle bigLightStreak = new(p.Center, Vector2.Zero, new(228, 215, 239), 10, new(2.4f, 3f));
                MagicBurstParticle magicBurst = new(p.Center, Vector2.Zero, new(150, 109, 219), 12, 0.1f);

                GeneralParticleHandler.SpawnParticle(circle);
                GeneralParticleHandler.SpawnParticle(bigLightStreak);
                GeneralParticleHandler.SpawnParticle(magicBurst);

                // Shake the screen a little bit.
                ShakeScreen(p.Center, 6f);
            }
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (!WasInSubworldLastUpdateFrame)
                return;

            tileColor = Color.Wheat * 0.3f;
            backgroundColor = new(4, 6, 14);

            // Make the background brighter the closer the camera is to the center of the world.
            float centerOfWorld = Main.maxTilesX * 8f;
            float distanceToCenterOfWorld = Distance(Main.screenPosition.X + Main.screenWidth * 0.5f, centerOfWorld);
            float brightnessInterpolant = GetLerpValue(3200f, 1400f, distanceToCenterOfWorld, true);
            backgroundColor = Color.Lerp(backgroundColor, Color.LightCoral, brightnessInterpolant * 0.27f);
            tileColor = Color.Lerp(tileColor, Color.LightPink, brightnessInterpolant * 0.4f);

            // Make everything bright if Xeroc is present.
            tileColor = Color.Lerp(tileColor, Color.White, XerocSky.HeavenlyBackgroundIntensity);
        }
    }
}
