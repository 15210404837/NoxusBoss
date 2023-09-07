using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Fixes
{
    public class ScreenShaderFixerSystem : ModSystem
    {
        public override void OnModLoad()
        {
            Main.QueueMainThreadAction(() =>
            {
                IL_Main.DoDraw += SubjugateTheRetroPilled;
            });
        }

        private void SubjugateTheRetroPilled(ILContext il)
        {
            ILCursor cursor = new(il);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Lighting>("get_NotRetro")))
                return;

            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Or);

            if (!cursor.TryGotoPrev(MoveType.After, i => i.MatchLdsfld<Main>("gameMenu")))
                return;

            cursor.EmitDelegate(() => MainMenuScreenShakeShaderData.ScreenShakeIntensity <= 0.01f);
            cursor.Emit(OpCodes.And);
        }
    }
}
