using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        public static void LightHitWire(int type, int i, int j, int tileX, int tileY)
        {
            int x = i - Main.tile[i, j].TileFrameX / 18 % tileX;
            int y = j - Main.tile[i, j].TileFrameY / 18 % tileY;
            for (int l = x; l < x + tileX; l++)
            {
                for (int m = y; m < y + tileY; m++)
                {
                    if (Main.tile[l, m].HasTile && Main.tile[l, m].TileType == type)
                    {
                        if (Main.tile[l, m].TileFrameX < tileX * 18)
                            Main.tile[l, m].TileFrameX += (short)(tileX * 18);
                        else
                            Main.tile[l, m].TileFrameX -= (short)(tileX * 18);
                    }
                }
            }

            if (Wiring.running)
            {
                for (int k = 0; k < tileX; k++)
                {
                    for (int l = 0; l < tileY; l++)
                        Wiring.SkipWire(x + k, y + l);
                }
            }
        }

        public static Tile ParanoidTileRetrieval(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
                return new();

            return Main.tile[x, y];
        }
    }
}
