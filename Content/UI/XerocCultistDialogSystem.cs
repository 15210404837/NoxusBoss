using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace NoxusBoss.Content.UI
{
    public class XerocCultistDialogSystem : ModSystem
    {
        private UserInterface dialogUserInterface;

        internal XerocCultistDialogUI DialogUI;

        public override void Load()
        {
            dialogUserInterface = new();

            // Initialize the underlying UI state.
            DialogUI = new();
            DialogUI.Activate();
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Disable the UI based if the cultist is not present.
            if (dialogUserInterface.CurrentState is not null && !NPC.AnyNPCs(ModContent.NPCType<XerocCultist>()))
                HideUI();

            if (dialogUserInterface?.CurrentState is not null)
                dialogUserInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text", StringComparison.Ordinal));
            if (mouseTextIndex != -1)
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("NoxusBoss: Cultist Dialog", DrawUIWrapper, InterfaceScaleType.UI));
        }

        private bool DrawUIWrapper()
        {
            if (dialogUserInterface?.CurrentState is not null)
                dialogUserInterface.Draw(Main.spriteBatch, new GameTime());
            return true;
        }

        public static void ShowUI()
        {
            ModContent.GetInstance<XerocCultistDialogSystem>().DialogUI = new();
            ModContent.GetInstance<XerocCultistDialogSystem>().DialogUI.Activate();

            var ui = ModContent.GetInstance<XerocCultistDialogSystem>().dialogUserInterface;
            var uiState = ModContent.GetInstance<XerocCultistDialogSystem>().DialogUI;
            ui?.SetState(uiState);
        }

        public static void HideUI() => ModContent.GetInstance<XerocCultistDialogSystem>().dialogUserInterface?.SetState(null);
    }
}
