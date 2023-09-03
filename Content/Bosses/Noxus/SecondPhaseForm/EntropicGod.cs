using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.CrossCompatibility;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using static NoxusBoss.Core.Graphics.Shaders.Keyboard.NoxusKeyboardShader;

namespace NoxusBoss.Content.Bosses.Noxus.SecondPhaseForm
{
    // My pride and joy of Terraria.

    // I at one point sought to replicate the magic of others. To create something "special" that stands above almost all others in enjoyment and graphical fidelity.
    // I at one point sought to create a Seth of my own. A MEAC Empress. Something so incredible that people would pay attention to it long after it has finished.
    // Something that would elevate me to the point of being a "somebody". A "master".
    // I no longer hold that desire. I have no need to prove myself to this community any longer. I need only to prove myself to myself.
    //
    // I have done exactly that here.
    //
    // And amusingly, with that paradigm shift, the object of the abandoned desire will be realized.
    [AutoloadBossHead]
    public partial class EntropicGod : ModNPC, IBossChecklistSupport, IToastyQoLChecklistBossSupport
    {
        #region Custom Types and Enumerations
        public enum EntropicGodAttackType
        {
            // Phase 1 attacks.
            DarkExplosionCharges,
            DarkEnergyBoltHandWave,
            FireballBarrage,
            HoveringHandGasBursts,
            RapidExplosiveTeleports,
            TeleportAndShootNoxusGas,

            // Phase 2 attacks.
            Phase2Transition,
            GeometricSpikesTeleportAndFireballs,
            ThreeDimensionalNightmareDeathRay,
            PortalChainCharges,
            RealityWarpSpinCharge,
            OrganizedPortalCometBursts,

            // Phase 3 attacks.
            Phase3Transition,
            BrainFogAndThreeDimensionalCharges,
            PortalChainCharges2,

            // Lol lmao get FUCKED Noxus!!!!!!!!! (Cooldown ""attack"")
            MigraineAttack,

            // Self-explanatory.
            DeathAnimation
        }

        public class EntropicGodHand
        {
            public int Frame;

            public int FrameTimer;

            public bool ShouldOpen;

            public float? RotationOverride;

            public Vector2 Center;

            public Vector2 Velocity;

            public Vector2 DefaultOffset;

            public void WriteTo(BinaryWriter writer)
            {
                writer.WriteVector2(Center);
                writer.WriteVector2(Velocity);
                writer.WriteVector2(DefaultOffset);
            }

            public void ReadFrom(BinaryReader reader)
            {
                Center = reader.ReadVector2();
                Velocity = reader.ReadVector2();
                DefaultOffset = reader.ReadVector2();
            }
        }
        #endregion Custom Types and Enumerations

        #region Fields and Properties
        private static NPC myself;

        public EntropicGodHand[] Hands = new EntropicGodHand[2];

        public int CurrentPhase
        {
            get;
            set;
        }

        public int PhaseCycleIndex
        {
            get;
            set;
        }

        public int PortalChainDashCounter
        {
            get;
            set;
        }

        public int BrainFogChargeCounter
        {
            get;
            set;
        }

        public int FightLength
        {
            get;
            set;
        }

        public float LaserSpinDirection
        {
            get;
            set;
        }

        public float LaserTelegraphOpacity
        {
            get;
            set;
        }

        public float LaserSquishFactor
        {
            get;
            set;
        }

        public float LaserLengthFactor
        {
            get;
            set;
        }

        public float FogIntensity
        {
            get;
            set;
        }

        public float FogSpreadDistance
        {
            get;
            set;
        }

        public float HeadSquishiness
        {
            get;
            set;
        }

        public float EyeGleamInterpolant
        {
            get;
            set;
        }

        public float BigEyeOpacity
        {
            get;
            set;
        }

        public Vector2 TeleportPosition
        {
            get;
            set;
        }

        public Vector2 TeleportDirection
        {
            get;
            set;
        }

        public Vector2 PortalArcSpawnCenter
        {
            get;
            set;
        }

