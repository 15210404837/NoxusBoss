using System.Collections.Generic;
using CalamityMod.World;
using NoxusBoss.Content.Items;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace NoxusBoss.Content.WorldGenAlterations
{
    public class AbyssChestTweaker : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            tasks.Add(new PassLegacy("Add Boss Rush Item", AddItemToAbyssChest));
        }

        public static void AddItemToAbyssChest(GenerationProgress progress, GameConfiguration config)
        {
            int x = CalamityWorld.SChestX[(int)UndergroundShrines.UndergroundShrineType.Abyss];
            int y = CalamityWorld.SChestY[(int)UndergroundShrines.UndergroundShrineType.Abyss];
            int chestID = -1;
            for (int i = -8; i < 8; i++)
            {
                for (int j = -8; j < 8; j++)
                {
                    chestID = Chest.FindChestByGuessing(x - i, y - j);
                    if (chestID != -1)
                        goto LeaveLoop;
                }
            }

            LeaveLoop:

            if (chestID >= 0)
            {
                Chest terminusChest = Main.chest[chestID];
                terminusChest.AddItemToShop(new Item(ModContent.ItemType<BossRushStarter>()));
            }
        }
    }
}
