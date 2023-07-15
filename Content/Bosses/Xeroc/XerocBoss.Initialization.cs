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

        // These attack cycles for Xeroc are specifically designed to go in a repeated quick paced -> precise dance that gradually increases in speed across the different cycles.
        // Please do not change them without careful consideration.
        public static XerocAttackType[] Phase1Cycle => new[]
        {
            // Start off with the arcing attack. It will force the player to move around to evade the starbursts.
            XerocAttackType.ShootArcingStarburstsFromEye,

            // After the starburst attack, there will be some leftover momentum. Force them to quickly get rid of it and transition to weaving with the dagger walls attack.
            XerocAttackType.RealityTearDaggers,

            // After the daggers have passed, it's a safe bet the player won't have much movement at the start to mess with the attack. As such, the exploding star attack happens next to work with that.
            XerocAttackType.ConjureExplodingStars,

            // Resume the slower pace with a "slower" attack in the form of a laserbeam attack.
            XerocAttackType.PortalLaserBarrages,

            // Now that the player has spent a bunch of time doing weaving and tight, precise movements, get them back into the fast moving action again with the arcing starbursts.
            XerocAttackType.ShootArcingStarburstsFromEye,

            // And again, follow up with a precise attack in the form of the star lasers. This naturally follows with the chasing quasar, which amps up the pacing again.
            XerocAttackType.StarManagement,
            XerocAttackType.StarManagement_CrushIntoQuasar,

            // Return to the fast starbursts attack again.
            XerocAttackType.ShootArcingStarburstsFromEye,

            // Do the precise laserbeam charge attack to slow them down. From here the cycle will repeat at another high point.
            XerocAttackType.PortalLaserBarrages
        };

        public static XerocAttackType[] Phase2Cycle => new[]
        {
            // Start out with a fast attack in the form of the screen slices.
            XerocAttackType.VergilScreenSlices,

            // Continue the fast pace with the punches + screen slices attack.
            XerocAttackType.PunchesWithScreenSlices,

            // Slow down the pace with circular portals. Following this the player will be moving in a precise, slow way.
            XerocAttackType.CircularPortalLaserBarrages,

            // Amp the pace up again with stars from the background. This will demand fast movement and zoning of the player.
            XerocAttackType.BrightStarJumpscares,

            // Get the player up close and personal with Xeroc with the true-melee sword attack.
            XerocAttackType.SwordConstellation2,

            // Return to something a bit slower again with the converging stars. This has a fast end point, however, which should naturally transition to the other attacks.
            XerocAttackType.StarConvergenceAndRedirecting,

            // Make the player use their speed from the end of the previous attack with the punches.
            XerocAttackType.PunchesWithScreenSlices,
            
            // Use the zoning background stars attack again the continue applying fast pressure onto the player.
            XerocAttackType.BrightStarJumpscares,

            // Follow with a precise attack in the form of the star lasers. This naturally follows with the chasing quasar, which amps up the pacing again.
            // This is a phase 1 attack, but is faster in the second phase.
            XerocAttackType.StarManagement,
            XerocAttackType.StarManagement_CrushIntoQuasar,

            // Return to the fast paced cycle with the true melee sword constellation attack again.
            XerocAttackType.SwordConstellation2,

            // Use the star convergence again, as the cycle repeats.
            XerocAttackType.StarConvergenceAndRedirecting,
        };

        // With the exception of the clock attack this cycle should keep the player constantly on the move.
        public static XerocAttackType[] Phase3Cycle => new[]
        {
            // Start with the true melle sword attack. This one uses two swords and is a bit faster than the original from the second phase.
            XerocAttackType.SwordConstellation2,

            // The aforementioned slow clock attack.
            XerocAttackType.TimeManipulation,
            
            // A chaotic laser chase sequence to keep the player constantly on their feet.
            XerocAttackType.LightBeamTransformation,

            // Slice the screen.
            XerocAttackType.VergilScreenSlices,
            
            // Perform the cosmic laserbeam attack.
            XerocAttackType.LightMagicCircle,
        };

        public static XerocAttackType[] TestCycle => new[]
        {
            XerocAttackType.RealityTearDaggers,
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
            NPC.damage = 375;
            NPC.width = 400;
            NPC.height = 500;
            NPC.defense = 150;
            NPC.LifeMaxNERB(8000000, 9364000);

            // That is all. Goodbye.
            // No, I will not entertain Master Mode or the difficulty seeds.
            if (CalamityWorld.death)
                NPC.lifeMax = 13767256;

            if (Main.expertMode)
                NPC.damage = 415;

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
