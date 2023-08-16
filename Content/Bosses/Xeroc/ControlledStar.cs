using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class ControlledStar : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float UnstableOverlayInterpolant => ref Projectile.ai[1];

        public static int GrowToFullSizeTime => 60;

        public static float MaxScale => 4.5f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // No Xeroc? Die.
            if (XerocBoss.Myself is null)
                Projectile.Kill();

            Time++;

            if (UnstableOverlayInterpolant <= 0.01f)
                Projectile.scale = Pow(GetLerpValue(1f, GrowToFullSizeTime, Time, true), 4.1f) * MaxScale;

            // Release a bunch of smoke particles.
            for (int i = 0; i < 5; i++)
            {
                if (Projectile.scale <= 1f)
                    break;

                Vector2 smokeVelocity = -Vector2.UnitY * Main.rand.NextFloat(9f, 29f) + Main.rand.NextVector2Circular(8f, 8f);
                Color smokeColor = Color.Lerp(new(255, 205, 136), new(118, 53, 53), Main.rand.NextFloat(0.1f, 0.4f));
                HeavySmokeParticle smoke = new(Projectile.Center + Main.rand.NextVector2Circular(80f, 80f) * Projectile.scale, smokeVelocity, smokeColor * 0.4f, 15, Projectile.scale * 0.8f, 1f, Main.rand.NextFloat(0.04f), true, 0f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            Texture2D noise = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/FireNoise").Value;
            Texture2D noise2 = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise").Value;

            Color starColor = Color.Lerp(Color.Yellow, Color.IndianRed, 0.8f);
            starColor = Color.Lerp(starColor, Color.Wheat, UnstableOverlayInterpolant * 0.7f);

            var fireballShader = ShaderManager.GetShader("FireballShader");
            fireballShader.TrySetParameter("mainColor", starColor.ToVector3() * Projectile.Opacity);
            fireballShader.TrySetParameter("resolution", new Vector2(200f, 200f));
            fireballShader.TrySetParameter("speed", 0.76f);
            fireballShader.TrySetParameter("zoom", 0.0004f);
            fireballShader.TrySetParameter("dist", 60f);
            fireballShader.TrySetParameter("opacity", Projectile.Opacity);
            fireballShader.SetTexture(noise, 1);
            fireballShader.SetTexture(noise2, 2);
            fireballShader.Apply();

            Main.spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, pixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.5f, 0, 0f);

            fireballShader.TrySetParameter("mainColor", Color.Wheat.ToVector3() * Projectile.Opacity * 0.6f);
            fireballShader.Apply();
            Main.spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, pixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.31f, 0, 0f);

            // Draw a pure white overlay over the fireball if instructed.
            if (UnstableOverlayInterpolant >= 0.2f)
            {
                Main.pixelShader.CurrentTechnique.Passes[0].Apply();

                Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
                float glowPulse = Sin(Main.GlobalTimeWrappedHourly * UnstableOverlayInterpolant * 55f) * UnstableOverlayInterpolant * 0.35f;
                Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.White * UnstableOverlayInterpolant, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale * 0.7f + glowPulse, 0, 0f);
            }

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
