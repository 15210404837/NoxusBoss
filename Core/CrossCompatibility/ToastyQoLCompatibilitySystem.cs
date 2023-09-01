using System.Collections.Generic;
using System.Linq;
using Terraria.Localization;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class ToastyQoLCompatibilitySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            // Don't load anything if Toasty's QoL mod is not enabled.
            if (ToastyQoL is null)
                return;

            // Load item support.
            LoadItemSupport();

            // Load boss support.
            LoadBossSupport();
        }

        public void LoadItemSupport()
        {
            // Collect all items that should adhere to Toasty's QoL.
            var modItemsWithQoLSupport = Mod.GetContent().Where(c =>
            {
                return c is ModItem and IToastyQoLChecklistItemSupport;
            }).Select(c => c as ModItem);

            Dictionary<ToastyQoLRequirement, List<int>> requirementItems = new();

            // Load information into the list.
            foreach (var modItem in modItemsWithQoLSupport)
            {
                IToastyQoLChecklistItemSupport qolInfo = modItem as IToastyQoLChecklistItemSupport;

                if (!requirementItems.ContainsKey(qolInfo.Requirement))
                    requirementItems[qolInfo.Requirement] = new();

                requirementItems[qolInfo.Requirement].Add(modItem.Type);
            }

            // Use the mod call.
            foreach (var requirement in requirementItems.Keys)
                ToastyQoL.Call("AddNewBossLockInformation", requirement.Requirement, requirement.RequirementName, requirementItems[requirement], false);
        }

        public void LoadBossSupport()
        {
            // Collect all bosses that should adhere to Toasty's QoL.
            var modNPCsWithQoLSupport = Mod.GetContent().Where(c =>
            {
                return c is ModNPC and IToastyQoLChecklistBossSupport;
            }).Select(c => c as ModNPC);

            // Use the mod call.
            foreach (var modNPC in modNPCsWithQoLSupport)
            {
                IToastyQoLChecklistBossSupport qolInfo = modNPC as IToastyQoLChecklistBossSupport;
                string singularName = Language.GetTextValue($"Mods.{Mod.Name}.NPCs.{modNPC.Name}.DisplayNameSingular");
                ToastyQoL.Call("AddBossToggle", modNPC.BossHeadTexture, singularName, qolInfo.IsDefeatedField, qolInfo.ProgressionValue + 6f, 1f);
            }
        }
    }
}
