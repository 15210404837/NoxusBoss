using System;
using System.IO;
using CalamityMod;
using CalamityMod.Items.Potions;
using NoxusBoss.Content.Items.Accessories.VanityEffects;
using NoxusBoss.Content.Items.Armor.Vanity.Masks;
using NoxusBoss.Content.Items.Dyes;
using NoxusBoss.Content.Items.MiscOPTools;
using NoxusBoss.Content.Items.Pets;
using NoxusBoss.Content.Items.Placeable.Monoliths;
using NoxusBoss.Content.Items.Placeable.Relics;
using NoxusBoss.Content.Items.Placeable.Trophies;
using NoxusBoss.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC
    {
        #region Multiplayer Syncs
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(FightLength);
            writer.Write(PhaseCycleIndex);
            writer.Write(PortalChainDashCounter);
            writer.Write(CurrentPhase);
            writer.Write(BrainFogChargeCounter);
            writer.Write(NPC.Opacity);
            writer.Write(LaserSpinDirection);
            writer.WriteVector2(TeleportPosition);
            writer.WriteVector2(TeleportDirection);
            writer.WriteVector2(PortalArcSpawnCenter);
            writer.Write(LaserRotation.X);
            writer.Write(LaserRotation.Y);
            writer.Write(LaserRotation.Z);

            InitializeHandsIfNecessary();
            Hands[0].WriteTo(writer);
            Hands[1].WriteTo(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            FightLength = reader.ReadInt32();
            PhaseCycleIndex = reader.ReadInt32();
            PortalChainDashCounter = reader.ReadInt32();
            CurrentPhase = reader.ReadInt32();
            BrainFogChargeCounter = reader.ReadInt32();
            NPC.Opacity = reader.ReadSingle();
            LaserSpinDirection = reader.ReadSingle();
            TeleportPosition = reader.ReadVector2();
            TeleportDirection = reader.ReadVector2();
            PortalArcSpawnCenter = reader.ReadVector2();
            LaserRotation = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            InitializeHandsIfNecessary();
            Hands[0].ReadFrom(reader);
            Hands[1].ReadFrom(reader);
        }
        #endregion Multiplayer Syncs

        #region Hit Effects and Loot

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay >= 1 || CurrentAttack == EntropicGodAttackType.DeathAnimation)
                return;

            NPC.soundDelay = 9;
            SoundEngine.PlaySound(HitSound, NPC.Center);
        }

        public override bool CheckDead()
        {
            AttackTimer = 0f;

            // Disallow natural death. The time check here is as a way of catching cases where multiple hits happen on the same frame and trigger a death.
            // If it just checked the attack state, then hit one would trigger the state change, set the HP to one, and the second hit would then deplete the
            // single HP and prematurely kill Noxus.
            if (CurrentAttack == EntropicGodAttackType.DeathAnimation && AttackTimer >= 10f)
                return true;

            SelectNextAttack();
            ClearAllProjectiles();
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            CurrentAttack = EntropicGodAttackType.DeathAnimation;
            NPC.netUpdate = true;
            return false;
        }

        public override void OnKill()
        {
            if (!WorldSaveSystem.HasDefeatedNoxus)
            {
                WorldSaveSystem.HasDefeatedNoxus = true;
                CalamityNetcode.SyncWorld();
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // General drops.
            npcLoot.Add(ModContent.ItemType<NoxiousEvocator>());
            npcLoot.Add(ModContent.ItemType<NoxusSprayer>());

            // Vanity and decorations.
            npcLoot.Add(ModContent.ItemType<MidnightMonolith>());
            npcLoot.Add(ModContent.ItemType<EntropicDye>(), 1, 3, 5);
            npcLoot.Add(ModContent.ItemType<NoxusMask>(), 7);
            npcLoot.Add(ModContent.ItemType<NoxusTrophy>(), 10);
            npcLoot.DefineConditionalDropSet(DropHelper.RevAndMaster).Add(ModContent.ItemType<NoxusRelic>());
            npcLoot.DefineConditionalDropSet(DropHelper.RevAndMaster).Add(ModContent.ItemType<OblivionRattle>());
        }

        public override void BossLoot(ref string name, ref int potionType) => potionType = ModContent.ItemType<OmegaHealingPotion>();

        // Ensure that Noxus' contact damage adhere to the special boss-specific cooldown slot, to prevent things like lava cheese.
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), DebuffDuration_RegularAttack);
        }

        // Timed DR but a bit different. I'm typically very, very reluctant towards this mechanic, but given that this boss exists in shadowspec tier, I am willing to make
        // an exception. This will not cause the dumb "lol do 0 damage for 30 seconds" problems that Calamity had in the past.
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            // Calculate how far ahead Noxus' HP is relative to how long he's existed so far.
            // This would be one if you somehow got him to death on the first frame of the fight.
            // This naturally tapers off as the fight goes on.
            float fightLengthInterpolant = GetLerpValue(0f, IdealFightDuration, FightLength, true);
            float aheadOfFightLengthInterpolant = MathF.Max(0f, 1f - fightLengthInterpolant - LifeRatio);

            float damageReductionInterpolant = Pow(aheadOfFightLengthInterpolant, 0.64f);
            float damageReductionFactor = Lerp(1f, MaxTimedDRDamageReduction, damageReductionInterpolant);
            modifiers.FinalDamage *= damageReductionFactor;
        }
        #endregion Hit Effects and Loot

        #region Gotta Manually Disable Despawning Lmao
        // Disable natural despawning for Noxus.
        public override bool CheckActive() => false;

        #endregion Gotta Manually Disable Despawning Lmao
    }
}
