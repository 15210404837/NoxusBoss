using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using NoxusBoss.Content.Tiles;

namespace NoxusBoss.Content.Items
{
    public class NoxusTrophy : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Noxus Trophy");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<NoxusTrophyTile>());
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(0, 1);
        }
    }
}

