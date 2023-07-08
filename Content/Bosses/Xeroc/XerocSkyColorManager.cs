using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class XerocSkyColorManager : ModSystem
    {
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            // Make the background color get darker based on Xeroc's star fear effect.
            backgroundColor = Color.Lerp(backgroundColor, new(2, 2, 3), Pow(XerocSky.StarRecedeInterpolant, 2.5f));
        }
    }
}
