using CalamityMod;
using CalamityMod.World;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        #region Attack Cycles

        // The attack cycles for Xeroc are specifically designed to go in a repeated quick paced -> precise dance that gradually increases in speed across the different cycles.
        // Please do not change it without careful consideration.
        public static XerocAttackType[] Phase1Cycle => new[]
        {
            // Start off with the arcing attack. It will force the player to move around to evade the starbursts.
            XerocAttackType.ShootArcingStarburstsFromEye,

            // After the starburst attack, there will be some leftover momentum. Force them to quickly get rid of it and transition to weaving with the dagger walls attack.
            XerocAttackType.RealityTearDaggers,

            // After the daggers have passed, it's a safe bet the player won't have much movement at the start to mess with the attack. As such, the exploding star attack happens next to work with that.
            XerocAttackType.ConjureExplodingStars,

            // Now that the player has spent a bunch of time doing weaving and tight, precise movements, get them back into the fast moving action again with the arcing starbursts.
            XerocAttackType.ShootArcingStarburstsFromEye,

            // And again, account for the leftover momentum with a "slower" attack in the form of the attack where Xeroc uses the sun to conjure fireballs and solar flares.
            // The wind-up time will be sufficient to prevent cheap hits.
            XerocAttackType.StealSun,

            // And again, follow up with a precise attack in the form of the star lasers. This naturally follows with the chasing quasar, which amps up the pacing again.
            XerocAttackType.StarManagement,
            XerocAttackType.StarManagement_CrushIntoQuasar,

            // Return to the fast starbursts attack again.
            XerocAttackType.ShootArcingStarburstsFromEye,

            // Do the precise laserbeam charge attack to slow them down. From here the cycle will repeat at another high point.
            XerocAttackType.PortalLaserBarrages
        };

        public static XerocAttackType[] TestCycle => new[]
        {
            XerocAttackType.LightBeamTransformation,
            XerocAttackType.SwordConstellation2
        };

        #endregion Attack Cycles

        #region Initialization
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Xeroc");
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 90;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                Scale = 0.3f,
                PortraitScale = 0.5f
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            On.Terraria.NPC.GetMeleeCollisionData += ExpandEffectiveHitboxForHands;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 100f;
            NPC.damage = 540;
            NPC.width = 440;
            NPC.height = 700;
            NPC.defense = 150;
            NPC.LifeMaxNERB(8000000, 9364000);

            // That is all. Goodbye.
            // No, I will not entertain Master Mode or the difficulty seeds.
            if (CalamityWorld.death)
                NPC.lifeMax = 13765256;

            if (Main.expertMode)
                NPC.damage = 600;

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
            NPC.value = Item.buyPrice(100, 0, 0, 0) / 5;
            NPC.netAlways = true;
            NPC.hide = true;
            NPC.Opacity = 0f;
            NPC.Calamity().ShouldCloseHPBar = true;

            Wings = new XerocWing[WingCount];
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * bossLifeScale / (Main.masterMode ? 3f : 2f));
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("The cosmic creator."),
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement()
            });
        }
        #endregion Initialization
    }
}
