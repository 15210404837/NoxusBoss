using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.ShapeCurves;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class BookConstellation : BaseXerocConstellationProjectile
    {
        public float MagicCircleOpacity => GetLerpValue(45f, 105f, Time - ConvergeTime, true);

        public override int ConvergeTime => ConvergeTimeConst;

        public override int StarDrawIncrement => 1;

        public override float StarConvergenceSpeed => 0.0025f;

        public override float StarRandomOffsetFactor => 1f;

        protected override ShapeCurve constellationShape
        {
            get
            {
                ShapeCurveManager.TryFind("Book", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * Projectile.scale * 0.4f);
            }
        }

        public override Color DecidePrimaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Cyan, Color.Yellow, Pow(colorVariantInterpolant, 2f) * 0.5f) * 0.34f;
        }

        public override Color DecideSecondaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Wheat, Color.White, colorVariantInterpolant) * 0.43f;
        }

        public const int ConvergeTimeConst = 150;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 840;
            Projectile.height = 840;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = ConvergeTime + SuperCosmicBeam.DefaultLifetime + 175;
        }

        public override void PostAI()
        {
            // Fade in.
            Projectile.scale = GetLerpValue(0f, 45f, Projectile.timeLeft, true);
            Projectile.Opacity = GetLerpValue(0f, 45f, Time, true) * Projectile.scale;

            // Stick to Xeroc and inherit the current direction from him.
            Projectile.velocity = XerocBoss.Myself.ai[2].ToRotationVector2();
            Projectile.Center = XerocBoss.Myself.Center + Projectile.velocity * 200f;
            Projectile.rotation = XerocBoss.Myself.ai[2];

            // Create charge particles.
            if (MagicCircleOpacity >= 1f && Time <= ConvergeTime + 150f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 lightAimPosition = Projectile.Center + Projectile.velocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * Projectile.scale * 400f + Main.rand.NextVector2Circular(10f, 10f);
                    Vector2 lightSpawnPosition = Projectile.Center + Projectile.velocity * 75f + Projectile.velocity.RotatedByRandom(1.53f) * Main.rand.NextFloat(330f, 960f);
                    Vector2 lightVelocity = (lightAimPosition - lightSpawnPosition) * 0.06f;
                    SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, 0.33f, Color.Pink, 19, 0.04f, 3f, 8f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }
        }

        public void DrawBloom()
        {
            Color bloomCircleColor = Projectile.GetAlpha(Color.Bisque) * 0.5f;
            Vector2 bloomDrawPosition = Projectile.Center - Main.screenPosition;

            // Draw the bloom circle.
            Main.spriteBatch.Draw(bloomCircle, bloomDrawPosition, null, bloomCircleColor, 0f, bloomCircle.Size() * 0.5f, 5f, 0, 0f);

            // Draw bloom flares that go in opposite rotations.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * -0.4f;
            Color bloomFlareColor = Projectile.GetAlpha(new(75, 33, 164)) * 0.75f;
            Main.spriteBatch.Draw(bloomFlare, bloomDrawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, bloomDrawPosition, null, bloomFlareColor, bloomFlareRotation * -0.7f, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw bloom behind the book to give a nice ambient glow.
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawBloom();
            Main.spriteBatch.ResetBlendState();

            // Draw the book.
            base.PreDraw(ref lightColor);

            // Draw the magic circle.
            Main.spriteBatch.EnterShaderRegion();
            DrawMagicCircle();
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

        public void DrawMagicCircle()
        {
            // Acquire textures.
            Texture2D magicCircle = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Projectiles/XerocLightCircle").Value;
            Texture2D magicCircleCenter = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Projectiles/XerocLightCircleCenter").Value;

            // Determine draw values.
            Vector2 circleDrawPosition = Projectile.Center + Projectile.velocity * 200f - Main.screenPosition;
            Vector2 circleScale = Vector2.One * Projectile.scale * Projectile.Opacity * 1.5f;
            Color circleColor = Projectile.GetAlpha(new(92, 40, 204)) * MagicCircleOpacity;

            // Apply the shader.
            var magicCircleShader = ShaderManager.GetShader("MagicCircleShader");
            CalculatePrimitivePerspectiveMatricies(out Matrix viewMatrix, out Matrix projectionMatrix);
            magicCircleShader.TrySetParameter("orientationRotation", Projectile.rotation);
            magicCircleShader.TrySetParameter("spinRotation", -Main.GlobalTimeWrappedHourly * 3.87f);
            magicCircleShader.TrySetParameter("flip", Projectile.direction == -1f);
            magicCircleShader.TrySetParameter("uWorldViewProjection", viewMatrix * projectionMatrix);
            magicCircleShader.Apply();

            // Draw the circle.
            Main.EntitySpriteDraw(magicCircle, circleDrawPosition, null, circleColor with { A = 0 }, 0f, magicCircle.Size() * 0.5f, circleScale, 0, 0);

            // Draw the eye on top of the circle.
            magicCircleShader.TrySetParameter("spinRotation", 0f);
            magicCircleShader.Apply();
            Main.EntitySpriteDraw(magicCircleCenter, circleDrawPosition, null, Color.Lerp(circleColor, Color.White * MagicCircleOpacity, 0.5f) with { A = 0 }, 0f, magicCircleCenter.Size() * 0.5f, circleScale, 0, 0);
        }
    }
}
