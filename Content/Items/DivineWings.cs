using CalamityMod.Items;
using CalamityMod.Rarities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    [AutoloadEquip(EquipType.Wings)]
    public class DivineWings : ModItem
    {
        public static int WingSlotID
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;

            WingSlotID = Item.wingSlot;
            ArmorIDs.Wing.Sets.Stats[WingSlotID] = new WingStats(100000000, 16.67f, 3.7f);
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 30;
            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<CalamityRed>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) => player.noFallDmg = true;

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 2f;
            ascentWhenRising = 0.184f;
            maxCanAscendMultiplier = 1.2f;
            maxAscentMultiplier = 3.25f;
            constantAscend = 0.29f;
        }
    }
}
