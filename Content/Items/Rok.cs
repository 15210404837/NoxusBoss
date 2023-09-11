using System.Reflection;
using CalamityMod.Items;
using CalamityMod.Projectiles.Typeless;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class Rok : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostDraedonAndCal;

        public override void Load()
        {
            MethodInfo bossRushEnderKillMethod = typeof(BossRushEndEffectThing).GetMethod("Kill");
            MonoModHooks.Modify(bossRushEnderKillMethod, ReplaceRockInBossRush);
        }

        private static void ReplaceRockInBossRush(ILContext context)
        {
            // Define methods. These will be used for findings and replacements.
            MethodInfo itemTypeMethod = typeof(ModContent).GetMethod("ItemType", BindingFlags.Public | BindingFlags.Static);
            MethodInfo itemTypeMethod_Rock = itemTypeMethod.MakeGenericMethod(typeof(Rock));

            // Define the method cursor.
            ILCursor cursor = new(context);

            // Replace the real rock with the fake rock.
            cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(itemTypeMethod_Rock));
            cursor.Emit(OpCodes.Pop);
            cursor.EmitDelegate(ModContent.ItemType<Rok>);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = 0;
        }
    }
}
