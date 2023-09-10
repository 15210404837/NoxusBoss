using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Items.Armor.Vanity.Masks;
using NoxusBoss.Content.Items.Pets;
using NoxusBoss.Content.Items.Placeable.Monoliths;
using NoxusBoss.Content.Items.Placeable.Relics;
using NoxusBoss.Content.Items.Placeable.Trophies;
using NoxusBoss.Content.Items.SummonItems;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC, IBossChecklistSupport, IToastyQoLChecklistBossSupport
    {
        #region Crossmod Compatibility

        public bool IsMiniboss => false;

        public string ChecklistEntryName => "EntropicGodNoxus";

        public bool IsDefeated => WorldSaveSystem.HasDefeatedNoxus;

        public FieldInfo IsDefeatedField => typeof(WorldSaveSystem).GetField("hasDefeatedNoxus", BindingFlags.Static | BindingFlags.NonPublic);

        public float ProgressionValue => 27f;

        public List<int> Collectibles => new()
        {
            ModContent.ItemType<MidnightMonolith>(),
            ModContent.ItemType<NoxusMask>(),
            ModContent.ItemType<NoxusTrophy>(),
            ModContent.ItemType<NoxusRelic>(),
            ModContent.ItemType<OblivionRattle>(),
        };

        public int? SpawnItem => ModContent.ItemType<Genesis>();

        public bool UsesCustomPortraitDrawing => true;

        public void DrawCustomPortrait(SpriteBatch spriteBatch, Rectangle area, Color color)
        {
            Texture2D texture = ModContent.Request<Texture2D>($"{Texture}_BossChecklist").Value;
            Vector2 centered = area.Center.ToVector2() - texture.Size() * 0.5f;
            spriteBatch.Draw(texture, centered, color);
        }

        #endregion Crossmod Compatibility

        #region Attack Cycles
        public static EntropicGodAttackType[] Phase1AttackCycle => new EntropicGodAttackType[]
        {
            EntropicGodAttackType.DarkExplosionCharges,
            EntropicGodAttackType.DarkEnergyBoltHandWave,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.HoveringHandGasBursts,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RapidExplosiveTeleports,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.DarkExplosionCharges,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RapidExplosiveTeleports,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.DarkEnergyBoltHandWave,
            EntropicGodAttackType.HoveringHandGasBursts,
            EntropicGodAttackType.MigraineAttack
        };

        public static EntropicGodAttackType[] Phase2AttackCycle => new EntropicGodAttackType[]
        {
            EntropicGodAttackType.GeometricSpikesTeleportAndFireballs,
            EntropicGodAttackType.PortalChainCharges,
            EntropicGodAttackType.ThreeDimensionalNightmareDeathRay,
            EntropicGodAttackType.OrganizedPortalCometBursts,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.RealityWarpSpinCharge,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.GeometricSpikesTeleportAndFireballs,
            EntropicGodAttackType.ThreeDimensionalNightmareDeathRay,
            EntropicGodAttackType.PortalChainCharges,
            EntropicGodAttackType.OrganizedPortalCometBursts,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RealityWarpSpinCharge,
            EntropicGodAttackType.FireballBarrage,
            EntropicGodAttackType.TeleportAndShootNoxusGas,
            EntropicGodAttackType.MigraineAttack,
        };

        public static EntropicGodAttackType[] Phase3AttackCycle => new EntropicGodAttackType[]
        {
            EntropicGodAttackType.PortalChainCharges,
            EntropicGodAttackType.PortalChainCharges2,
            EntropicGodAttackType.MigraineAttack,
            EntropicGodAttackType.RealityWarpSpinCharge,
            EntropicGodAttackType.BrainFogAndThreeDimensionalCharges,
            EntropicGodAttackType.ThreeDimensionalNightmareDeathRay,
            EntropicGodAttackType.MigraineAttack,
        };
        #endregion Attack Cycles

        #region Initialization
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 90;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                Scale = 0.3f,
                PortraitScale = 0.5f
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);

            // This debuff makes this boss look ugly.
            NPCID.Sets.DebuffImmunitySets[Type] = new()
            {
                SpecificallyImmuneTo = new int[] { ModContent.BuffType<MiracleBlight>() }
            };
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 50f;
            NPC.damage = 335;
            NPC.width = 122;
            NPC.height = 290;
            NPC.defense = 130;
            NPC.LifeMaxNERB(7544400, 8475000);

            // That is all. Goodbye.
            // No, I will not entertain Master Mode or the difficulty seeds.
            if (CalamityWorld.death)
                NPC.lifeMax = 12477600;

            if (Main.expertMode)
            {
                NPC.damage = 550;

                // Undo vanilla's automatic Expert boosts.
                NPC.lifeMax /= 2;
                NPC.damage /= 2;
            }

            double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;
            NPC.value = Item.buyPrice(50, 0, 0, 0) / 5;
            NPC.netAlways = true;
            NPC.hide = true;
            NPC.Calamity().ShouldCloseHPBar = true;
            InitializeHandsIfNecessary();

            Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/EntropicGod");
        }

        public void InitializeHandsIfNecessary()
        {
            if (Hands is not null && Hands[0] is not null)
                return;

            Hands[0] = new()
            {
                DefaultOffset = DefaultHandOffset * new Vector2(-1f, 1f),
                Center = NPC.Center + DefaultHandOffset * new Vector2(-1f, 1f),
                Velocity = Vector2.Zero
            };
            Hands[1] = new()
            {
                DefaultOffset = DefaultHandOffset,
                Center = NPC.Center + DefaultHandOffset,
                Velocity = Vector2.Zero
            };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement($"Mods.{Mod.Name}.Bestiary.{Name}"),
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement()
            });
        }
        #endregion Initialization
    }
}
