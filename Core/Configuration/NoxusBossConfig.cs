using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace NoxusBoss.Core.Configuration
{
    [BackgroundColor(96, 30, 53, 216)]
    public class NoxusBossConfig : ModConfig
    {
        public static NoxusBossConfig Instance => ModContent.GetInstance<NoxusBossConfig>();

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        public bool ScreenShatterEffects { get; set; }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(0.5f)]
        [Range(0f, 1f)]
        public float VisualOverlayIntensity { get; set; }

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => false;
    }
}
