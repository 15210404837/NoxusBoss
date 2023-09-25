using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.BaseEntities;
using NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class TelegraphedStarLaserbeam : BaseTelegraphedPrimitiveLaserbeam, IDrawsWithShader
    {
        public ref float MaxSpinAngularVelocity => ref Projectile.ai[2];

        // This laser should be drawn in the DrawWithShader interface, and as such should not be drawn manually via the base projectile.
        public override bool UseStandardDrawing => false;

        // This is used by the IDrawsWithShader to ensure that this projectile's drawing via DrawWithShader is performed under the Additive blend state.
        public bool ShaderShouldDrawAdditively => true;

        public override int TelegraphPointCount => 47;

        public override int LaserPointCount => 21;

        public override float MaxLaserLength => 5000f;

        public override float LaserExtendSpeedInterpolant => 0.085f;

        public override ManagedShader TelegraphShader => ShaderManager.GetShader("SideStreakShader");

        public override ManagedShader LaserShader => ShaderManager.GetShader("XerocStarLaserShader");

        public override void SetDefaults()
        {
            Projectile.width = 138;
            Projectile.height = 138;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(LaserLengthFactor);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LaserLengthFactor = reader.ReadSingle();
        }

        // This uses PreAI instead of PostAI to ensure that the AI hook uses the correct, updated velocity when deciding Projectile.rotation.
        public override bool PreAI()
        {
            // Stick to a star if possible. If there is no star, die immediately.
            List<Projectile> stars = AllProjectilesByID(ModContent.ProjectileType<ControlledStar>()).ToList();
            if (XerocSky.ManualSunScale >= 1.1f)
                Projectile.Center = XerocSky.ManualSunDrawPosition + Main.screenPosition;
            else if (stars.Any())
                Projectile.Center = stars.First().Center;
            else
            {
                Projectile.Kill();
                return false;
            }

            // This is calculated as necessary to ensure that a turn of exactly MaxSpinAngularVelocity * TelegraphTime is achieved
            // during the telegraph. This will use a turning factor based on the Convert01To010 function, or sin(pi * x)^p
            // In order to determine this, it is useful to frame the problem in terms of a series of discrete steps, because
            // the entire process is a series of discrete angular updates across a discrete number of frames.

            // In this case, N is the number of discrete steps being performed. For the sake of example, it will be assumed to be 6. p will be assumed to be 4.

            // Frame zero would be an angular offset of sin(pi * 0 / 6)^4, or 0.
            // Frame one would be sin(pi * 1 / 6)^4, or about 0.0625.
            // Frame two would be sin(pi * 2 / 6)^4, or about 0.5625.
            // Frame three would be sin(pi * 3 / 6)^4, or about 1.
            // Frame four would be sin(pi * 4 / 6)^4, or about 0.5625.
            // Frame five would be sin(pi * 5 / 6)^4, or about 0.0625.
            // And frame six would be sin(pi * 6 / 6)^4, or 0 again.

            // Adding this together results in a total offset of about 1.249. This is obviously quite a bit of turning for just five frames.
            // Naturally, it would make sense to "normalize" the offsets by dividing each step by 6, resulting in a total of around 0.622 instead.
            // Now, ideally this value would be 1, because if that were the case that'd mean simple multiplication would allow specification of how much
            // angular change happens across the entire process.

            // In order to achieve this, we must figure out what the 0.622 value approaches as N gets bigger and bigger and we slice the entire process into more and more frames.
            // This is exactly what the purpose of a definite integral is, interestingly! In order to figure out what that 0.622 approaches as N approaches infinity, we can do some
            // calculus to find the exact value, like so:

            // ∫(0, 1) sin(x * pi)^p =
            // 1 / pi * ∫(0, pi) sin(x)^p =
            // 0.375 for p = 4
            // In order to make the entire process add up neatly to MaxSpinAngularVelocity * TelegraphTime, a correction factor of 1 / 0.375 will be necessary.
            float spinInterpolant = GetLerpValue(0f, TelegraphTime, Time, true);
            float angularVelocity = Pow(Convert01To010(spinInterpolant), 4f) * MaxSpinAngularVelocity / 0.375f;
            Projectile.velocity = Projectile.velocity.RotatedBy(angularVelocity);

            // Fade out when the laser is about to die.
            Projectile.Opacity = GetLerpValue(TelegraphTime + LaserShootTime - 1f, TelegraphTime + LaserShootTime - 12f, Time, true);

            return true;
        }

        public override void OnLaserFire()
        {
            // Inform Xeroc that the laser has fired.
            if (XerocBoss.Myself is not null)
                XerocBoss.Myself.ai[3] = 1f;

            // Shake the screen.
            if (OverallShakeIntensity <= 7.5f)
                StartShakeAtPoint(Projectile.Center, 4f);

            // Blur the screen for a short moment.
            ScreenEffectSystem.SetBlurEffect(Main.LocalPlayer.Center - Vector2.UnitY * 400f, 1f, 13);

            // Play explosion sounds.
            SoundEngine.PlaySound(XerocBoss.ExplosionTeleportSound);
            SoundEngine.PlaySound(XerocBoss.SupernovaSound with { Volume = 0.7f, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest });
        }

        public override float TelegraphWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public override Color TelegraphColorFunction(float completionRatio)
        {
            float timeFadeOpacity = GetLerpValue(TelegraphTime - 1f, TelegraphTime - 7f, Time, true) * GetLerpValue(0f, TelegraphTime - 15f, Time, true);
            float endFadeOpacity = GetLerpValue(0f, 0.15f, completionRatio, true) * GetLerpValue(1f, 0.67f, completionRatio, true);
            return Color.LightGoldenrodYellow * endFadeOpacity * timeFadeOpacity * Projectile.Opacity * 0.26f;
        }

        public override float LaserWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public override Color LaserColorFunction(float completionRatio) => Color.OrangeRed * GetLerpValue(LaserShootTime - 1f, LaserShootTime - 8f, Time - TelegraphTime, true) * Projectile.Opacity;

        public override void PrepareTelegraphShader(ManagedShader telegraphShader)
        {
            telegraphShader.TrySetParameter("generalOpacity", Projectile.Opacity);
        }

        public override void PrepareLaserShader(ManagedShader laserShader)
        {
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"), 1);
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            // Draw a backglow for the laser if it's being fired.
            if (Time >= TelegraphTime)
            {
                DrawBloomLineTelegraph(Projectile.Center - Main.screenPosition, new()
                {
                    LineRotation = -Projectile.rotation,
                    Opacity = Sqrt(Projectile.Opacity),
                    WidthFactor = 0.001f,
                    LightStrength = 0.2f,
                    MainColor = Color.Wheat,
                    DarkerColor = Color.SaddleBrown,
                    BloomIntensity = Projectile.Opacity * 0.8f + 0.35f,
                    BloomOpacity = Projectile.Opacity,
                    Scale = Vector2.One * LaserLengthFactor * MaxLaserLength
                });
            }

            // Draw the regular telegraph/laser stuff.
            DrawTelegraphOrLaser();
        }
    }
}
