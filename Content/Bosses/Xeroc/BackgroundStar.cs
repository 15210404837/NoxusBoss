using System.IO;
using CalamityMod;
using CalamityMod.Particles;
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
    public class BackgroundStar : ModProjectile, IDrawPixelatedPrims
    {
        public PrimitiveTrail TrailDrawer
        {
            get;
            private set;
        }

        public ref float ZPosition => ref Projectile.ai[0];

        public ref float Index => ref Projectile.ai[1];

        public bool ApproachingScreen
        {
            get;
            set;
        }

        public ref float Time => ref Projectile.localAI[0];

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainbowRodBullet}";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Star");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 13;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = Projectile.MaxUpdates * 360;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(ApproachingScreen);

        public override void ReceiveExtraAI(BinaryReader reader) => ApproachingScreen = reader.ReadBoolean();

        public override void AI()
        {
            // Determine the scale of the star based on its Z position.
            Projectile.scale = 2f / (ZPosition + 1f);

            // Create fire on the first frame.
            if (Projectile.localAI[1] == 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 fireVelocity = Main.rand.NextVector2Circular(4f, 4f);
                    Color fireColor = Color.Lerp(Color.Cyan, Color.Wheat, Main.rand.NextFloat(0.75f)) * 0.4f;
                    HeavySmokeParticle fire = new(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), fireVelocity, fireColor, 18, 0.9f, 1f, 0f, true);
                    GeneralParticleHandler.SpawnParticle(fire);
                }
                Projectile.localAI[1] = 1f;
            }

            // Get close to the screen if instructed to do so.
            if (ApproachingScreen)
            {
                ZPosition = Lerp(ZPosition, -0.93f, 0.13f);

                Vector2 targetPosition = Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center;
                if (Projectile.velocity == Vector2.Zero)
                {
                    Projectile.velocity = (targetPosition - Projectile.Center) * 0.011f;
                    Projectile.netUpdate = true;
                }

                Projectile.velocity *= 1.014f;

                if (ZPosition <= -0.92f)
                {
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = 7f;
                    SoundEngine.PlaySound(XerocBoss.SupernovaSound, Projectile.Center);
                    SoundEngine.PlaySound(XerocBoss.ExplosionTeleportSound, Projectile.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int starIndex = NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ExplodingStar>(), XerocBoss.StarDamage, 0f, -1, 1.089f);
                        if (starIndex >= 0 && starIndex < Main.maxProjectiles)
                            Main.projectile[starIndex].ModProjectile<ExplodingStar>().Time = 18f;
                    }

                    Projectile.Kill();
                }
            }

            // Fade in based on how long the starburst has existed.
            Projectile.Opacity = GetLerpValue(0f, 12f, Time, true);

            // Make the opacity weaker depending on how close the star is to the background.
            Projectile.Opacity *= Remap(ZPosition, 0.8f, 3.4f, 1f, 0.5f);

            Time++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(25f, 5f, completionRatio) * Pow(Projectile.scale, 0.6f) * Projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            // Make the trail fade out at the end and fade in shparly at the start, to prevent the trail having a definitive, flat "start".
            float trailOpacity = GetLerpValue(0.75f, 0.27f, completionRatio, true) * GetLerpValue(0f, 0.067f, completionRatio, true) * 0.9f;

            // Interpolate between a bunch of colors based on the completion ratio.
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.25f);
            Color middleColor = Color.Lerp(Color.SkyBlue, Color.Yellow, 0.4f);
            Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;

            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public void DrawBloomFlare()
        {
            Texture2D bloomFlare = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare").Value;
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;
            float scale = Pow(Projectile.scale, 0.77f);

            float colorInterpolant = Projectile.identity / 13f % 1f;
            Color bloomFlareColor1 = Color.Lerp(Color.SkyBlue, Color.Orange, colorInterpolant % 0.6f) with { A = 0 } * Projectile.Opacity * 0.54f;
            Color bloomFlareColor2 = Color.Lerp(Color.Cyan, Color.White, colorInterpolant % 0.6f) with { A = 0 } * Projectile.Opacity * 0.81f;

            Vector2 bloomFlareDrawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(bloomFlare, bloomFlareDrawPosition, null, bloomFlareColor1, bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.13f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, bloomFlareDrawPosition, null, bloomFlareColor2, -bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.146f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);

            // Draw a bloom flare behind the starburst.
            DrawBloomFlare();

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Projectile.GetAlpha(Color.Wheat) with { A = 0 };
            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, Sqrt(Projectile.scale), 0, 0f);
            return false;
        }

        public void Draw()
        {
            TrailDrawer ??= new(FlameTrailWidthFunction, FlameTrailColorFunction, null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (ZPosition >= 0f)
                return;

            // Draw a flame trail.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/TrailStreaks/StreakMagma"));
            TrailDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 11);
        }
    }
}
