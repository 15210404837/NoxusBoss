using System.Collections.Generic;
using NoxusBoss.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable.Relics
{
    public class XerocRelic : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<XerocRelicTile>());
            Item.width = 30;
            Item.height = 40;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.buyPrice(0, 5);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Find(t => t.Name == "Master").Text += " or Revengeance";
        }
    }
}
