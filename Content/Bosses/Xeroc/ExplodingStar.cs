using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class ExplodingStar : ModProjectile, IDrawsWithShader
    {
        public bool DrawAdditiveShader => true;

        public ref float Temperature => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public ref float ScaleGrowBase => ref Projectile.ai[0];

        public ref float StarburstShootSpeedFactor => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Star");
        }

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 40;
        }

        public override void AI()
        {
            // Initialize the star temperature. This is used for determining colors.
            if (Temperature <= 0f)
                Temperature = Main.rand.NextFloat(3000f, 32000f);

            Time++;

            // Perform scale effects to do the explosion.
            if (Time <= 20f)
                Projectile.scale = Pow(GetLerpValue(0f, 20f, Time, true), 2.7f);
            else
            {
                if (ScaleGrowBase < 1f)
                    ScaleGrowBase = 1.066f;

                Projectile.scale *= ScaleGrowBase;
            }

            float fadeIn = GetLerpValue(0.05f, 0.2f, Projectile.scale, true);
            float fadeOut = GetLerpValue(40f, 31f, Time, true);
            Projectile.Opacity = fadeIn * fadeOut;

            // Create screenshake and play explosion sounds when ready.
            if (Time == 11f)
            {
                float screenShakePower = GetLerpValue(1600f, 750f, Main.LocalPlayer.Distance(Projectile.Center), true) * 3f;
                Main.LocalPlayer.Calamity().GeneralScreenShakePower += screenShakePower;
                SoundEngine.PlaySound(XerocBoss.ExplosionTeleportSound with { Pitch = 0.5f, MaxInstances = 2 });
                ScreenEffectSystem.SetFlashEffect(Projectile.Center, 1f, 30);
            }
            if (fadeOut <= 0.8f)
                Projectile.damage = 0;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.19f, targetHitbox);
        }

        public override void Kill(int timeLeft)
        {
            // Release and even spread of starbursts.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int starburstCount = 5;
                int starburstID = ModContent.ProjectileType<ArcingStarburst>();
                if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.StarConvergenceAndRedirecting)
                {
                    starburstCount = 7;
                    starburstID = ModContent.ProjectileType<Starburst>();
                }

                Vector2 directionToTarget = Projectile.SafeDirectionTo(Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center);
                for (int i = 0; i < starburstCount; i++)
                {
                    Vector2 starburstVelocity = directionToTarget.RotatedBy(TwoPi * i / starburstCount) * 22f + Main.rand.NextVector2Circular(4f, 4f);
                    NewProjectileBetter(Projectile.Center, starburstVelocity, starburstID, XerocBoss.StarburstDamage, 0f, -1);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            Texture2D noise = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/FireNoise").Value;
            Texture2D noise2 = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise").Value;

            float colorInterpolant = GetLerpValue(3000f, 32000f, Temperature, true);
            Color starColor = CalamityUtils.MulticolorLerp(colorInterpolant, Color.Red, Color.Orange, Color.Yellow);
            starColor = Color.Lerp(starColor, Color.IndianRed, 0.32f);

            Effect fireballShader = GameShaders.Misc[$"{Mod.Name}:FireballShader"].Shader;
            fireballShader.Parameters["sampleTexture2"].SetValue(noise);
            fireballShader.Parameters["sampleTexture3"].SetValue(noise2);
            fireballShader.Parameters["mainColor"].SetValue(starColor.ToVector3() * Projectile.Opacity);
            fireballShader.Parameters["resolution"].SetValue(new Vector2(100f, 100f));
            fireballShader.Parameters["speed"].SetValue(0.76f);
            fireballShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            fireballShader.Parameters["zoom"].SetValue(0.0004f);
            fireballShader.Parameters["dist"].SetValue(60f);
            fireballShader.Parameters["opacity"].SetValue(Projectile.Opacity);
            fireballShader.CurrentTechnique.Passes[0].Apply();

            spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, pixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.3f, SpriteEffects.None, 0f);

            fireballShader.Parameters["mainColor"].SetValue(Color.Wheat.ToVector3() * Projectile.Opacity * 0.6f);
            fireballShader.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, pixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.08f, SpriteEffects.None, 0f);
        }
    }
}
