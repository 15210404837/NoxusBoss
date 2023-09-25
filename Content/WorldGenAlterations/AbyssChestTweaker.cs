using System.Collections.Generic;
using System.Linq;
using CalamityMod.Items.SummonItems;
using NoxusBoss.Content.Items.SummonItems;
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
            // Check all chests to see if they contain Terminus. If they do, also add Terminal.
            int terminusID = ModContent.ItemType<Terminus>();
            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest c = Main.chest[i];
                if (c?.item.Any(s => s.stack >= 1 && s.type == terminusID) ?? false)
                    c.AddItemToShop(new Item(ModContent.ItemType<Terminal>()));
            }
        }
    }
}
