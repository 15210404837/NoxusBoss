using CalamityMod.Rarities;
using NoxusBoss.Content.Items.Placeable.Trophies;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.LoreItems
{
    public class LoreNoxus : BaseLoreItem
    {
        public override int TrophyID => ModContent.ItemType<NoxusTrophy>();

        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<Violet>();
            base.SetDefaults();
        }
    }
}
