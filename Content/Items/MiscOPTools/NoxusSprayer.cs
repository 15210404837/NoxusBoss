﻿using System.Collections.Generic;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Noxus;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.NPCs;
using NoxusBoss.Content.Projectiles.Typeless;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.MiscOPTools
{
    public class NoxusSprayer : ModItem
    {
        public static List<int> NPCsToNotDelete => new()
        {
            NPCID.CultistTablet,
            NPCID.DD2LanePortal,
            NPCID.DD2EterniaCrystal,
            NPCID.TargetDummy,
            ModContent.NPCType<NoxusEgg>(),
            ModContent.NPCType<NoxusEggCutscene>(),
            ModContent.NPCType<EntropicGod>()
        };

        public static List<int> NPCsThatReflectSpray => new()
        {
            ModContent.NPCType<XerocBoss>(),
        };

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 34;
            Item.useAnimation = 2;
            Item.useTime = 2;
            Item.autoReuse = true;
            Item.noMelee = true;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item20 with { MaxInstances = 50, Volume = 0.3f };

            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<CalamityRed>();

            Item.shoot = ModContent.ProjectileType<NoxusSprayerGas>();
            Item.shootSpeed = 7f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new(0f, 4f);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position -= velocity * 4f;
        }
    }
}
