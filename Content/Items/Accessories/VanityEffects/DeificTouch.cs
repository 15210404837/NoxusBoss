﻿using CalamityMod.Items;
using CalamityMod.Rarities;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Accessories.VanityEffects
{
    public class DeificTouch : ModItem
    {
        public static bool UsingEffect => !Main.gameMenu && Main.LocalPlayer.GetModPlayer<NoxusPlayer>().GetValue<bool>("DeificTouch") && XerocBoss.Myself is null;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            NoxusPlayer.ResetEffectsEvent += ResetValue;
        }

        private void ResetValue(NoxusPlayer p)
        {
            p.SetValue("DeificTouch", false);
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<CalamityRed>();
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                player.GetModPlayer<NoxusPlayer>().SetValue("DeificTouch", true);
        }

        public override void UpdateVanity(Player player) => player.GetModPlayer<NoxusPlayer>().SetValue("DeificTouch", true);
    }
}