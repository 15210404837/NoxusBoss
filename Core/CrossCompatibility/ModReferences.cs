﻿using Terraria.ModLoader;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class ModReferences : ModSystem
    {
        public static Mod BossChecklist
        {
            get;
            private set;
        }

        public static Mod Infernum
        {
            get;
            private set;
        }

        public override void Load()
        {
            // Check for relevant mods.
            if (ModLoader.TryGetMod("BossChecklist", out Mod bcl))
                BossChecklist = bcl;
            if (ModLoader.TryGetMod("InfernumMode", out Mod inf))
                Infernum = inf;
        }
    }
}