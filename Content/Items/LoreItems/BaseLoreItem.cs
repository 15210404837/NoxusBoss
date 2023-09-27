using CalamityMod;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.LoreItems
{
    public abstract class BaseLoreItem : ModItem
    {
        // Used for the purpose of creating custom lore tooltip colors. Overriding is not required.
        public virtual Color? LoreColor => null;

        // Used for automated crafting recipe loading.
        public abstract int TrophyID
        {
            get;
        }

        public override void SetStaticDefaults()
        {
            // All lore items float in the air.
            ItemID.Sets.ItemNoGravity[Type] = true;

            // All lore items only require a single acquirement to duplicate in Journey Mode.
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            // All lore items are stored in a separate item group.
            // Base Calamity uses a custom value of 12000 to represent said group, which is not a named variant in the ItemGroup enumeration, hence the explicit cast.
            itemGroup = (ContentSamples.CreativeHelper.ItemGroup)12000;
        }

        // Token: 0x06009F23 RID: 40739 RVA: 0x005A3058 File Offset: 0x005A1258
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine fullLore = new(Mod, "CalamityMod:Lore", this.GetLocalizedValue("Lore"))
            {
                OverrideColor = LoreColor
            };

            // Override vanilla tooltips and display the lore tooltip instead.
            CalamityUtils.HoldShiftTooltip(tooltips, new TooltipLine[]
            {
                fullLore
            }, true);
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddTile(TileID.Bookcases).
                AddIngredient(TrophyID).
                Register();
        }
    }
}
