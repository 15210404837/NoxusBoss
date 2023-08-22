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
        // If this is enabled, various development-specific tools are operational, such as the automatic shader compiler and the keyboard shader debug drawer.
        // If it's disabled, they do not run at all, and where possible don't even load in the first place.
        // This means that this isn't something that other mods can just turn by flipping this property to true, since it will be too late for
        // automatic loading to happen.
        // The reason for this is mainly performance. No use having a bunch of resources in the background on the 1/100 chance a single person actually cares.
        public static bool DebugFeaturesEnabled
        {
            get;
            private set;
        }

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
