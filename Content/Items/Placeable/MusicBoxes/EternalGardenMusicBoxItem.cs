using NoxusBoss.Content.Tiles;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable.MusicBoxes
{
    public class EternalGardenMusicBoxItem : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostNoxus;

        public override void SetStaticDefaults()
        {
            // Music boxes can't get prefixes in vanilla.
            ItemID.Sets.CanGetPrefixes[Type] = false;

            // Recorded music boxes transform into the basic form in shimmer.
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;

            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/EternalGarden"), Type, ModContent.TileType<EternalGardenMusicBox>());
        }

        public override void SetDefaults()
        {
            Item.DefaultToMusicBox(ModContent.TileType<EternalGardenMusicBox>());
        }
    }
}
