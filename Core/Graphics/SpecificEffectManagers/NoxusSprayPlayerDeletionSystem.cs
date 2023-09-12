using System.Threading;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Capture;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class NoxusSprayPlayerDeletionSystem : ModSystem
    {
        public static bool PlayerWasDeleted
        {
            get;
            set;
        }

        public static Vector2 ScreenPositionAtPointOfDeletion
        {
            get;
            set;
        }

        public static int DeletionTimer
        {
            get;
            set;
        }

        public static int MainMenuReturnDelay
        {
            get;
            set;
        }

        public override void PostUpdateEverything()
        {
            if (PlayerWasDeleted)
            {
                Main.LocalPlayer.immuneAlpha = 255;
                Main.blockInput = true;
                Main.hideUI = true;
                Main.LocalPlayer.dead = true;
                Main.LocalPlayer.respawnTimer = 100;

                // Store the screen position at the time of the screen being deleted.
                if (DeletionTimer == 1)
                    ScreenPositionAtPointOfDeletion = Main.screenPosition;

                DeletionTimer++;

                // Kick the player to the game menu after being gone for long enough.
                if (DeletionTimer >= 210)
                {
                    Main.blockInput = false;
                    Main.hideUI = false;

                    Main.menuMode = 10;
                    Main.gameMenu = true;
                    Main.hideUI = false;
                    XerocTipsOverrideSystem.UseSprayText = true;

                    ThreadPool.QueueUserWorkItem(new WaitCallback(context =>
                    {
                        int netMode = Main.netMode;
                        if (netMode == NetmodeID.SinglePlayer)
                            WorldFile.CacheSaveTime();

                        Main.invasionProgress = -1;
                        Main.invasionProgressDisplayLeft = 0;
                        Main.invasionProgressAlpha = 0f;
                        Main.invasionProgressIcon = 0;
                        Main.menuMode = 10;
                        Main.gameMenu = true;
                        SoundEngine.StopTrackedSounds();
                        SoundEngine.PlaySound(XerocBoss.DoNotVoiceActedSound);
                        MainMenuReturnDelay = 1;

                        CaptureInterface.ResetFocus();
                        Main.ActivePlayerFileData.StopPlayTimer();
                        Player.SavePlayer(Main.ActivePlayerFileData);
                        Player.ClearPlayerTempInfo();
                        Rain.ClearRain();
                        if (netMode == NetmodeID.SinglePlayer)
                            WorldFile.SaveWorld();
                        else
                        {
                            Netplay.Disconnect = true;
                            Main.netMode = NetmodeID.SinglePlayer;
                        }
                        Main.fastForwardTimeToDawn = false;
                        Main.fastForwardTimeToDusk = false;
                        Main.UpdateTimeRate();
                    }));

                    DeletionTimer = 0;
                    PlayerWasDeleted = false;
                }
            }
        }

        public override void ModifyScreenPosition()
        {
            if (DeletionTimer >= 2)
                Main.screenPosition = ScreenPositionAtPointOfDeletion;
        }
    }
}
