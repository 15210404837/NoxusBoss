﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using NoxusBoss.Content.Tiles;
using System.Collections.Generic;

namespace NoxusBoss.Content.Items
{
    public class NoxusRelic : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<NoxusRelicTile>());
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

