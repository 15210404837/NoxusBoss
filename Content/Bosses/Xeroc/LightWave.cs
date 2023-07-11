﻿using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class LightWave : ModProjectile
    {
        public static int Lifetime => 25;

        public ref float Radius => ref Projectile.ai[0];

        public static float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => GetLerpValue(2400f, 1000f, distanceFromPlayer, true) * (1f - lifetimeCompletionRatio) * 5.5f;

        public static Color DetermineExplosionColor() => Color.Lerp(Color.IndianRed, Color.Wheat, 0.24f) with { A = 80 };

        public static Texture2D ExplosionNoiseTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons").Value;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosion");
        }

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
                Projectile.localAI[0] = 1f;
            }

            // Do screen shake effects.
            float distanceFromPlayer = Projectile.Distance(Main.LocalPlayer.Center);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = MathF.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, DetermineScreenShakePower(1f - Projectile.timeLeft / (float)Lifetime, distanceFromPlayer));

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

            var shader = GameShaders.Misc[$"{Mod.Name}:ShockwaveShader"];
            shader.UseColor(DetermineExplosionColor());
            shader.Shader.Parameters["screenSize"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            shader.Shader.Parameters["explosionDistance"].SetValue(Radius * Projectile.scale * 0.5f);
            shader.Shader.Parameters["projectilePosition"].SetValue(Projectile.Center - Main.screenPosition);
            shader.Shader.Parameters["shockwaveOpacityFactor"].SetValue(Projectile.Opacity);
            shader.Apply();
            explosionDrawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}