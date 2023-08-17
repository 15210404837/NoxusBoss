using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Content.Subworlds
{
    public static class EternalGardenWorldGen
    {
        public const int DirtDepth = 64;

        public const int LakeWidth = 132;

        public const int LakeMaxDepth = 35;

        public static int SurfaceTilePoint => Main.maxTilesY - DirtDepth - LakeMaxDepth;

        public static void Generate()
        {
            // Set the base level of dirt. Its top serves as the bottom of the lakes, with no natural way of going further down.
            GenerateDirtBase();

            // Generate both lakes.
            Rectangle leftLakeArea = new(0, SurfaceTilePoint + 1, LakeWidth + 1, LakeMaxDepth - 1);
            Rectangle rightLakeArea = new(Main.maxTilesX - LakeWidth - 1, SurfaceTilePoint + 1, LakeWidth, LakeMaxDepth - 1);
            GenerateLake(leftLakeArea, false);
            GenerateLake(rightLakeArea, true);

            // Set spawn points.
            SetInitialPlayerSpawnPoint();

            // Replace the dirt with grass where necessary.
            ReplaceDirtWithGrass();
        }

        public static void GenerateDirtBase()
        {
            // Self-explanatory. Just a simple rectangle of dirt that acts as the foundation for everything else.
            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int j = 0; j < DirtDepth; j++)
                {
                    int y = Main.maxTilesY - j;

                    Main.tile[x, y].TileType = TileID.Dirt;
                    Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                }
            }

            // Create a second mound of dirt in the center. This leaves the edges unchanged so that they can be occupied by the tranquil lakes.
            // In this first step the process is unnaturally rigid since there's no gradual descent into the lake or anything, but this will be addressed in
            // later generation steps.
            for (int x = LakeWidth + 1; x < Main.maxTilesX - LakeWidth; x++)
            {
                for (int j = 0; j < LakeMaxDepth; j++)
                {
                    int y = Main.maxTilesY - DirtDepth - j;

                    Main.tile[x, y].TileType = TileID.Dirt;
                    Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                }
            }
        }

        public static void GenerateLake(Rectangle area, bool right)
        {
            // Fill the currently empty rectangular box with water.
            for (int x = area.Left; x < area.Right; x++)
            {
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    Main.tile[x, y].Get<LiquidData>().Amount = byte.MaxValue;
                    Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Water;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
                }
            }
        }

        public static void SetInitialPlayerSpawnPoint()
        {
            // Set the default spawn position for the player, right next to the left lake.
            // This is where the player appears when they've teleported with the Terminus, not where they respawn when killed by Xeroc.
            // Once the player has properly entered the world and spawned already, this gets set again via the other method below.
            Main.spawnTileX = LakeWidth + 15;
            Main.spawnTileY = SurfaceTilePoint - 1;
        }

        public static void SetPlayerRespawnPoint()
        {

        }

        public static void ReplaceDirtWithGrass()
        {
            // Loop through the mound's tiles and replace the dirt with grass if it's exposed to air.
            for (int x = LakeWidth - 5; x < Main.maxTilesX - LakeWidth + 4; x++)
            {
                for (int j = 0; j < LakeMaxDepth; j++)
                {
                    int y = Main.maxTilesY - DirtDepth - j;

                    // The tile found is dirt. Now check if it has an exposed air pocket.
                    if (Main.tile[x, y].TileType == TileID.Dirt)
                    {
                        Tile left = CalamityUtils.ParanoidTileRetrieval(x - 1, y);
                        Tile right = CalamityUtils.ParanoidTileRetrieval(x + 1, y);
                        Tile top = CalamityUtils.ParanoidTileRetrieval(x, y - 1);
                        Tile bottom = CalamityUtils.ParanoidTileRetrieval(x, y + 1);
                        bool anyExposedAir = (!left.HasTile && left.LiquidAmount <= 0) || (!right.HasTile && right.LiquidAmount <= 0) || (!top.HasTile && top.LiquidAmount <= 0) || (!bottom.HasTile && bottom.LiquidAmount <= 0);
                        if (anyExposedAir)
                            WorldGen.SpreadGrass(x, y, TileID.Dirt, TileID.Grass, false);
                    }
                }
            }
        }
    }
}
