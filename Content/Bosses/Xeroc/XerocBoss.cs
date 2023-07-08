using System.Collections.Generic;
using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    [AutoloadBossHead]
    public partial class XerocBoss : ModNPC
    {
        #region Custom Types and Enumerations

        public class XerocHand
        {
            public int Frame;

            public int FrameTimer;

            public bool ShouldOpen;

            public bool UsePalmForm;

            public float Rotation;

            public bool HasSigil;

            public bool CanDoDamage;

            public float Opacity = 1f;

            public float ScaleFactor = 1f;

            public float TrailOpacity;

            public Vector2 Center;

            public Vector2 Velocity;

            public Vector2[] OldCenters = new Vector2[40];

            public PrimitiveTrail HandTrailDrawer;

            public XerocHand()
            {
                if (Main.netMode == NetmodeID.Server)
                    return;

                HandTrailDrawer = new(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);
            }

            public void ClearPositionCache()
            {
                for (int i = 0; i < OldCenters.Length; i++)
                    OldCenters[i] = Vector2.Zero;
            }

            public float FlameTrailWidthFunction(float completionRatio)
            {
                return MathHelper.SmoothStep(80f, 7.8f, completionRatio) * TrailOpacity;
            }

            public Color FlameTrailColorFunction(float completionRatio)
            {
                // Make the trail fade out at the end and fade in shparly at the start, to prevent the trail having a definitive, flat "start".
                float trailOpacity = GetLerpValue(0.75f, 0.27f, completionRatio, true) * GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;

                // Interpolate between a bunch of colors based on the completion ratio.
                Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
                Color middleColor = Color.Lerp(Color.OrangeRed, Color.Yellow, 0.4f);
                Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
                Color color = MulticolorLerp(Pow(completionRatio, 1.6f), startingColor, middleColor, endColor) * trailOpacity;

                color.A /= 8;
                return color * TrailOpacity;
            }

            public void WriteTo(BinaryWriter writer)
            {
                writer.Write((byte)CanDoDamage.ToInt());
                writer.Write((byte)HasSigil.ToInt());
                writer.Write(Opacity);
                writer.Write(Rotation);
                writer.WriteVector2(Center);
                writer.WriteVector2(Velocity);
            }

            public static XerocHand ReadFrom(BinaryReader reader)
            {
                return new()
                {
                    CanDoDamage = reader.ReadByte() != 0,
                    HasSigil = reader.ReadByte() != 0,
                    Opacity = reader.ReadSingle(),
                    Rotation = reader.ReadSingle(),
                    Center = reader.ReadVector2(),
                    Velocity = reader.ReadVector2()
                };
            }
        }

        public struct XerocWing
        {
            public float WingRotation
            {
                get;
                set;
            }

            public float PreviousWingRotation
            {
                get;
                set;
            }

            public float WingRotationDifferenceMovingAverage
            {
                get;
                set;
            }

            // Piecewise function variables for determining the angular offset of wings when flapping.
            // Positive rotations = upward flaps.
            // Negative rotations = downward flaps.
            public static CurveSegment Anticipation => new(EasingType.PolyOut, 0f, -0.4f, 0.65f, 3);

            public static CurveSegment Flap => new(EasingType.PolyIn, 0.5f, Anticipation.EndingHeight, -1.88f, 4);

            public static CurveSegment Rest => new(EasingType.PolyIn, 0.71f, Flap.EndingHeight, 0.59f, 3);

            public static CurveSegment Recovery => new(EasingType.PolyIn, 0.9f, Rest.EndingHeight, -0.4f - Rest.EndingHeight, 2);

            public void Update(WingMotionState motionState, float animationCompletion, float instanceRatio)
            {
                PreviousWingRotation = WingRotation;

                switch (motionState)
                {
                    case WingMotionState.RiseUpward:
                        WingRotation = (-0.6f).AngleLerp(0.36f - instanceRatio * 0.25f, animationCompletion);
                        break;
                    case WingMotionState.Flap:
                        WingRotation = PiecewiseAnimation((animationCompletion + Lerp(instanceRatio, 0f, 0.5f)) % 1f, Anticipation, Flap, Rest, Recovery);
                        break;
                }

                WingRotationDifferenceMovingAverage = Lerp(WingRotationDifferenceMovingAverage, WingRotation - PreviousWingRotation, 0.15f);
            }
        }

        public enum WingMotionState
        {
            Flap,
            RiseUpward,
        }

        public enum XerocAttackType
        {
            Awaken,
            OpenScreenTear,
            RoarAnimation,

            // Magic attacks.
            ConjureExplodingStars,
            ShootArcingStarburstsFromEye,
            RealityTearDaggers,
            LightBeamTransformation,

            // Fire attacks.
            StarManagement,
            PortalLaserBarrages,
            StealSun,
            CircularPortalLaserBarrages,

            // General cosmic attacks.
            StarManagement_CrushIntoQuasar,
            StarConvergenceAndRedirecting,
            BrightStarJumpscares,
            SwordConstellation,
            SwordConstellation2,

            // Reality manipulation attacks.
            ScreenSlicesWithTeleport,
            PunchesWithScreenSlices,

            // TODO -- Find out something for phase transition attacks.

            DeathAnimation
        }

        #endregion Custom Types and Enumerations

        #region Fields and Properties

        private static NPC myself;

        public SlotId IdleSoundSlot;

        public List<XerocHand> Hands = new();

        public List<Vector2> StarSpawnOffsets = new();

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

        public int SwordSlashCounter
        {
            get;
            set;
        }

        public int SwordSlashDirection
        {
            get;
            set;
        }

        public float PupilScale
        {
            get;
            set;
        } = 1f;

        public float SkyEyeDirection
        {
            get;
            set;
        }

        public float UniversalUpdateTimer
        {
            get;
            set;
        }

        public float ZPosition
        {
            get;
            set;
        }

        public float UniversalBlackOverlayInterpolant
        {
            get;
            set;
        }

        public bool DrawCongratulatoryText
        {
            get;
            set;
        }

        public Vector2 PupilOffset
        {
            get;
            set;
        }

        public Vector2 GeneralHoverOffset
        {
            get;
            set;
        }

        public Vector2 CensorPosition
        {
            get;
            set;
        }

        public Vector2 PunchDestination
        {
            get;
            set;
        }

        public Vector2 SwordChargeDestination
        {
            get;
            set;
        }

        public XerocWing[] Wings
        {
            get;
            set;
        }

        public bool ShouldDrawBehindTiles => ZPosition >= 0.2f;

        public float LifeRatio => NPC.life / (float)NPC.lifeMax;

        public float ZPositionOpacity => Remap(ZPosition, 0f, 2.1f, 1f, 0.58f);

        public Player Target => Main.player[NPC.target];

        public Vector2 TeleportVisualsAdjustedScale
        {
            get
            {
                float maxStretchFactor = 1.4f;
                Vector2 scale = Vector2.One * NPC.scale;
                if (TeleportVisualsInterpolant > 0f)
                {
                    // 1. Horizontal stretch.
                    if (TeleportVisualsInterpolant <= 0.25f)
                    {
                        float localInterpolant = GetLerpValue(0f, 0.25f, TeleportVisualsInterpolant);
                        scale.X *= Lerp(1f, maxStretchFactor, Sin(Pi * localInterpolant));
                        scale.Y *= Lerp(1f, 0.2f, localInterpolant);
                    }

                    // 2. Vertical collapse.
                    else if (TeleportVisualsInterpolant <= 0.5f)
                    {
                        float localInterpolant = Pow(GetLerpValue(0.5f, 0.25f, TeleportVisualsInterpolant), 1f);
                        scale.X = localInterpolant;
                        scale.Y = localInterpolant * 0.2f;
                    }

                    // 3. Return to normal scale, use vertical overshoot at the end.
                    else
                    {
                        float localInterpolant = GetLerpValue(0.5f, 0.92f, TeleportVisualsInterpolant, true);

                        // 1.17234093 = 1 / sin(1.8)^6, acting as a correction factor to ensure that the final scale in the sinusoidal overshoot is one.
                        float verticalScaleOvershot = Pow(Sin(localInterpolant * 1.8f), 6f) * 1.17234093f;
                        scale.X = localInterpolant;
                        scale.Y = verticalScaleOvershot;
                    }
                }
                return scale;
            }
        }

        public XerocAttackType CurrentAttack
        {
            get => (XerocAttackType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float TeleportVisualsInterpolant => ref NPC.localAI[0];

        public ref float PupilTelegraphArc => ref NPC.localAI[1];

        public ref float PupilTelegraphOpacity => ref NPC.localAI[2];

        public WingMotionState WingsMotionState
        {
            get => (WingMotionState)NPC.localAI[3];
            set => NPC.localAI[3] = (int)value;
        }

        public Vector2 EyePosition => NPC.Center - Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale.Y * (NPC.scale * 110f + 120f);

        public Vector2 IdealCensorPosition => EyePosition;

        public Vector2 PupilPosition => EyePosition + TeleportVisualsAdjustedScale * PupilOffset;

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

        public static readonly SoundStyle ExplosionTeleportSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocExplosion") with { Volume = 1.3f };

        public static readonly SoundStyle FingerSnapSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocFingerSnap") with { Volume = 1.4f };

        public static readonly SoundStyle HummSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocHumm") with { Volume = 1.1f, IsLooped = true };

        public static readonly SoundStyle PortalCastSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocPortalCast") with { Volume = 1.2f };

        public static readonly SoundStyle QuasarLoopSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/QuasarLoopSound") with { Volume = 1.2f, IsLooped = true };

        public static readonly SoundStyle ScreamSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocScream") with { Volume = 1.05f, MaxInstances = 20 };

        public static readonly SoundStyle SliceSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocSliceTelegraph") with { Volume = 1.05f, MaxInstances = 20 };

        public static readonly SoundStyle SunFireballShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/SunFireballShootSound") with { Volume = 1.05f, MaxInstances = 5 };

        public static readonly SoundStyle SupernovaSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocSupernova") with { Volume = 0.8f, MaxInstances = 20 };

        public static int WingCount => 3;

        public static int BackgroundStarDamage => Main.expertMode ? 400 : 300;

        public static int FireballDamage => Main.expertMode ? 400 : 300;

        public static int StarburstDamage => Main.expertMode ? 400 : 300;

        public static int SupernovaEnergyDamage => Main.expertMode ? 400 : 300;

        public static int StarDamage => Main.expertMode ? 450 : 360;

        public static int DaggerDamage => Main.expertMode ? 450 : 360;

        public static int ScreenSliceDamage => Main.expertMode ? 450 : 360;

        public static int LightLaserbeamDamage => Main.expertMode ? 720 : 480;

        public static int SwordConstellationDamage => Main.expertMode ? 720 : 480;

        public static int QuasarDamage => Main.expertMode ? 775 : 500;

        public static int SuperLaserbeamDamage => Main.expertMode ? 800 : 500;

        public const int DefaultTeleportDelay = 8;

        public const float DefaultDR = 0.25f;

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

            // Disable rain.
            CalamityMod.CalamityMod.StopRain();

            // Reset things every frame.
            NPC.damage = NPC.defDamage;
            NPC.defense = NPC.defDefense;
            NPC.dontTakeDamage = false;
            NPC.ShowNameOnHover = true;
            NPC.Calamity().DR = DefaultDR;
            NPC.Calamity().ProvidesProximityRage = true;
            TeleportVisualsInterpolant = 0f;

            // Make certain values approach a stable state over time.
            PupilOffset = Vector2.Lerp(PupilOffset, Vector2.Zero, 0.084f);
            PupilScale = Lerp(PupilScale, 1f, 0.072f);
            PupilTelegraphOpacity = Clamp(PupilTelegraphOpacity - 0.02f, 0f, 1f);
            PupilTelegraphArc *= 0.96f;

            // Ensure that the player receives the boss effects buff.
            NPC.Calamity().KillTime = 1800;

            // Do not despawn.
            NPC.timeLeft = 7200;

            // Say NO to weather that destroys the ambience!
            // They cannot take away your gameplay aesthetic without your consent.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CalamityMod.CalamityMod.StopRain();
                Sandstorm.StopSandstorm();
            }

            // Set the global NPC instance.
            Myself = NPC;

            switch (CurrentAttack)
            {
                case XerocAttackType.Awaken:
                    DoBehavior_Awaken();
                    break;
                case XerocAttackType.OpenScreenTear:
                    DoBehavior_OpenScreenTear();
                    break;
                case XerocAttackType.RoarAnimation:
                    DoBehavior_RoarAnimation();
                    break;

                case XerocAttackType.ConjureExplodingStars:
                    DoBehavior_ConjureExplodingStars();
                    break;
                case XerocAttackType.ShootArcingStarburstsFromEye:
                    DoBehavior_ShootArcingStarburstsFromEye();
                    break;
                case XerocAttackType.RealityTearDaggers:
                    DoBehavior_RealityTearDaggers();
                    break;
                case XerocAttackType.LightBeamTransformation:
                    DoBehavior_LightBeamTransformation();
                    break;

                case XerocAttackType.StarManagement:
                    DoBehavior_StarManagement();
                    break;
                case XerocAttackType.PortalLaserBarrages:
                    DoBehavior_PortalLaserBarrages();
                    break;
                case XerocAttackType.StealSun:
                    DoBehavior_StealSun();
                    break;
                case XerocAttackType.CircularPortalLaserBarrages:
                    DoBehavior_CircularPortalLaserBarrages();
                    break;

                case XerocAttackType.StarManagement_CrushIntoQuasar:
                    DoBehavior_StarManagement_CrushIntoQuasar();
                    break;
                case XerocAttackType.StarConvergenceAndRedirecting:
                    DoBehavior_StarConvergenceAndRedirecting();
                    break;
                case XerocAttackType.BrightStarJumpscares:
                    DoBehavior_BrightStarJumpscares();
                    break;
                case XerocAttackType.SwordConstellation:
                    DoBehavior_SwordConstellation();
                    break;
                case XerocAttackType.SwordConstellation2:
                    DoBehavior_SwordConstellation2();
                    break;

                case XerocAttackType.ScreenSlicesWithTeleport:
                    DoBehavior_ScreenSlicesWithTeleport();
                    break;
                case XerocAttackType.PunchesWithScreenSlices:
                    DoBehavior_PunchesWithScreenSlices();
                    break;

                case XerocAttackType.DeathAnimation:
                    DoBehavior_DeathAnimation();
                    break;
            }

            // Disable damage when invisible.
            if (NPC.Opacity <= 0.35f)
            {
                NPC.ShowNameOnHover = false;
                NPC.dontTakeDamage = true;
                NPC.damage = 0;
            }

            // Get rid of all falling stars. Their noises completely ruin the ambience.
            foreach (Projectile star in AllProjectilesByID(ProjectileID.FallingStar))
                star.active = false;

            // Make the censor intentionally move in a bit of a "choppy" way, where it tries to stick to the ideal position, but only if it's far
            // enough away.
            // As a failsafe, it sticks perfectly if Xeroc is moving really quickly so that it doesn't gain too large of a one-frame delay. Don't want to be
            // accidentally revealing what's behind there, after all.
            if (NPC.velocity.Length() >= 32f || !CensorPosition.WithinRange(IdealCensorPosition, 12f))
                CensorPosition = IdealCensorPosition;

            // Increment timers.
            AttackTimer++;
            UniversalUpdateTimer++;

            // Perform Z position visual effects.
            PerformZPositionEffects();

            // Update the idle sound.
            UpdateIdleSound();

            // Make it night time.
            Main.dayTime = false;
            Main.time = Lerp((float)Main.time, 16000f, 0.14f);

            // Update hands.
            foreach (XerocHand hand in Hands)
            {
                hand.Center += hand.Velocity;
                hand.FrameTimer++;
                if (hand.FrameTimer % 2 == 1)
                    hand.Frame = Clamp(hand.Frame - hand.ShouldOpen.ToDirectionInt(), 0, 2);

                if (!hand.ShouldOpen)
                    hand.ShouldOpen = true;
                hand.Opacity = Clamp(hand.Opacity + 0.03f, 0f, 1f);

                for (int i = hand.OldCenters.Length - 1; i >= 1; i--)
                    hand.OldCenters[i] = hand.OldCenters[i - 1];

                hand.OldCenters[0] = hand.Center;
            }

            // Rotate based on horizontal speed.
            NPC.rotation = NPC.velocity.X * 0.001f;
        }

        #endregion AI
    }
}
