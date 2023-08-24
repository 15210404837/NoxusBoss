using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Fixes
{
    public class XerocProjectileDeletionFixSystem : ModSystem
    {
        public static bool HostileProjectilesCanBeDeleted => XerocBoss.Myself is null;

        public override void OnModLoad()
        {
            IL_Projectile.Update += PreventXerocProjectilesFromDespawning;
        }

        private void PreventXerocProjectilesFromDespawning(ILContext il)
        {
            ILCursor cursor = new(il);

            // Locate the instance where Main.leftWorld is loaded. It (along with a few other world variables) will be used to determine if the projectile has left the world
            // and must be deleted.
            cursor.GotoNext(i => i.MatchLdsfld<Main>("leftWorld"));

            // Find the instances of Projectile.active being set.
            cursor.GotoNext(MoveType.Before, i => i.MatchStfld<Entity>("active"));

            // Replace the value of the Projectile.active set so that it remains unchanged when Xeroc is present.
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Projectile, bool>>(p => !HostileProjectilesCanBeDeleted && p.hostile && p.active);

            // Branch after the return if hostile projectiles should not be deleted.
            ILLabel afterReturn = cursor.DefineLabel();
            cursor.GotoNext(MoveType.After, i => i.MatchRet());
            cursor.MarkLabel(afterReturn);

            cursor.GotoPrev(MoveType.After, i => i.MatchStfld<Entity>("active"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Projectile, bool>>(p => !HostileProjectilesCanBeDeleted && p.hostile);
            cursor.Emit(OpCodes.Brtrue, afterReturn);
        }
    }
}
