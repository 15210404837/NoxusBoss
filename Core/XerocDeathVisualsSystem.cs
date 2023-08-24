using System;
using CalamityMod;
using MonoMod.Cil;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class XerocDeathVisualsSystem : ModSystem
    {
        public int DeathTimerOverride
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            IL_Main.DrawInterface_35_YouDied += ChangeXerocText;
        }

        private void ChangeXerocText(ILContext il)
        {
            ILCursor cursor = new(il);

            // Make the text offset higher up if Xeroc killed the player so that the player can better see the death vfx.
            cursor.GotoNext(MoveType.Before, i => i.MatchStloc(out _));
            cursor.EmitDelegate<Func<float, float>>(textOffset =>
            {
                if (Main.LocalPlayer.GetModPlayer<XerocPlayerDeathVisualsPlayer>().WasKilledByXeroc)
                    textOffset -= 120f;

                return textOffset;
            });

            // Replace the "You were slain..." text with something special.
            cursor.GotoNext(i => i.MatchLdsfld<Lang>("inter"));
            cursor.GotoNext(MoveType.Before, i => i.MatchStloc(out _));
            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                if (Main.LocalPlayer.GetModPlayer<XerocPlayerDeathVisualsPlayer>().WasKilledByXeroc)
                    return Language.GetTextValue("Mods.NoxusBoss.Dialog.XerocPlayerDeathText");

                return originalText;
            });

            // Replace the number text.
            cursor.GotoNext(i => i.MatchLdstr("Game.RespawnInSuffix"));
            cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(typeof(Language), "GetTextValue"));
            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                var modPlayer = Main.LocalPlayer.GetModPlayer<XerocPlayerDeathVisualsPlayer>();
                if (modPlayer.WasKilledByXeroc)
                {
                    float deathTimerInterpolant = modPlayer.DeathTimerOverride / (float)XerocPlayerDeathVisualsPlayer.DeathTimerMax;
                    ulong start = 5;
                    ulong end = int.MaxValue * 2uL;
                    float smoothInterpolant = CalamityUtils.PolyInOutEasing(deathTimerInterpolant, 20);
                    long textValue = (long)Lerp(start, end, smoothInterpolant);
                    if (textValue >= int.MaxValue)
                        textValue -= int.MaxValue * 2L + 2;

                    return textValue.ToString();
                }

                return originalText;
            });
        }
    }
}
