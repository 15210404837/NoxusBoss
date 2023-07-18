using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class XerocScreenShaderFixerSystem : ModSystem
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

            cursor.EmitDelegate(() => XerocBoss.Myself is not null);
            cursor.Emit(OpCodes.Or);
        }
    }
}