        public Vector3 LaserRotation
        {
            get;
            set;
        }

        public float LifeRatio => NPC.life / (float)NPC.lifeMax;

        public Player Target => Main.player[NPC.target];

        public Color GeneralColor => Color.Lerp(Color.White, Color.Black, Clamp(Abs(ZPosition) * 0.35f, 0f, 1f));

        public EntropicGodAttackType CurrentAttack
        {
            get => (EntropicGodAttackType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float SpinAngularOffset => ref NPC.ai[2];

        public ref float ZPosition => ref NPC.ai[3];

        public ref float TeleportVisualsInterpolant => ref NPC.localAI[0];

        public ref float ChargeAfterimageInterpolant => ref NPC.localAI[1];

        public ref float HeadRotation => ref NPC.localAI[2];

        public Vector2 TeleportVisualsAdjustedScale
        {
            get
            {
                float maxStretchFactor = 1.3f;
                Vector2 scale = Vector2.One * NPC.scale;
                if (TeleportVisualsInterpolant > 0f)
                {
                    // 1. Horizontal stretch.
                    if (TeleportVisualsInterpolant <= 0.166f)
                    {
                        float localInterpolant = GetLerpValue(0f, 0.166f, TeleportVisualsInterpolant);
                        scale.X *= Lerp(1f, maxStretchFactor, Sin(Pi * localInterpolant));
                        scale.Y *= Lerp(1f, 0.2f, Sin(Pi * localInterpolant));
                    }

                    // 2. Vertical stretch.
                    else if (TeleportVisualsInterpolant <= 0.333f)
                    {
                        float localInterpolant = GetLerpValue(0.166f, 0.333f, TeleportVisualsInterpolant);
                        scale.X *= Lerp(1f, 0.2f, Sin(Pi * localInterpolant));
                        scale.Y *= Lerp(1f, maxStretchFactor, Sin(Pi * localInterpolant));
                    }

                    // 3. Shrink into nothing on both axes.
                    else if (TeleportVisualsInterpolant <= 0.5f)
                    {
                        float localInterpolant = GetLerpValue(0.333f, 0.5f, TeleportVisualsInterpolant);
                        scale *= Pow(1f - localInterpolant, 4f);
                    }

                    // 4. Return to normal scale, use vertical overshoot at the end.
                    else
                    {
                        float localInterpolant = GetLerpValue(0.5f, 0.73f, TeleportVisualsInterpolant, true);

                        // 1.17234093 = 1 / sin(1.8)^6, acting as a correction factor to ensure that the final scale in the sinusoidal overshoot is one.
                        float verticalScaleOvershot = Pow(Sin(localInterpolant * 1.8f), 6f) * 1.17234093f;
                        scale.X = localInterpolant;
                        scale.Y = verticalScaleOvershot;
                    }
                }
                return scale;
            }
        }

        public bool ShouldDrawBehindTiles => ZPosition >= 0.2f;

        public Vector2 HeadOffset => -Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale * 60f;

        public static NPC Myself
        {
            get
            {
                if (myself is not null && !myself.active)
                    return null;

                return myself;
            }
            private set => myself = value;
        }

        public static int CometDamage => Main.expertMode ? 425 : 275;

        public static int FireballDamage => Main.expertMode ? 400 : 250;

        public static int NoxusGasDamage => Main.expertMode ? 425 : 275;

        public static int SpikeDamage => Main.expertMode ? 400 : 250;

        public static int ExplosionDamage => Main.expertMode ? 450 : 300;

        public static int NightmareDeathrayDamage => Main.expertMode ? 750 : 480;

        public static int DebuffDuration_RegularAttack => CalamityUtils.SecondsToFrames(5f);

        public static int DebuffDuration_PowerfulAttack => CalamityUtils.SecondsToFrames(10f);

        public static int IdealFightDuration => CalamityUtils.SecondsToFrames(180f);

        public static float MaxTimedDRDamageReduction => 0.45f;

        public static readonly Vector2 DefaultHandOffset = new(226f, 108f);

        // Used during the migraine stun behavior.
        public static readonly SoundStyle BrainRotSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusBrainRot");

        public static readonly SoundStyle ClapSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusClap") with { Volume = 1.5f };

        public static readonly SoundStyle ExplosionSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusExplosion");

        public static readonly SoundStyle ExplosionTeleportSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusExplosionTeleport");

        public static readonly SoundStyle FireballShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusFireballShoot") with { Volume = 0.65f, MaxInstances = 20 };

        public static readonly SoundStyle HitSound = new SoundStyle("NoxusBoss/Assets/Sounds/NPCHit/NoxusHurt") with { PitchVariance = 0.4f, Volume = 0.5f };

        public static readonly SoundStyle JumpscareSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusJumpscare");

        public static readonly SoundStyle NightmareDeathrayShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusNightmareDeathray") with { Volume = 1.56f };

        public static readonly SoundStyle ScreamSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusScream") with { Volume = 0.45f, MaxInstances = 20 };

        public static readonly SoundStyle TwinkleSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Noxus/NoxusTwinkle") with { MaxInstances = 5, PitchVariance = 0.16f };

        public const int DefaultTeleportDelay = 22;

        public const float Phase2LifeRatio = 0.65f;

        public const float Phase3LifeRatio = 0.25f;

        public const float DefaultDR = 0.23f;
        #endregion Fields and Properties

        #region AI
        public override void AI()
        {
            // Pick a target if the current one is invalid.
            bool invalidTargetIndex = NPC.target is < 0 or >= 255;
            if (invalidTargetIndex)
                NPC.TargetClosest();

            bool invalidTarget = Target.dead || !Target.active;
            if (invalidTarget)
                NPC.TargetClosest();

            if (!NPC.WithinRange(Target.Center, 4600f - Target.aggro))
                NPC.TargetClosest();

            // Hey bozo the player's gone. Leave.
            if (Target.dead || !Target.active)
                NPC.active = false;

            // Grant the target infinite flight.
            Target.wingTime = Target.wingTimeMax;
            Target.Calamity().infiniteFlight = true;

            // Disable rain.
            CalamityMod.CalamityMod.StopRain();

            // Set the global NPC instance.
            Myself = NPC;

            // Reset things every frame.
            NPC.damage = NPC.defDamage;
            NPC.defense = NPC.defDefense;
            NPC.dontTakeDamage = false;
            NPC.ShowNameOnHover = true;
            NPC.Calamity().DR = DefaultDR;

            // Make hands by default close and not use a rotation override.
            for (int i = 0; i < Hands.Length; i++)
            {
                Hands[i].ShouldOpen = false;
                Hands[i].RotationOverride = null;
            }

            // Make the head spin back into place.
            HeadRotation = HeadRotation.AngleTowards(0f, 0.02f);
            HeadSquishiness = Clamp(HeadSquishiness - 0.02f, 0f, 0.5f);

            // Ensure that the player receives the boss effects buff.
            NPC.Calamity().KillTime = IdealFightDuration;

            // Do not despawn.
            NPC.timeLeft = 7200;

            // Make the charge afterimage interpolant dissipate.
            ChargeAfterimageInterpolant = Clamp(ChargeAfterimageInterpolant * 0.98f - 0.02f, 0f, 1f);

            // Make the laser telegraph opacity dissipate. This is useful for cases where Noxus changes phases in the middle of the telegraph being prepared.
            LaserTelegraphOpacity = Clamp(LaserTelegraphOpacity - 0.01f, 0f, 1f);

            switch (CurrentAttack)
            {
                case EntropicGodAttackType.DarkExplosionCharges:
                    DoBehavior_DarkExplosionCharges();
                    break;
                case EntropicGodAttackType.DarkEnergyBoltHandWave:
                    DoBehavior_DarkEnergyBoltHandWave();
                    break;
                case EntropicGodAttackType.FireballBarrage:
                    DoBehavior_FireballBarrage();
                    break;
                case EntropicGodAttackType.RealityWarpSpinCharge:
                    DoBehavior_RealityWarpSpinCharge();
                    break;
                case EntropicGodAttackType.OrganizedPortalCometBursts:
                    DoBehavior_OrganizedPortalCometBursts();
                    break;
                case EntropicGodAttackType.HoveringHandGasBursts:
                    DoBehavior_HoveringHandGasBursts();
                    break;
                case EntropicGodAttackType.RapidExplosiveTeleports:
                    DoBehavior_RapidExplosiveTeleports();
                    break;
                case EntropicGodAttackType.Phase2Transition:
                    DoBehavior_Phase2Transition();
                    break;
                case EntropicGodAttackType.GeometricSpikesTeleportAndFireballs:
                    DoBehavior_GeometricSpikesTeleportAndFireballs();
                    break;
                case EntropicGodAttackType.TeleportAndShootNoxusGas:
                    DoBehavior_TeleportAndShootNoxusGas();
                    break;
                case EntropicGodAttackType.ThreeDimensionalNightmareDeathRay:
                    DoBehavior_ThreeDimensionalNightmareDeathRay();
                    break;
                case EntropicGodAttackType.Phase3Transition:
                    DoBehavior_Phase3Transition();
                    break;
                case EntropicGodAttackType.BrainFogAndThreeDimensionalCharges:
                    DoBehavior_BrainFogAndThreeDimensionalCharges();
                    break;
                case EntropicGodAttackType.PortalChainCharges:
                    DoBehavior_PortalChainCharges();
                    break;
                case EntropicGodAttackType.PortalChainCharges2:
                    DoBehavior_PortalChainCharges2();
                    break;
                case EntropicGodAttackType.MigraineAttack:
                    DoBehavior_MigraineAttack();
                    break;
                case EntropicGodAttackType.DeathAnimation:
                    DoBehavior_DeathAnimation();
                    break;
            }

            // Handle phase transition triggers.
            PreparePhaseTransitionsIfNecessary();

            // Update all hands.
            UpdateHands();

            // Perform Z position visual effects.
            PerformZPositionEffects();

            // Disable damage when invisible.
            if (NPC.Opacity <= 0.35f)
            {
                NPC.dontTakeDamage = true;
                NPC.damage = 0;
            }

            // Rotate slightly based on horizontal movement.
            bool teleported = NPC.position.Distance(NPC.oldPosition) >= 80f;
            NPC.rotation = Clamp((NPC.position.X - NPC.oldPosition.X) * 0.0024f, -0.16f, 0.16f);
            if (teleported)
                NPC.rotation = 0f;

            // Emit pitch black metaballs around based on movement.
            else if (NPC.Opacity >= 0.5f)
            {
                int metaballSpawnLoopCount = (int)Remap(NPC.Opacity, 1f, 0f, 9f, 1f) - (int)Remap(ZPosition, 0.1f, 1.2f, 0f, 5f);

                for (int i = 0; i < metaballSpawnLoopCount; i++)
                {
                    Vector2 gasSpawnPosition = NPC.Center + Main.rand.NextVector2Circular(82f, 82f) * TeleportVisualsAdjustedScale + (NPC.position - NPC.oldPosition).SafeNormalize(Vector2.UnitY) * 3f;
                    float gasSize = NPC.width * TeleportVisualsAdjustedScale.X * NPC.Opacity * 0.45f;
                    float angularOffset = Sin(Main.GlobalTimeWrappedHourly * 1.1f) * 0.77f;
                    PitchBlackMetaball.CreateParticle(gasSpawnPosition, Main.rand.NextVector2Circular(2f, 2f) + NPC.velocity.RotatedBy(angularOffset).RotatedByRandom(0.6f) * 0.26f, gasSize);
                }
            }

            // Gain permanent afterimages if in phase 2 and onward.
            if (CurrentPhase >= 1)
                ChargeAfterimageInterpolant = 1f;

            // Set the keyboard shader's eye intensity.
            EyeBrightness = BigEyeOpacity;

            // Increment timers.
            AttackTimer++;
            FightLength++;
        }       
        #endregion AI
    }
}
