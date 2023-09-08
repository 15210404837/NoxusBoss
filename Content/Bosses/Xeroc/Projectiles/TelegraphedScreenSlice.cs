using CalamityMod;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class TelegraphedScreenSlice : ModProjectile, IDrawAdditive
    {
        public ref float TelegraphTime => ref Projectile.ai[0];

        public ref float LineLength => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public static int SliceTime => 10;

        public int ShotProjectileTelegraphTime => (int)Clamp(TelegraphTime * 2f - 14f, 10f, 60f);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20000;

        public override void SetDefaults()
        {
            Projectile.width = 105;
            Projectile.height = 105;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
        }

        public override void AI()
        {
            // Decide the rotation of the line.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Define the universal opacity.
            Projectile.Opacity = GetLerpValue(TelegraphTime + SliceTime - 1f, TelegraphTime + SliceTime - 12f, Time, true);

            if (Time >= TelegraphTime + SliceTime)
                Projectile.Kill();

            // Split the screen and create daggers if the telegraph is over.
            if (Time == TelegraphTime - 1f)
            {
                bool clockExists = AnyProjectiles(ModContent.ProjectileType<ClockConstellation>());
                bool cosmicLaserExists = AnyProjectiles(ModContent.ProjectileType<SuperCosmicBeam>());
                float sliceWidth = Projectile.width * (clockExists ? 1f : 0.5f) * (cosmicLaserExists ? 0.4f : 1f);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower += 2f;
                LocalScreenSplitSystem.Start(Projectile.Center + Projectile.velocity * LineLength * 0.5f, SliceTime * 2 + 3, Projectile.rotation, sliceWidth);

                if (clockExists)
                    RadialScreenShoveSystem.Start(Projectile.Center + Projectile.velocity * LineLength * 0.5f, 60);

                // Release the daggers.
                if (clockExists)
                    SoundEngine.PlaySound(CommonCalamitySounds.LargeWeaponFireSound with { Volume = 2.75f });
                else
                    SoundEngine.PlaySound(XerocBoss.ExplosionTeleportSound);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float daggerOffset = 48f;
                    float daggerSpacing = 192f;
                    if (clockExists)
                    {
                        daggerSpacing = 160f;
                        daggerOffset = 0f;
                    }
                    if (cosmicLaserExists)
                    {
                        daggerSpacing = 230f;
                        daggerOffset = 0f;
                    }

                    for (float d = daggerOffset; d < LineLength; d += daggerSpacing)
                    {
                        float hueInterpolant = d / LineLength * 2f % 1f;
                        Vector2 daggerStartingVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2) * 16f;
                        Vector2 left = Projectile.Center + Projectile.velocity * d - daggerStartingVelocity * 3f;
                        Vector2 right = Projectile.Center + Projectile.velocity * d + daggerStartingVelocity * 3f;

                        NewProjectileBetter(left, daggerStartingVelocity, ModContent.ProjectileType<LightDagger>(), XerocBoss.DaggerDamage, 0f, -1, ShotProjectileTelegraphTime, hueInterpolant);
                        NewProjectileBetter(right, -daggerStartingVelocity, ModContent.ProjectileType<LightDagger>(), XerocBoss.DaggerDamage, 0f, -1, ShotProjectileTelegraphTime, hueInterpolant);
                    }
                }
            }

            if (Time >= TelegraphTime + SliceTime)
                Projectile.Kill();

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= TelegraphTime)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * LineLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            if (XerocBoss.Myself is not null && XerocBoss.Myself.ModNPC<XerocBoss>().CurrentAttack == XerocBoss.XerocAttackType.LightBeamTransformation)
                return;

            // Create a telegraph.
            if (Time <= TelegraphTime)
            {
                float telegraphInterpolant = GetLerpValue(0f, TelegraphTime - 4f, Time, true);
                Color telegraphColor = Color.Lerp(Color.IndianRed, Color.White, Pow(telegraphInterpolant, 0.6f)) * telegraphInterpolant;
                spriteBatch.DrawBloomLine(Projectile.Center, Projectile.Center + Projectile.velocity * LineLength, telegraphColor, Projectile.width * telegraphInterpolant * 2f);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
