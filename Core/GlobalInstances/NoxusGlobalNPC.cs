﻿using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.PrimordialWyrm;
using MonoMod.Cil;
using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Content.Items.SummonItems;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalNPC : GlobalNPC
    {
        private static bool wasOriginallyDaytime;

        public override void Load()
        {
            On_NPC.SpawnNPC += MakeNightSpawnsPermanentForNoxusWorld;
            IL_Main.DoUpdateInWorld += MakeNPCsUseNightAIInNoxusWorld;
        }

        private void MakeNPCsUseNightAIInNoxusWorld(ILContext il)
        {
            ILCursor cursor = new(il);

            // Go a bit before NPC updating code.
            if (!cursor.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(FixExploitManEaters).GetMethod("Update"))))
                return;

            cursor.EmitDelegate(() =>
            {
                // Temporarily change the time to night. This will be reset back to what it was originally after all NPCs update.
                // The fews exceptions to this are the Profaned Guardians and Providence fight, since those two specifically enrage/despawn during the day, and the eclipse, since the enemies that event spawns
                // go away if it's night time.
                wasOriginallyDaytime = Main.dayTime;
                if (NoxusWorldManager.Enabled && CalamityGlobalNPC.doughnutBoss == -1 && CalamityGlobalNPC.holyBoss == -1 && !Main.eclipse)
                    Main.dayTime = false;
            });

            // Go a bit after NPC updating code.
            if (!cursor.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(LockOnHelper).GetMethod("SetUP"))))
                return;

            cursor.EmitDelegate(() =>
            {
                // Reset the time to its original state.
                Main.dayTime = wasOriginallyDaytime;
            });
        }

        private void MakeNightSpawnsPermanentForNoxusWorld(On_NPC.orig_SpawnNPC orig)
        {
            bool wasDaytime = Main.dayTime;
            if (NoxusWorldManager.Enabled && !Main.eclipse)
                Main.dayTime = false;

            try
            {
                orig();
            }
            finally
            {
                Main.dayTime = wasDaytime;
            }
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // The Primordial Wyrm drops the Terminus in Infernum instead of being in an abyss chest.
            // For compatibility reasons, the Wyrm drops the boss rush starter item as well with this mod.
            // This item is only shown in the bestiary if Infernum is active, because in all other contexts it's unobtainable.
            bool showInBestiary = ModReferences.Infernum is not null;
            if (npc.type == ModContent.NPCType<PrimordialWyrmHead>())
                npcLoot.AddIf(() => InfernumCompatibilitySystem.InfernumModeIsActive, ModContent.ItemType<BossRushStarter>(), 1, 1, 1, showInBestiary);
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (NoxusWorldManager.Enabled)
            {
                if (spawnRate >= 3 && spawnRate <= 1000000)
                    spawnRate = (int)(spawnRate * 0.6f);
                if (maxSpawns >= 1)
                    maxSpawns += 3;
            }
        }
    }
}