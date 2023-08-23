using CalamityMod;
using CalamityMod.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Pets
{
    public class BabyXeroc : ModProjectile, IAdditiveDrawer
    {
        public Vector2 LeftHandPosition
        {
            get;
            set;
        }

        public Vector2 RightHandPosition
        {
            get;
            set;
        }

        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
        }

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                LeftHandPosition = RightHandPosition = Projectile.Center;
            }

            CheckActive();
            Projectile.FloatingPetAI(false, 0.007f);

            // Have hands hover near Xeroc.
            Vector2 leftHandDestination = Projectile.Center + new Vector2(-72f, 100f);
            Vector2 rightHandDestination = Projectile.Center + new Vector2(72f, 100f);
            LeftHandPosition = Utils.MoveTowards(Vector2.Lerp(LeftHandPosition, leftHandDestination, 0.05f), leftHandDestination, 15f);
            RightHandPosition = Utils.MoveTowards(Vector2.Lerp(RightHandPosition, rightHandDestination, 0.05f), rightHandDestination, 15f);

            // Fade out based on proximity to the owner.
            Projectile.Opacity = Remap(Projectile.Distance(Owner.Center), 220f, 660f, 1f, 0.18f);
        }

        public void CheckActive()
        {
            // Keep the projectile from disappearing as long as the player isn't dead and has the pet buff.
            if (!Owner.dead && Owner.HasBuff(ModContent.BuffType<BabyXerocBuff>()))
                Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu)
                return false;

            Texture2D handTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Projectiles/Pets/BabyXerocHand").Value;
            Texture2D censorTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocBoss").Value;

            float handRotationOffset = Sin(Main.GlobalTimeWrappedHourly * 2.9f) * 0.35f + PiOver4 + 0.6f;
            float leftHandRotation = (LeftHandPosition - Projectile.Center).ToRotation() + handRotationOffset;
            float rightHandRotation = (RightHandPosition - Projectile.Center).ToRotation() - handRotationOffset;
            if (LeftHandPosition.X < Projectile.Center.X)
                leftHandRotation += Pi;
            if (RightHandPosition.X < Projectile.Center.X)
                rightHandRotation += Pi;

            Vector2 censorDrawPosition = (Projectile.Center / new Vector2(4f, 0.01f)).Floor() * new Vector2(4f, 0.01f) - Main.screenPosition;
            Main.EntitySpriteDraw(censorTexture, censorDrawPosition, null, Projectile.GetAlpha(Color.White), 0f, censorTexture.Size() * 0.5f, Projectile.scale * 0.18f, 0, 0);
            Main.EntitySpriteDraw(handTexture, LeftHandPosition - Main.screenPosition, null, Projectile.GetAlpha(Color.White) * 0.6f, leftHandRotation, handTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.FlipHorizontally, 0);
            Main.EntitySpriteDraw(handTexture, RightHandPosition - Main.screenPosition, null, Projectile.GetAlpha(Color.White) * 0.6f, rightHandRotation, handTexture.Size() * 0.5f, Projectile.scale, 0, 0);
            return false;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D backglow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            // Draw the backglow.
            Vector2 backglowDrawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(backglow, backglowDrawPosition, null, Projectile.GetAlpha(Color.HotPink) * 0.6f, 0f, backglow.Size() * 0.5f, Projectile.scale * 1.1f, 0, 0);
            Main.EntitySpriteDraw(backglow, backglowDrawPosition, null, Projectile.GetAlpha(Color.IndianRed) * 0.4f, 0f, backglow.Size() * 0.5f, Projectile.scale * 2f, 0, 0);

            // Draw the eye trail.
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float afterimageInterpolant = i / (float)(Projectile.oldPos.Length - 1f);
                Color eyeColor = Projectile.GetAlpha(Color.Lerp(Color.Red, Color.White, 1f - afterimageInterpolant)) * (1f - afterimageInterpolant);
                Vector2 eyeDrawPosition = Vector2.Lerp(Projectile.oldPos[i] + Projectile.Size * 0.5f, Projectile.Center, 0.84f) - Main.screenPosition;
                Main.EntitySpriteDraw(texture, eyeDrawPosition, null, eyeColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.8f, 0, 0);
            }
        }
    }
}
