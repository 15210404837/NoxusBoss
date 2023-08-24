using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class LightDagger : ModProjectile, IDrawAdditive
    {
        public float DaggerAppearInterpolant => GetLerpValue(TelegraphTime - 16f, TelegraphTime - 3f, Time, true);

        public Color GeneralColor => Color.Lerp(LocalScreenSplitSystem.UseCosmicEffect ? Color.Wheat : Color.IndianRed, Color.White, HueInterpolant) * Projectile.Opacity;

        public ref float Time => ref Projectile.localAI[0];

        public ref float TelegraphTime => ref Projectile.ai[0];

        public ref float HueInterpolant => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 120;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Time);

        public override void ReceiveExtraAI(BinaryReader reader) => Time = reader.ReadSingle();

        public override void AI()
        {
            // Sharply fade in.
            Projectile.Opacity = GetLerpValue(0f, 12f, Time, true);

            // Decide rotation based on direction.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Accelerate after the telegraph dissipates.
            if (Time >= TelegraphTime)
            {
                float newSpeed = Clamp(Projectile.velocity.Length() + 5f, 14f, 90f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
            }

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            if (Time <= TelegraphTime)
                DrawTelegraph();

            // Draw bloom underneath the dagger. This is strongest when the blade itself has not yet fully faded in.
            float bloomOpacity = Lerp(1f, 0.6f, DaggerAppearInterpolant) * Projectile.Opacity;

            Color c1 = Color.IndianRed;
            Color c2 = Color.LightCoral;
            if (LocalScreenSplitSystem.UseCosmicEffect)
            {
                c1 = Color.DeepSkyBlue;
                c2 = Color.White;
            }

            Color mainColor = Color.Lerp(c1, c2, Sin01(TwoPi * HueInterpolant + Main.GlobalTimeWrappedHourly * 2f)) * bloomOpacity;
            Color secondaryColor = Color.Lerp(c1, c2, Sin01(TwoPi * (1f - HueInterpolant) + Main.GlobalTimeWrappedHourly * 2f)) * bloomOpacity;

            Main.EntitySpriteDraw(bloom, Projectile.oldPos[1] + Projectile.Size * 0.5f - Main.screenPosition, null, mainColor, 0f, bloom.Size() * 0.5f, Projectile.scale * 1.32f, 0, 0);
            Main.EntitySpriteDraw(bloom, Projectile.oldPos[2] + Projectile.Size * 0.5f - Main.screenPosition, null, secondaryColor, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.34f, 0, 0);

            // Make the dagger appear near the end of the telegraph fade-in.
            float daggerOffsetFactor = Projectile.velocity.Length() * 0.2f;
            for (int i = 0; i < 30; i++)
            {
                float daggerScale = Lerp(1f, 0.48f, i / 29f) * Projectile.scale;
                Vector2 daggerDrawOffset = Projectile.velocity.SafeNormalize(Vector2.UnitY) * DaggerAppearInterpolant * i * daggerScale * -daggerOffsetFactor;
                Color daggerDrawColor = c2 * DaggerAppearInterpolant * Pow(1f - i / 10f, 1.6f) * Projectile.Opacity;
                Main.EntitySpriteDraw(texture, Projectile.Center + daggerDrawOffset - Main.screenPosition, null, daggerDrawColor, Projectile.rotation, texture.Size() * 0.5f, daggerScale, 0, 0);
            }
        }

        public void DrawTelegraph()
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 2100f;
            Main.spriteBatch.DrawBloomLine(start, end, GeneralColor * Sqrt(1f - DaggerAppearInterpolant), Projectile.Opacity * 40f);
        }

        public override bool? CanDamage() => Time >= TelegraphTime;

        public override bool ShouldUpdatePosition() => Time >= TelegraphTime;
    }
}
