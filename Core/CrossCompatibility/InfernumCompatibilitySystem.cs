using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class InfernumCompatibilitySystem : ModSystem
    {

        public static bool InfernumModeIsActive
        {
            get
            {
                if (Infernum is null)
                    return false;

                return (bool)Infernum.Call("GetInfernumActive");
            }
        }
    }
}
