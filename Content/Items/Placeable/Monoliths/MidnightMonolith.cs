using CalamityMod.Rarities;
using NoxusBoss.Content.Tiles;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable.Monoliths
{
    public class MidnightMonolith : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<MidnightMonolithTile>());
            Item.rare = ModContent.RarityType<Violet>();
        }
    }
}
