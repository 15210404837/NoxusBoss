using System;
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
            writer.Write(CurrentPhase);
            writer.Write(PhaseCycleIndex);
            writer.Write(SwordSlashCounter);
            writer.Write(SwordSlashDirection);
            writer.Write(SwordAnimationTimer);

            writer.Write(PunchOffsetAngle);
            writer.Write(FightLength);
            writer.Write(ZPosition);
            writer.WriteVector2(GeneralHoverOffset);
            writer.WriteVector2(CensorPosition);
            writer.WriteVector2(PunchDestination);
            writer.WriteVector2(SwordChargeDestination);
            writer.WriteVector2(HandFireDestination);

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
            CurrentPhase = reader.ReadInt32();
            PhaseCycleIndex = reader.ReadInt32();
            SwordSlashCounter = reader.ReadInt32();
            SwordSlashDirection = reader.ReadInt32();
            SwordAnimationTimer = reader.ReadInt32();

            FightLength = reader.ReadSingle();
            PunchOffsetAngle = reader.ReadSingle();
            ZPosition = reader.ReadSingle();
            GeneralHoverOffset = reader.ReadVector2();
            CensorPosition = reader.ReadVector2();
            PunchDestination = reader.ReadVector2();
            SwordChargeDestination = reader.ReadVector2();
            HandFireDestination = reader.ReadVector2();

            // Read lists.
            if (Hands.Any())
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < Hands.Count; i++)
                        Hands[i].HandTrailDrawer?.BaseEffect?.Dispose();
                }
                Hands.Clear();
            }
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
            NPC.life = NPC.lifeMax;
            return false;
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

        // Timed DR but a bit different. I'm typically very, very reluctant towards this mechanic, but given that this boss exists in shadowspec tier, I am willing to make
        // an exception. This will not cause the dumb "lol do 0 damage for 30 seconds" problems that Calamity had in the past.
        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            // Calculate how far ahead Xeroc's HP is relative to how long he's existed so far.
            // This would be one if you somehow got him to death on the first frame of the fight.
            // This naturally tapers off as the fight goes on.
            float fightLengthInterpolant = GetLerpValue(0f, IdealFightDuration, FightLength, true);
            float aheadOfFightLengthInterpolant = MathF.Max(0f, 1f - fightLengthInterpolant - LifeRatio);

            float damageReductionInterpolant = Pow(aheadOfFightLengthInterpolant, 0.64f);
            float damageReductionFactor = Lerp(1f, MaxTimedDRDamageReduction, damageReductionInterpolant);
            damage *= damageReductionFactor;
            return true;
        }

        #endregion Hit Effects and Loot

        #region Gotta Manually Disable Despawning Lmao

        // Disable natural despawning for Xeroc.
        public override bool CheckActive() => false;

        #endregion Gotta Manually Disable Despawning Lmao
    }
}
