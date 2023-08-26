using NoxusBoss.Content.Items.Placeable.Trophies;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class NoxusTrophyTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.addTile(Type);

            AddMapEntry(new(120, 85, 60), Language.GetText("MapObject.Trophy"));
            DustType = 7;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            if (frameX == 0 && frameY == 0)
                Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 32, ModContent.ItemType<NoxusTrophy>());
        }
    }
}
