using System;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace NoxusBoss.Assets.Fonts
{
    public class FontRegistry : ModSystem
    {
        // Historically Calamity received errors when attempting to load fonts on Linux systems for their MGRR boss HP bar.
        // Out of an abundance of caution, this mod implements the same solution as them and only uses the font on windows operating systems.
        public static bool CanLoadFonts => Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static FontRegistry Instance => ModContent.GetInstance<FontRegistry>();

        public DynamicSpriteFont CultistFont
        {
            get
            {
                if (CanLoadFonts)
                    return Mod.Assets.Request<DynamicSpriteFont>("Assets/Fonts/CultistText", AssetRequestMode.ImmediateLoad).Value;

                return FontAssets.MouseText.Value;
            }
        }

        public DynamicSpriteFont CultistFontItalics
        {
            get
            {
                if (CanLoadFonts)
                    return Mod.Assets.Request<DynamicSpriteFont>("Assets/Fonts/CultistTextItalics", AssetRequestMode.ImmediateLoad).Value;

                return FontAssets.MouseText.Value;
            }
        }
    }
}
