using CalamityMod;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace NoxusBoss.Content.Subworlds
{
    public static class EternalGardenWorldGen
    {
        public const int DirtDepth = 64;

        public const int LakeWidth = 236;

        public const int LakeMaxDepth = 35;

        // Avoid using this in loops. It's more efficient to store it in a local variable and reference that instead of calling this getter property over and over.
        public static int SurfaceTilePoint => Main.maxTilesY - DirtDepth - LakeMaxDepth;

        // How much of a magnification is performed when calculating perlin noise for height maps. The closer to 0 this value is, the more same-y they will seem in
        // terms of direction, size, etc.
        public const float SurfaceMapMagnification = 0.0025f;

        public const int MaxGroundHeight = 18;

        // This is how many tiles it takes for the flatness interpolant to go from its maximum to its minimum.
        public const int GroundFlatnessTaperZone = 45;

        public const int TotalFlatTilesAtCenter = 25;

        public const int TotalFlatTilesAtEdge = 12;

        public static void Generate()
        {
            // Set the base level of dirt. Its top serves as the bottom of the lakes, with no natural way of going further down.
            GenerateDirtBase();

            // Generate both lakes.
            Rectangle leftLakeArea = new(0, SurfaceTilePoint + 1, LakeWidth + 1, LakeMaxDepth);
            Rectangle rightLakeArea = new(Main.maxTilesX - LakeWidth - 1, SurfaceTilePoint + 1, LakeWidth, LakeMaxDepth);
            GenerateLake(leftLakeArea, false);
            GenerateLake(rightLakeArea, true);

            // Set spawn points.
            SetInitialPlayerSpawnPoint();

            // Calculate the topography of the ground and generate height accordingly.
            int[] topography = GenerateGroundTopography(leftLakeArea.Right, rightLakeArea.Left);

            // Replace the dirt with grass where necessary.
            ReplaceDirtWithGrass();

            // Smoothen everything.
            SmoothenWorld();

            // Generate plants atop the grass.
            GeneratePlants(topography, leftLakeArea.Right);
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

        public static int[] GenerateGroundTopography(int left, int right)
        {
            // Calculate the center point, width, and surface line.
            int width = right - left;
            int center = (left + right) / 2;
            int surfaceY = SurfaceTilePoint;

            // Use noise to determine the height topography of the ground.
            int heightMapSeed = WorldGen.genRand.Next(999999999);
            int[] topography = new int[right - left];
            for (int i = 0; i < topography.Length; i++)
            {
                // Use a flatness interpolant of 1 (meaning the result is unaffected) by default.
                float heightFlatnessInterpolant = 1f;

                // Make the height more flat at the center of the garden.
                int x = i + left;
                float distanceFromCenter = Distance(x, center);
                float distanceFromEdge = width * 0.5f - distanceFromCenter;
                if (distanceFromCenter <= TotalFlatTilesAtCenter + GroundFlatnessTaperZone)
                    heightFlatnessInterpolant = GetLerpValue(0f, GroundFlatnessTaperZone, distanceFromCenter - TotalFlatTilesAtCenter, true);

                // Make the height more flat at the edges of the garden, so that there's no awkward ledges at the lake.
                if (distanceFromEdge <= TotalFlatTilesAtEdge + GroundFlatnessTaperZone)
                    heightFlatnessInterpolant = GetLerpValue(0f, GroundFlatnessTaperZone, distanceFromEdge - TotalFlatTilesAtEdge, true);

                Vector2 heightMapInput = new Vector2(i, SurfaceTilePoint) * SurfaceMapMagnification;
                int height = (int)(Abs(SulphurousSea.FractalBrownianMotion(heightMapInput.X, heightMapInput.Y, heightMapSeed, 3)) * MaxGroundHeight * heightFlatnessInterpolant);
                topography[i] = height;
            }

            // Use the height map to generate the ground.
            for (int i = 0; i < topography.Length; i++)
            {
                int x = i + left;
                int height = topography[i];

                for (int dy = 0; dy < height; dy++)
                {
                    int y = surfaceY - dy;
                    Main.tile[x, y].TileType = TileID.Dirt;
                    Main.tile[x, y].Get<TileWallWireStateData>().Slope = SlopeType.Solid;
                    Main.tile[x, y].Get<TileWallWireStateData>().IsHalfBlock = false;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                }
            }

            return topography;
        }

        public static void SetInitialPlayerSpawnPoint()
        {
            // Set the default spawn position for the player, right next to the left lake.
            // This is where the player appears when they've teleported with the Terminus, not where they respawn when killed by Xeroc.
            // Once the player has properly entered the world and spawned already, this gets set again via the other method below.
            Main.spawnTileX = LakeWidth + 60;
            Main.spawnTileY = SurfaceTilePoint - 1;
        }

        public static void SetPlayerRespawnPoint()
        {

        }

        public static void ReplaceDirtWithGrass()
        {
            for (int x = LakeWidth - 5; x < Main.maxTilesX - LakeWidth + 4; x++)
            {
                for (int y = 10; y < Main.maxTilesY - 1; y++)
                {
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

        public static void SmoothenWorld()
        {
            for (int x = 5; x < Main.maxTilesX - 5; x++)
            {
                for (int y = 5; y < Main.maxTilesY - 5; y++)
                    Tile.SmoothSlope(x, y);
            }
        }

        public static void GeneratePlants(int[] topography, int left)
        {
            WeightedRandom<ushort> plantSelector = new(WorldGen.genRand.Next());
            plantSelector.Add(TileID.Plants, 1.32);
            plantSelector.Add(TileID.Plants2, 0.5);
            plantSelector.Add(TileID.DyePlants, 0.1);
            plantSelector.Add((ushort)ModContent.TileType<BrimstoneRose>(), 0.09);
            plantSelector.Add((ushort)ModContent.TileType<ElysianRose>(), 0.4);

            // Loop through the mound's tiles and replace the dirt with grass if it's exposed to air.
            int surfaceY = SurfaceTilePoint;
            ushort previousPlantID = 0;
            for (int i = 0; i < topography.Length; i++)
            {
                int height = topography[i];
                int x = i + left;
                int y = surfaceY - height;
                bool inCenter = Distance(x, Main.maxTilesX * 0.5f) <= TotalFlatTilesAtCenter + 24f;
                ushort plantID = plantSelector.Get();

                // Prevent placing special plants twice in succession.
                if (previousPlantID != TileID.Plants && previousPlantID != TileID.Plants2)
                {
                    // Re-roll until the plant ID is different. This only happens 50 times at most, in case something goes wrong and would otherwise cause an infinite loop freeze.
                    for (int j = 0; j < 50; j++)
                    {
                        if (plantID != previousPlantID)
                            break;

                        plantID = plantSelector.Get();
                    }
                }

                // In the center special plants are always replaced with First Flowers.
                if (inCenter && (plantID != TileID.Plants || plantID != TileID.Plants2))
                    plantID = WorldGen.genRand.NextBool(4) ? (ushort)ModContent.TileType<FirstFlower>() : TileID.Plants2;

                previousPlantID = plantID;

                // Certain tiles require manual selection of frames. Handle such cases.
                // The reason this is necessary is because the tile placement method will automatically determine the frame for some tiles on its own if the input is 0, but for others
                // it will just use the first frame variant universally. This inconsistent behavior is quite weird, but workable.
                int frameVariant = 0;
                switch (plantID)
                {
                    // This tile's variant selection is handled automatically, but I find it more interesting to have it hand-picked so that they're far more likely to be flowers instead of boring grass.
                    case TileID.Plants:
                        frameVariant = SelectRandom(WorldGen.genRand, new int[]
                        {
                            // Flower variants.
                            6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44,

                            // Grass. Duplicate entries exist here to bias the weight a little bit in the favor the grass, but given how many
                            // flower variants there are this shouldn't cause many problems.
                            0, 1, 1, 2, 3, 3, 4, 5, 5
                        });
                        break;

                    // Same idea as TileID.Plants. Grass is boring.
                    case TileID.Plants2:
                        frameVariant = SelectRandom(WorldGen.genRand, new int[]
                        {
                            // Flower variants.
                            6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44,

                            // Grass.
                            0, 1, 2, 3, 4, 5
                        });
                        break;

                    // Uses the stylist strange plant variants.
                    case TileID.DyePlants:
                        frameVariant = WorldGen.genRand.Next(8, 12);
                        break;
                }
                if (plantID == ModContent.TileType<ElysianRose>())
                    frameVariant = WorldGen.genRand.Next(2);
                if (plantID == ModContent.TileType<FirstFlower>())
                    frameVariant = WorldGen.genRand.Next(3);

                // Since vanilla's grass variants seemingly use hardcoded frame code that's unresponsive to manual inputs, replace them with a modded variant.
                if (plantID == TileID.Plants)
                    plantID = (ushort)ModContent.TileType<EternalFlower>();
                if (plantID == TileID.Plants2)
                    plantID = (ushort)ModContent.TileType<EternalTallFlower>();

                WorldGen.PlaceObject(x, y, plantID, true, frameVariant);
            }
        }
    }
}
