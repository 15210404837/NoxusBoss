using System.IO;
using System.Linq;
using CalamityMod.Items.Potions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        #region Multiplayer Syncs

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(PhaseCycleIndex);
            writer.Write(SwordSlashCounter);
            writer.Write(SwordSlashDirection);

            writer.Write(UniversalUpdateTimer);
            writer.Write(ZPosition);
            writer.WriteVector2(GeneralHoverOffset);
            writer.WriteVector2(CensorPosition);
            writer.WriteVector2(PunchDestination);
            writer.WriteVector2(SwordChargeDestination);

            // Write lists.
            writer.Write(Hands.Count);
            for (int i = 0; i < Hands.Count; i++)
                Hands[i].WriteTo(writer);

            writer.Write(StarSpawnOffsets.Count);
            for (int i = 0; i < StarSpawnOffsets.Count; i++)
                writer.WriteVector2(StarSpawnOffsets[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            PhaseCycleIndex = reader.ReadInt32();
            SwordSlashCounter = reader.ReadInt32();
            SwordSlashDirection = reader.ReadInt32();
            SwordChargeDestination = reader.ReadVector2();

            UniversalUpdateTimer = reader.ReadSingle();
            ZPosition = reader.ReadSingle();
            GeneralHoverOffset = reader.ReadVector2();
            CensorPosition = reader.ReadVector2();
            PunchDestination = reader.ReadVector2();

            // Read lists.
            Hands.Clear();
            StarSpawnOffsets.Clear();

            int handCount = reader.ReadInt32();
            for (int i = 0; i < handCount; i++)
                Hands.Add(XerocHand.ReadFrom(reader));

            int starOffsetCount = reader.ReadInt32();
            for (int i = 0; i < starOffsetCount; i++)
                StarSpawnOffsets.Add(reader.ReadVector2());
        }

        #endregion Multiplayer Syncs

        #region Hit Effects and Loot

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.soundDelay >= 1)
                return;

            NPC.soundDelay = 9;
            //SoundEngine.PlaySound(HitSound, NPC.Center);
        }

        public override bool CheckDead()
        {
            AttackTimer = 0f;

            // Disallow natural death. The time check here is as a way of catching cases where multiple hits happen on the same frame and trigger a death.
            // If it just checked the attack state, then hit one would trigger the state change, set the HP to one, and the second hit would then deplete the
            // single HP and prematurely kill Xeroc.
            if (CurrentAttack == XerocAttackType.DeathAnimation && AttackTimer >= 10f)
                return true;

            TriggerDeathAnimation();
            return false;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            scale *= TeleportVisualsAdjustedScale.Length() * 0.707f;
            return null;
        }

        public override void BossLoot(ref string name, ref int potionType) => potionType = ModContent.ItemType<OmegaHealingPotion>();

        // Ensure that Xeroc' contact damage adheres to the special boss-specific cooldown slot, to prevent things like lava cheese.
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses;

            // This is quite scuffed, but since there's no equivalent easy Colliding hook for NPCs, it is necessary to increase Xeroc's "effective hitbox" to an extreme
            // size via a detour and then use the CanHitPlayer hook to selectively choose whether the target should be inflicted damage or not (in this case, based on hands that can do damage).
            // This is because NPC collisions are fundamentally based on rectangle intersections. CanHitPlayer does not allow for the negation of that. But by increasing the hitbox by such an
            // extreme amount that that check is always passed, this issue is mitigated. Again, scuffed, but the onus is on TML to make this easier for modders to do.
            if (Hands.Where(h => h.CanDoDamage).Any())
                return Hands.Where(h => h.CanDoDamage).Any(h => CenteredRectangle(h.Center, TeleportVisualsAdjustedScale * 106f).Intersects(target.Hitbox));

            return false;
        }

        private void ExpandEffectiveHitboxForHands(On.Terraria.NPC.orig_GetMeleeCollisionData orig, Rectangle victimHitbox, int enemyIndex, ref int specialHitSetter, ref float damageMultiplier, ref Rectangle npcRect)
        {
            orig(victimHitbox, enemyIndex, ref specialHitSetter, ref damageMultiplier, ref npcRect);

            // See the big comment in CanHitPlayer.
            if (Main.npc[enemyIndex].type == Type)
                npcRect.Inflate(8000, 8000);
        }

        #endregion Hit Effects and Loot

        #region Gotta Manually Disable Despawning Lmao

        // Disable natural despawning for Xeroc.
        public override bool CheckActive() => false;

        #endregion Gotta Manually Disable Despawning Lmao
    }
}
