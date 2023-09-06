using CalamityMod.Rarities;
using NoxusBoss.Content.Tiles;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable.Monoliths
{
    public class DivineMonolith : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostXeroc;

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<DivineMonolithTile>());
            Item.rare = ModContent.RarityType<CalamityRed>();
        }
    }
}
