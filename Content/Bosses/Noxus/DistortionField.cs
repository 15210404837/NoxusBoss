using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Noxus
{
    public class DistortionField : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/HollowCircleSoftEdge";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Distortion Field");

        public override void SetDefaults()
        {
            Projectile.width = 360;
            Projectile.height = 360;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Handle fade effects.
            Projectile.Opacity = GetLerpValue(0f, 10f, Time, true) * GetLerpValue(0f, 90f, Projectile.timeLeft, true);

            // Emit a bunch of gas.
            float fogOpacity = Projectile.Opacity * 0.6f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 fogVelocity = Main.rand.NextVector2Circular(36f, 36f) * Projectile.Opacity;
                HeavySmokeParticle fog = new(Projectile.Center, fogVelocity, NoxusSky.FogColor, 50, 3f, fogOpacity, 0f, true);
                GeneralParticleHandler.SpawnParticle(fog);
            }

            Time++;

            if (Projectile.timeLeft <= 60)
                Projectile.damage = 0;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), EntropicGod.DebuffDuration_RegularAttack);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.SetBlendState(Main.spriteBatch, BlendState.Additive);

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = Projectile.Size / texture.Size() * 1.4f;
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(Color.MediumPurple), Projectile.rotation, texture.Size() * 0.5f, scale, 0, 0f);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
