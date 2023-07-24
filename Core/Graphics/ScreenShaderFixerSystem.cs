﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class ScreenShaderFixerSystem : ModSystem
    {
        public override void OnModLoad()
        {
            Main.QueueMainThreadAction(() =>
            {
                IL.Terraria.Main.DoDraw += SubjugateTheRetroPilled;
            });
        }

        private void SubjugateTheRetroPilled(ILContext il)
        {
            ILCursor cursor = new(il);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Lighting>("get_NotRetro")))
                return;

            cursor.EmitDelegate(() => !Main.gameMenu);
            cursor.Emit(OpCodes.Or);
        }
    }
}