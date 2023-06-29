using CalamityMod.Items;
using CalamityMod.Rarities;
using NoxusBoss.Content.Bosses.Noxus;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class NoxiousEvocator : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Noxious Evocator");
            Tooltip.SetDefault("Makes cosmic hallucinations materialize around you");
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ModContent.RarityType<Violet>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                NoxusFumes.CreateIllusions(player);
        }

        public override void UpdateVanity(Player player) => NoxusFumes.CreateIllusions(player);
    }
}
