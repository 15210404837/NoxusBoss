using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class XerocTipsOverrideSystem : ModSystem
    {
        public static bool UseDeathAnimationText
        {
            get;
            set;
        }

        public static bool UseSprayText
        {
            get;
            set;
        }

        public static readonly Regex PercentageExtractor = new(@"([0-9]+%)", RegexOptions.Compiled);

        public static string SprayDeletionTipsText => "Do not.";

        public static string DeathAnimationTipsText => "You have passed the test. You have passed the test. You have passed the test. You have passed the test. You have passed the test. You have passed the test. " +
            "You have passed the test. You have passed the test. You have passed the test. You have passed the test.";

        public override void OnModLoad()
        {
            Terraria.GameContent.UI.IL_GameTipsDisplay.Draw += ChangeTipText;
            On_Main.DrawMenu += ChangeStatusText;
        }

        private void ChangeStatusText(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            if (UseSprayText)
            {
                Main.statusText = string.Empty;
                orig(self, gameTime);
                return;
            }

            if (UseDeathAnimationText)
            {
                string oldStatusText = Main.statusText;

                // Incorporate the percentage into the replacement text, if one was present previously.
                if (PercentageExtractor.IsMatch(oldStatusText))
                {
                    string percentage = PercentageExtractor.Match(oldStatusText).Value;
                    Main.statusText = $"You have passed the test: {percentage}";
                }

                // Otherwise simply use the regular ominous text about having "passed the test".
                else
                    Main.statusText = "You have passed the test.";

                Main.oldStatusText = Main.statusText;
            }
            orig(self, gameTime);
        }

        public override void PostUpdateEverything()
        {
            if (!Main.gameMenu)
            {
                UseDeathAnimationText = false;
                UseSprayText = false;
            }
        }

        private void ChangeTipText(ILContext il)
        {
            ILCursor cursor = new(il);

            if (!cursor.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(Language), "get_ActiveCulture")))
                return;

            int textLocalIndex = 0;
            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStloc(out textLocalIndex)))
                return;

            cursor.EmitDelegate<Func<string, string>>(originalText =>
            {
                if (UseDeathAnimationText)
                    return DeathAnimationTipsText;
                else if (UseSprayText)
                    return SprayDeletionTipsText;
                return originalText;
            });

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Color>("get_White")))
                return;

            cursor.EmitDelegate<Func<Color, Color>>(originalColor =>
            {
                if (UseDeathAnimationText || UseSprayText)
                    return Color.IndianRed;
                return originalColor;
            });
        }
    }
}
