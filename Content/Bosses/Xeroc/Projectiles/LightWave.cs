using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class LightWave : ModProjectile
    {
        public static int Lifetime => 30;

        public ref float Radius => ref Projectile.ai[0];

        public static Color DetermineExplosionColor()
        {
            Color c = Color.Lerp(Color.IndianRed, Color.Wheat, 0.24f);
            c = Color.Lerp(c, Color.Cyan, XerocSky.DifferentStarsInterpolant * 0.85f);
            return c with { A = 80 };
        }

        public static Texture2D ExplosionNoiseTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons").Value;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            // Make screen shove effects happen.
            if (Projectile.localAI[0] == 0f)
            {
                RadialScreenShoveSystem.Start(Projectile.Center, 20);
                StartShakeAtPoint(Projectile.Center, 6f);
                Projectile.localAI[0] = 1f;
            }

            // Cause the wave to expand outward, along with its hitbox.
            Radius = Lerp(Radius, 3200f, 0.039f);
            Projectile.scale = Lerp(1.2f, 4.5f, GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true));
            Projectile.Opacity = GetLerpValue(2f, 15f, Projectile.timeLeft, true);

            // Randomly create small light particles.
            float lightVelocityArc = Pi * GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            for (int i = 0; i < 25; i++)
            {
                Vector2 particleSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Radius * Projectile.scale * Main.rand.NextFloat(0.75f, 0.96f);
                Vector2 particleVelocity = (particleSpawnPosition - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(lightVelocityArc) * Main.rand.NextFloat(2f, 25f);
                SquishyLightParticle particle = new(particleSpawnPosition, particleVelocity, Main.rand.NextFloat(0.24f, 0.41f), Color.Lerp(Color.Wheat, Color.Yellow, Main.rand.NextFloat(0.7f)), Main.rand.Next(25, 44));
                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius * 0.4f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawData explosionDrawData = new(ExplosionNoiseTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

            var shockwaveShader = ShaderManager.GetShader("ShockwaveShader");
            shockwaveShader.TrySetParameter("shockwaveColor", DetermineExplosionColor().ToVector3());
            shockwaveShader.TrySetParameter("screenSize", new Vector2(Main.screenWidth, Main.screenHeight));
            shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
            shockwaveShader.TrySetParameter("projectilePosition", Projectile.Center - Main.screenPosition);
            shockwaveShader.TrySetParameter("shockwaveOpacityFactor", Projectile.Opacity);
            shockwaveShader.Apply();
            explosionDrawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
