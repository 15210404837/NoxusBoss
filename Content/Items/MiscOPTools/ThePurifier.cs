using System.Collections.Generic;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Projectiles.Typeless;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.MiscOPTools
{
    public class ThePurifier : ModItem, IToastyQoLChecklistSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostXeroc;

        public static readonly SoundStyle BuildupSound = new("NoxusBoss/Assets/Sounds/Custom/PurifierBuildup");

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 16;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useAnimation = Item.useTime = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = false;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.value = 0;
            Item.rare = ModContent.RarityType<CalamityRed>();
            Item.shoot = ModContent.ProjectileType<ThePurifierProj>();
            Item.shootSpeed = 8f;
            Item.maxStack = 9999;
            Item.consumable = true;
        }

        // This only works on singleplayer because:
        // 1. Holy fuck this would be terrible if a single guy decided to throw this to cause maximal chaos.
        // 2. I don't want to get into the headache that would be accounting for world/tile syncs during worldgen when a player is in the world.
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0 && Main.netMode == NetmodeID.SinglePlayer;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Ensure that the tooltip is colored with a danger-indicating red.
            foreach (var tooltip in tooltips)
            {
                if (!tooltip.Name.Contains("Tooltip"))
                    continue;

                tooltip.OverrideColor = Color.Lerp(Color.OrangeRed, Color.Red, Cos01(Main.GlobalTimeWrappedHourly * 5f));
            }
        }
    }
}
