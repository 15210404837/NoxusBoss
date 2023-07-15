using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class XerocDeathVisualsSystem : ModSystem
    {
        public static int DeathTimerOverride
        {
            get;
            set;
        }

        public static int DeathTimerMax => 360;

        public static bool XerocWasAliveAtTimeOfDeath
        {
            get => Main.LocalPlayer.GetModPlayer<NoxusPlayer>().GetValue<bool>("NoxusAliveAtDeath");
            set => Main.LocalPlayer.GetModPlayer<NoxusPlayer>().SetValue("NoxusAliveAtDeath", value);
        }

        public override void OnModLoad()
        {
            IL.Terraria.Main.DrawInterface_35_YouDied += ChangeXerocText;
        }

        private void ChangeXerocText(ILContext il)
        {
            ILCursor cursor = new(il);

            // Replace the "You were slain..." text with something special.
            cursor.GotoNext(i => i.MatchLdsfld<Lang>("inter"));
            cursor.GotoNext(MoveType.Before, i => i.MatchStloc(out _));
            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                if (XerocWasAliveAtTimeOfDeath)
                    return "You have failed the test.";

                return originalText;
            });

            // Replace the number text.
            cursor.GotoNext(i => i.MatchLdstr("Game.RespawnInSuffix"));
            cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(typeof(Language), "GetTextValue"));
            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                if (XerocWasAliveAtTimeOfDeath)
                {
                    float deathTimerInterpolant = DeathTimerOverride / (float)DeathTimerMax;
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

        public override void PostUpdateEverything()
        {
            if (!Main.LocalPlayer.dead)
            {
                XerocWasAliveAtTimeOfDeath = XerocBoss.Myself is not null;
                DeathTimerOverride = 0;
            }
            else
            {
                DeathTimerOverride = Clamp(DeathTimerOverride + 1, 0, DeathTimerMax);
                if (DeathTimerOverride < DeathTimerMax)
                    Main.LocalPlayer.respawnTimer = 8;
            }
        }
    }
}
