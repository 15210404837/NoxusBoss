using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace NoxusBoss.Core
{
    [Label("Config")]
    [BackgroundColor(96, 30, 53, 216)]
    public class NoxusBossConfig : ModConfig
    {
        public static NoxusBossConfig Instance => ModContent.GetInstance<NoxusBossConfig>();

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Label("Screen Shatter Effects")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        [Tooltip("Enables screen shatter effects. Disable if they're too straining on the eyes.")]
        public bool ScreenShatterEffects { get; set; }

        [Label("Visual Overlay Intensity")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(0.5f)]
        [Range(0f, 1f)]
        [Tooltip("Changes the intensity of visual overlays such as blur and chromatic aberration.")]
        public float VisualOverlayIntensity { get; set; }

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => false;
    }
}
