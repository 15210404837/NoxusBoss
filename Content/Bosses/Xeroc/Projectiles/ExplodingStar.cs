﻿using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class ExplodingStar : ModProjectile, IDrawsWithShader
    {
        public bool DrawAdditiveShader => true;

        public static bool FromStarConvergenceAttack => XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.StarConvergenceAndRedirecting;

        public ref float Temperature => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public ref float ScaleGrowBase => ref Projectile.ai[0];

        public ref float StarburstShootSpeedFactor => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 40;

            if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.ConjureExplodingStars)
                Projectile.timeLeft = 30;
            if (FromStarConvergenceAttack)
                Projectile.timeLeft = 15;
        }

        public override void AI()
        {
            // Initialize the star temperature. This is used for determining colors.
            if (Temperature <= 0f)
                Temperature = Main.rand.NextFloat(3000f, 32000f);

            Time++;

            // Perform scale effects to do the explosion.
            if (Time <= 20f && !FromStarConvergenceAttack)
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
                StartShakeAtPoint(Projectile.Center, 5f, TwoPi, Vector2.UnitX, 0.1f);

                SoundStyle explosionSound = FromStarConvergenceAttack ? XerocBoss.SupernovaSound : XerocBoss.ExplosionTeleportSound with { Pitch = 0.5f };
                SoundEngine.PlaySound(explosionSound with { MaxInstances = 3 });
                ScreenEffectSystem.SetFlashEffect(Projectile.Center, 1f, 30);
                XerocKeyboardShader.BrightnessIntensity += 0.23f;
            }
            if (fadeOut <= 0.8f)
                Projectile.damage = 0;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.19f, targetHitbox);
        }

        public override void Kill(int timeLeft)
        {
            // Release and even spread of starbursts.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int starburstCount = 5;
                int starburstID = ModContent.ProjectileType<ArcingStarburst>();
                float starburstSpread = TwoPi;
                float starburstSpeed = 22f;
                if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.StarConvergenceAndRedirecting)
                    starburstCount = 7;

                if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.BrightStarJumpscares)
                    return;

                Vector2 directionToTarget = Projectile.SafeDirectionTo(Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center);
                for (int i = 0; i < starburstCount; i++)
                {
                    Vector2 starburstVelocity = directionToTarget.RotatedBy(Lerp(-starburstSpread, starburstSpread, i / (float)(starburstCount - 1f)) * 0.5f) * starburstSpeed + Main.rand.NextVector2Circular(starburstSpeed, starburstSpeed) / 11f;
                    NewProjectileBetter(Projectile.Center, starburstVelocity, starburstID, XerocBoss.StarburstDamage, 0f, -1);
                }
            }
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            Texture2D noise = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/FireNoise").Value;
            Texture2D noise2 = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise").Value;

            float colorInterpolant = GetLerpValue(3000f, 32000f, Temperature, true);
            Color starColor = MulticolorLerp(colorInterpolant, Color.Red, Color.Orange, Color.Yellow);
            starColor = Color.Lerp(starColor, Color.IndianRed, 0.32f);

            var fireballShader = ShaderManager.GetShader("FireballShader");
            fireballShader.TrySetParameter("mainColor", starColor.ToVector3() * Projectile.Opacity);
            fireballShader.TrySetParameter("resolution", new Vector2(100f, 100f));
            fireballShader.TrySetParameter("speed", 0.76f);
            fireballShader.TrySetParameter("zoom", 0.0004f);
            fireballShader.TrySetParameter("dist", 60f);
            fireballShader.TrySetParameter("opacity", Projectile.Opacity);
            fireballShader.SetTexture(noise, 1);
            fireballShader.SetTexture(noise2, 2);
            fireballShader.Apply();

            spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, pixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.3f, SpriteEffects.None, 0f);

            fireballShader.TrySetParameter("mainColor", Color.Wheat.ToVector3() * Projectile.Opacity * 0.6f);
            fireballShader.Apply();
            spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, pixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.08f, SpriteEffects.None, 0f);
        }
    }
}
