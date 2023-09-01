using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Projectiles.Pets;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Pets
{
    public class EmpyreanEmberlet : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostXeroc;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToVanitypet(ModContent.ProjectileType<BabyXeroc>(), ModContent.BuffType<BabyXerocBuff>());
            Item.width = 16;
            Item.height = 24;
            Item.rare = ItemRarityID.Master;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.master = true;
            Item.value = Item.sellPrice(0, 5);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Find(t => t.Name == "Master").Text += " or Revengeance";
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // The item applies the buff, the buff spawns the projectile.
            player.AddBuff(Item.buffType, 2);
            return false;
        }
    }
}

