using CalamityMod;
using CalamityMod.NPCs.PrimordialWyrm;
using NoxusBoss.Content.Items;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class NoxusGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // The Primordial Wyrm drops the Terminus in Infernum instead of being in an abyss chest.
            // For compatibility reasons, the Wyrm drops the boss rush starter item as well with this mod.
            // This item is only shown in the bestiary if Infernum is active, because in all other contexts it's unobtainable.
            bool showInBestiary = ModReferences.Infernum is not null;
            if (npc.type == ModContent.NPCType<PrimordialWyrmHead>())
                npcLoot.AddIf(() => InfernumCompatibilitySystem.InfernumModeIsActive, ModContent.ItemType<BossRushStarter>(), 1, 1, 1, showInBestiary);
        }
    }
}
