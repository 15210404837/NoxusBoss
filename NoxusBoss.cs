global using static System.MathF;
global using static Microsoft.Xna.Framework.MathHelper;
global using static NoxusBoss.Common.Utilities.Utilities;
global using static Terraria.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss
{
    public class NoxusBoss : Mod
    {
        public static Mod Instance
        {
            get;
            private set;
        }

        public static Mod Infernum
        {
            get;
            private set;
        }

        public static bool InfernumModeIsActive
        {
            get
            {
                if (Infernum is null)
                    return false;

                return (bool)Infernum.Call("GetInfernumActive");
            }
        }

        public override void Load()
        {
            Instance = this;
            if (ModLoader.TryGetMod("InfernumMode", out Mod inf))
                Infernum = inf;

            if (Main.netMode != NetmodeID.Server)
            {
                var calamityMod = ModLoader.GetMod("CalamityMod");
                Main.QueueMainThreadAction(() =>
                {
                    calamityMod.Call("LoadParticleInstances", this);
                });
            }
        }
    }
}
