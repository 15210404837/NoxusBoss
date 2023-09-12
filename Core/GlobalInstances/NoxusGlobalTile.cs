using NoxusBoss.Content.Subworlds;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalTile : GlobalTile
    {
        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            if (!EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
                return;

            // Erase tombstones in the garden.
            if (type == TileID.Tombstones)
                Main.tile[i, j].Get<TileWallWireStateData>().HasTile = false;
        }

        public static bool IsTileUnbreakable(int x, int y)
        {
            if (EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
                return true;

            return false;
        }

        public override bool CanExplode(int i, int j, int type)
        {
            if (IsTileUnbreakable(i, j))
                return false;

            return true;
        }

        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            if (IsTileUnbreakable(i, j))
                return false;

            return true;
        }
    }
}
