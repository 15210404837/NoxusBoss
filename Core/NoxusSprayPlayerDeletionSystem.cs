using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
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
                    WorldGen.SaveAndQuit();

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
