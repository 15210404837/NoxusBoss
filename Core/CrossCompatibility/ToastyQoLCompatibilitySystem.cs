using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class ToastyQoLCompatibilitySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Don't load anything if Toasty's QoL mod is not enabled.
            if (ToastyQoL is null)
                return;

            // Collect all items that should adhere to Toasty's QoL.
            var modItemsWithBossChecklistSupport = Mod.GetContent().Where(c =>
            {
                return c is ModItem and IToastyQoLChecklistSupport;
            }).Select(c => c as ModItem);

            Dictionary<ToastyQoLRequirement, List<int>> requirementItems = new();

            // Load information into the list.
            foreach (var modItem in modItemsWithBossChecklistSupport)
            {
                IToastyQoLChecklistSupport qolInfo = modItem as IToastyQoLChecklistSupport;

                if (!requirementItems.ContainsKey(qolInfo.Requirement))
                    requirementItems[qolInfo.Requirement] = new();

                requirementItems[qolInfo.Requirement].Add(modItem.Type);
            }

            // Use the mod call.
            foreach (var requirement in requirementItems.Keys)
                ToastyQoL.Call("AddNewBossLockInformation", requirement.Requirement, requirement.RequirementName, requirementItems[requirement], false);
        }
    }
}
