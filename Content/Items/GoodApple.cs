using CalamityMod.Rarities;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class GoodApple : ModItem, IToastyQoLChecklistItemSupport
    {
        public override string Texture => "NoxusBoss/Content/Tiles/FruitOfLife";

        // Acquired in the Eternal Garden (which is only accessible post-Noxus) but does not necessarily require Xeroc to be defeated.
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = 0;
            Item.maxStack = 9999;
            Item.rare = ModContent.RarityType<CalamityRed>();
        }
    }
}
