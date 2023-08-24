using NoxusBoss.Content.Subworlds;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalTile : GlobalTile
    {
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
