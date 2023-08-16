using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class SuperLightBeam : ModProjectile, IDrawPixelatedPrims
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            private set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLengthFactor => ref Projectile.ai[1];

        public static int LaserLifetime => 360;

        public static float MaxLaserLength => 5000f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20000;

        public override void SetDefaults()
        {
            Projectile.width = 880;
            Projectile.height = 880;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            // Stick to Xeroc if possible.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = XerocBoss.Myself.Center - Projectile.velocity * MaxLaserLength * LaserLengthFactor * 0.5f;

            // Define the laser's rotation.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Fade in.
            LaserLengthFactor = Clamp(LaserLengthFactor + 0.15f, 0f, 1f);
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            // Decide the scale of the laser.
            Projectile.scale = GetLerpValue(0f, 12f, Time, true) * GetLerpValue(0f, 12f, LaserLifetime - Time, true);

            if (Time >= LaserLifetime)
                Projectile.Kill();

            Time++;
        }

        public float LaserWidthFunction(float completionRatio) => Projectile.scale * Projectile.width;

        public Color LaserColorFunction(float completionRatio) => Color.Coral * Projectile.Opacity * 0.24f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Give a generous time window before the laser starts doing damage, in case players are unfortunate enough to be standing right below Xeroc when
            // it appears.
            if (Time <= 40f)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity * LaserLengthFactor * MaxLaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public void Draw()
        {
            // Initialize the laser drawer.
            var laserShader = ShaderManager.GetShader("XerocStarLaserShader");
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, laserShader);

            // Draw a backglow for the laser.
            Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 center = Projectile.Center + Projectile.velocity * LaserLengthFactor * MaxLaserLength * 0.5f;
            DrawBloomLineTelegraph(center - Main.screenPosition, new()
            {
                LineRotation = -Projectile.rotation,
                Opacity = Sqrt(Projectile.Opacity),
                WidthFactor = 0.001f,
                LightStrength = 0.42f,
                MainColor = Color.LightCoral,
                DarkerColor = Color.Red,
                BloomIntensity = Projectile.Opacity * 0.8f + 0.35f,
                BloomOpacity = Projectile.Opacity,
                Scale = Vector2.One * LaserLengthFactor * MaxLaserLength * Projectile.Opacity * 5f
            }, true);

            // Draw the laser after the telegraph is no longer necessary.
            Vector2[] laserPoints = new Vector2[]
            {
                Projectile.Center,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.25f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.5f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.75f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength,
            };
            laserShader.TrySetParameter("uStretchReverseFactor", 0.15f);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/FireNoise"), 1);
            LaserDrawer.Draw(laserPoints, -Main.screenPosition, 41);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
