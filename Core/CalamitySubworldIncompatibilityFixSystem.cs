using CalamityMod.BiomeManagers;
using CalamityMod.Systems;
using NoxusBoss.Content.Subworlds;
using SubworldLibrary;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class CalamitySubworldIncompatibilityFixSystem : ModSystem
    {
        public delegate void orig_TileGrowthMethod();

        public delegate void hook_TileGrowthMethod(orig_TileGrowthMethod orig);

        public delegate bool orig_IsSulphSeaActiveMethod(SulphurousSeaBiome instance, Player player);

        public delegate bool hook_IsSulphSeaActiveMethod(orig_IsSulphSeaActiveMethod orig, SulphurousSeaBiome instance, Player player);

        public override void OnModLoad()
        {
            MethodInfo tileGrowMethod = typeof(WorldMiscUpdateSystem).GetMethod("HandleTileGrowth", BindingFlags.Public | BindingFlags.Static);
            MonoModHooks.Add(tileGrowMethod, (hook_TileGrowthMethod)DisableTileGrowthInGarden);

            MethodInfo sulphSeaActiveMethod = typeof(SulphurousSeaBiome).GetMethod("IsBiomeActive", BindingFlags.Public | BindingFlags.Instance);
            MonoModHooks.Add(sulphSeaActiveMethod, (hook_IsSulphSeaActiveMethod)DisableSulphSeaInSubworlds);
        }

        public static void DisableTileGrowthInGarden(orig_TileGrowthMethod orig)
        {
            // This method throws a shitstorm of index errors when in the garden due to differing world sizes.
            // There's never going to be any natural sunken sea or abyss tiles to grow things on, so it's easiest to just tell that method to fuck off while in there.
            if (!SubworldSystem.IsActive<EternalGarden>())
                orig();
        }

        public static bool DisableSulphSeaInSubworlds(orig_IsSulphSeaActiveMethod orig, SulphurousSeaBiome instance, Player player)
        {
            // Hardcoded proximity checks don't work in subworlds.
            if (SubworldSystem.IsActive<EternalGarden>())
                return false;

            return orig(instance, player);
        }
    }
}
