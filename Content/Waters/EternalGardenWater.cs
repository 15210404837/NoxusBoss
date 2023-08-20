using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Waters
{
    public class EternalGardenWater : ModWaterStyle
    {
        public override int ChooseWaterfallStyle() => ModContent.Find<ModWaterfallStyle>("CalamityMod/SunkenSeaWaterflow").Slot;

        public override int GetSplashDust() => 33;

        public override int GetDropletGore() => 713;

        public override Color BiomeHairColor() => Color.ForestGreen;

        public override void LightColorMultiplier(ref float r, ref float g, ref float b)
        {
            r = 1.055f;
            g = 1.055f;
            b = 1.055f;
        }
    }
}
