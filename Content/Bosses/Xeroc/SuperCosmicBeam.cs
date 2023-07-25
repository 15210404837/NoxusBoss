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
    public class SuperCosmicBeam : ModProjectile, IDrawPixelatedPrims
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            private set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float LaserLengthFactor => ref Projectile.ai[1];

        public static int LaserLifetime => 540;

        public static float MaxLaserLength => 7200f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cosmic Deathray");
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 800;
            Projectile.height = 800;
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

            // Define the laser's direction.
            Projectile.rotation = XerocBoss.Myself.ai[2];
            Projectile.velocity = Projectile.rotation.ToRotationVector2();
            Projectile.Center = XerocBoss.Myself.Center + Projectile.velocity * 300f;

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

        public Color LaserColorFunction(float completionRatio) => new Color(53, 18, 100) * GetLerpValue(0f, 0.12f, completionRatio, true) * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Give a short time window before the laser starts doing damage, to prevent cheap hits.
            if (Time <= 22f)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity * LaserLengthFactor * MaxLaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public void Draw()
        {
            // Initialize the laser drawer.
            var gd = Main.instance.GraphicsDevice;
            var laserShader = ShaderManager.GetShader("XerocCosmicLaserShader");
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, laserShader);

            // Draw the laser after the telegraph is no longer necessary.
            Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2[] laserPoints = new Vector2[]
            {
                Projectile.Center,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.25f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.5f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength * 0.75f,
                Projectile.Center + laserDirection * LaserLengthFactor * MaxLaserLength,
            };
            laserShader.TrySetParameter("uStretchReverseFactor", 0.15f);
            laserShader.TrySetParameter("scrollSpeedFactor", 0.8f);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Cosmos"), 1);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"), 2);
            laserShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"), 3);
            LaserDrawer.Draw(laserPoints, -Main.screenPosition, 45);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
