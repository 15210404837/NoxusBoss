using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles
{
    public class WindStreakVisual : ModProjectile, IDrawPixelatedPrims
    {
        public PrimitiveTrail WindStreakDrawer;

        public ref float Time => ref Projectile.ai[0];

        public ref float LoopInterpolant => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Fade in and out based on time.
            Projectile.Opacity = GetLerpValue(0f, 32f, Time, true) * GetLerpValue(0f, 60f, Projectile.timeLeft, true) * 0.09f;

            // Ensure that the wind quickly fades out if Xeroc is present or if the wind is inside of tiles.
            bool insideTile = Collision.SolidCollision(Projectile.Center, 1, 1);
            if (XerocBoss.Myself is not null || insideTile)
            {
                if (Projectile.timeLeft > 60)
                    Projectile.timeLeft = 60;
            }

            if (insideTile)
                Projectile.Opacity *= 0.5f;

            // Move in a mostly flat horizontal streak, occasionally doing loops.
            float loopSpeed = Lerp(4.4f, 8.5f, Projectile.identity / 16f % 1f);
            float loopAnimationUpdateRate = Lerp(0.016f, 0.031f, Projectile.identity / 11f % 1f);
            float movementSpeed = Projectile.velocity.Length();
            float movementDirection = Sign(Projectile.velocity.X);
            Vector2 horizontalVelocity = Vector2.UnitX * movementSpeed * movementDirection;
            Vector2 baseVelocity = horizontalVelocity + Vector2.UnitY * (Sin(Projectile.Center.X / 132f + Projectile.identity) * 2f - 1.2f);
            Vector2 loopVelocity = (LoopInterpolant * TwoPi).ToRotationVector2() * new Vector2(1f, 0.6f) * loopSpeed;

            if (LoopInterpolant <= 0f)
            {
                Projectile.velocity = baseVelocity;

                // Randomly loop around.
                if (Time >= 32f && Main.rand.NextBool(240))
                {
                    LoopInterpolant = 0.001f;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, loopVelocity, 0.15f);
                LoopInterpolant += loopAnimationUpdateRate;
                if (LoopInterpolant >= 1f)
                {
                    LoopInterpolant = 0f;
                    Projectile.netUpdate = true;
                }
            }

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(Color.Wheat, Color.IndianRed, Projectile.identity / 12f % 1f * 0.56f);
        }

        public static float WindWidthFunction(float completionRatio) => 7f - (1f - completionRatio) * 3f;

        public Color WindColorFunction(float completionRatio) => Projectile.GetAlpha(Color.White) with { A = 0 } * (1f - completionRatio) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor) => false;

        public void Draw()
        {
            var streakShader = GameShaders.Misc["CalamityMod:TrailStreak"];
            streakShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"));

            WindStreakDrawer ??= new(WindWidthFunction, WindColorFunction, null, streakShader);
            WindStreakDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 11);
        }
    }
}
