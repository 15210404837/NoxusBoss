﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace NoxusBoss.Content.Items
{
    [AutoloadEquip(EquipType.Head)]
    public class NoxusMask : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            if (Main.netMode != NetmodeID.Server)
                ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 20;
            Item.rare = ItemRarityID.Blue;
            Item.vanity = true;
        }
    }
}

