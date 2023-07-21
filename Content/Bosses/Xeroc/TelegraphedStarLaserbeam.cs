using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class TelegraphedStarLaserbeam : ModProjectile, IDrawsWithShader
    {
        public PrimitiveTrail TelegraphDrawer
        {
            get;
            private set;
        }

        public PrimitiveTrail LaserDrawer
        {
            get;
            private set;
        }

        public bool DrawAdditiveShader => true;

        public ref float TelegraphTime => ref Projectile.ai[0];

        public ref float MaxSpinAngularVelocity => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public ref float LaserLengthFactor => ref Projectile.localAI[1];

        public static int LaserShootTime => 32;

        public static float MaxLaserLength => 5000f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Solar Burst");
        }

        public override void SetDefaults()
        {
            Projectile.width = 138;
            Projectile.height = 138;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(LaserLengthFactor);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LaserLengthFactor = reader.ReadSingle();
        }

        public override void AI()
        {
            // Stick to a star if possible.
            List<Projectile> stars = AllProjectilesByID(ModContent.ProjectileType<ControlledStar>()).ToList();
            if (XerocSky.ManualSunScale >= 1.1f)
                Projectile.Center = XerocSky.ManualSunDrawPosition + Main.screenPosition;
            else if (stars.Any())
                Projectile.Center = stars.First().Center;
            else
                Projectile.Kill();

            // Make the laser extend after the telegraph has vanished.
            if (Time >= TelegraphTime)
                LaserLengthFactor = Lerp(LaserLengthFactor, 1f, 0.08f);

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
            float angularVelocity = Pow(CalamityUtils.Convert01To010(spinInterpolant), 4f) * MaxSpinAngularVelocity / 0.375f;
            Projectile.velocity = Projectile.velocity.RotatedBy(angularVelocity);
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Define the universal opacity.
            Projectile.Opacity = GetLerpValue(TelegraphTime + LaserShootTime - 1f, TelegraphTime + LaserShootTime - 12f, Time, true);

            if (Time >= TelegraphTime + LaserShootTime)
                Projectile.Kill();

            // Apply screen impact effects when the laser fires.
            if (Time == TelegraphTime - 1f)
            {
                if (XerocBoss.Myself is not null)
                    XerocBoss.Myself.ai[3] = 1f;

                ScreenEffectSystem.SetBlurEffect(Main.LocalPlayer.Center - Vector2.UnitY * 400f, 1f, 13);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 12f;
                SoundEngine.PlaySound(XerocBoss.ExplosionTeleportSound);
                SoundEngine.PlaySound(XerocBoss.SupernovaSound with { Volume = 0.4f });
            }

            Time++;
        }

        public float TelegraphWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public Color TelegraphColorFunction(float completionRatio)
        {
            float timeFadeOpacity = GetLerpValue(TelegraphTime - 1f, TelegraphTime - 7f, Time, true) * GetLerpValue(0f, TelegraphTime - 15f, Time, true);
            float endFadeOpacity = GetLerpValue(0f, 0.15f, completionRatio, true) * GetLerpValue(1f, 0.67f, completionRatio, true);
            return Color.LightGoldenrodYellow * endFadeOpacity * timeFadeOpacity * Projectile.Opacity * 0.26f;
        }

        public float LaserWidthFunction(float completionRatio) => Projectile.Opacity * Projectile.width;

        public Color LaserColorFunction(float completionRatio) => Color.OrangeRed * GetLerpValue(LaserShootTime - 1f, LaserShootTime - 8f, Time - TelegraphTime, true) * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= TelegraphTime)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * LaserLengthFactor * MaxLaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.Opacity * Projectile.width * 0.9f, ref _);
        }

        public override void Kill(int timeLeft)
        {
            TelegraphDrawer?.BaseEffect?.Dispose();
            LaserDrawer?.BaseEffect?.Dispose();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Initialize primitive drawers.
            var telegraphShader = GameShaders.Misc[$"{Mod.Name}:SideStreakShader"];
            var laserShader = GameShaders.Misc[$"{Mod.Name}:XerocStarLaserShader"];
            TelegraphDrawer ??= new(TelegraphWidthFunction, TelegraphColorFunction, null, telegraphShader);
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, laserShader);

            // Draw the telegraph at first.
            Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            if (Time <= TelegraphTime)
            {
                Vector2[] telegraphPoints = new Vector2[]
                {
                    Projectile.Center,
                    Projectile.Center + laserDirection * MaxLaserLength * 0.2f,
                    Projectile.Center + laserDirection * MaxLaserLength * 0.4f,
                    Projectile.Center + laserDirection * MaxLaserLength * 0.6f,
                    Projectile.Center + laserDirection * MaxLaserLength * 0.8f,
                    Projectile.Center + laserDirection * MaxLaserLength,
                };
                TelegraphDrawer.Draw(telegraphPoints, -Main.screenPosition, 47);
                return;
            }

            // Draw a backglow for the lasers.
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

            // Draw the laser after the telegraph is no longer necessary.
            Vector2[] laserPoints = new Vector2[]
            {
                Projectile.Center,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.2f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.4f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.6f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.8f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength,
            };
            laserShader.SetShaderTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"));
            LaserDrawer.Draw(laserPoints, -Projectile.velocity * LaserLengthFactor * 150f - Main.screenPosition, 21);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
