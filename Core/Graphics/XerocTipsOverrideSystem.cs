using System;
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

        public static string SprayDeletionTipsText => "Do not.";

        public static string DeathAnimationTipsText => "You have passed the test. You have passed the test. You have passed the test. You have passed the test. You have passed the test. You have passed the test. " +
            "You have passed the test. You have passed the test. You have passed the test. You have passed the test.";

        public override void OnModLoad()
        {
            Terraria.GameContent.UI.IL_GameTipsDisplay.Draw += ChangeTipText;
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
