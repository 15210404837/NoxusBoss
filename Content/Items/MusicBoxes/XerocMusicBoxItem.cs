using NoxusBoss.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.MusicBoxes
{
    public class XerocMusicBoxItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Music boxes can't get prefixes in vanilla.
            ItemID.Sets.CanGetPrefixes[Type] = false;

            // Recorded music boxes transform into the basic form in shimmer.
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;

            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Xeroc"), Type, ModContent.TileType<XerocMusicBox>());
        }

        public override void SetDefaults()
        {
            Item.DefaultToMusicBox(ModContent.TileType<XerocMusicBox>());
        }
    }
}
