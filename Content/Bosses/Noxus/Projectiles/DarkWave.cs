using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Noxus.Projectiles
{
    public class DarkWave : ModProjectile
    {
        public int Lifetime = 60;

        public float Opacity = 1f;

        public float MinScale = 1.2f;

        public float MaxScale = 5f;

        public float MaxRadius = 2000f;

        public float RadiusExpandRateInterpolant = 0.08f;

        public ref float Radius => ref Projectile.ai[0];

        public static Color DetermineExplosionColor() => Color.Lerp(Color.MediumSlateBlue, Color.Black, 0.1f);

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
            // Do screen shake effects.
            if (Projectile.localAI[0] == 0f)
            {
                StartShakeAtPoint(Projectile.Center, 7f);
                Projectile.localAI[0] = 1f;
            }

            // Cause the wave to expand outward, along with its hitbox.
            Radius = Lerp(Radius, MaxRadius, RadiusExpandRateInterpolant);
            Projectile.scale = Lerp(MinScale, MaxScale, GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true));

            if (Projectile.ai[1] != 0f)
                Projectile.Opacity = Projectile.ai[1];
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
