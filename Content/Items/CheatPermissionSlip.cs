using CalamityMod.Rarities;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class CheatPermissionSlip : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostXeroc;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = 0;
            Item.rare = ModContent.RarityType<CalamityRed>();
        }
    }
}
