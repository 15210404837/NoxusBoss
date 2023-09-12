using Terraria.ModLoader;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class ModReferences : ModSystem
    {
        public static Mod BossChecklist
        {
            get;
            private set;
        }

        // Goozma mod.
        public static Mod CalamityHunt
        {
            get;
            private set;
        }

        public static Mod Infernum
        {
            get;
            private set;
        }

        public static Mod NycrosNohitMod
        {
            get;
            private set;
        }

        public static Mod ToastyQoL
        {
            get;
            private set;
        }

        public static Mod Wikithis
        {
            get;
            private set;
        }

        public override void Load()
        {
            // Check for relevant mods.
            if (ModLoader.TryGetMod("BossChecklist", out Mod bcl))
                BossChecklist = bcl;
            if (ModLoader.TryGetMod("CalamityHunt", out Mod calHunt))
                CalamityHunt = calHunt;
            if (ModLoader.TryGetMod("InfernumMode", out Mod inf))
                Infernum = inf;
            if (ModLoader.TryGetMod("EfficientNohits", out Mod nycros))
                NycrosNohitMod = nycros;
            if (ModLoader.TryGetMod("ToastyQoL", out Mod tQoL))
                ToastyQoL = tQoL;
            if (ModLoader.TryGetMod("Wikithis", out Mod wikithis))
                Wikithis = wikithis;
        }
    }
}
