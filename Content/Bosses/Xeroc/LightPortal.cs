using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Noxus;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class LightPortal : ModProjectile, IDrawsWithShader
    {
        public float MaxScale => Projectile.ai[0];

        public ref float Time => ref Projectile.localAI[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Light Portal");

        public override void SetDefaults()
        {
            Projectile.width = 600;
            Projectile.height = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9600;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            // Decide the current scale.
            Projectile.scale = GetLerpValue(0f, 25f, Time, true) * GetLerpValue(Lifetime, Lifetime - 16f, Time, true);
            Projectile.Opacity = Pow(Projectile.scale, 2.6f);
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Time >= Lifetime)
            {
                SoundEngine.PlaySound(EntropicGod.TwinkleSound with { Volume = 0.3f, MaxInstances = 20 }, Projectile.Center);
                TwinkleParticle twinkle = new(Projectile.Center, Vector2.Zero, Color.LightCyan, 30, 6, Vector2.One * MaxScale * 1.3f);
                GeneralParticleHandler.SpawnParticle(twinkle);
                Projectile.Kill();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var gd = Main.instance.GraphicsDevice;
            var portalShader = GameShaders.Misc[$"{Mod.Name}:PortalShader"];
            portalShader.Shader.Parameters["circleStretchInterpolant"].SetValue(Projectile.scale);
            portalShader.Shader.Parameters["transformation"].SetValue(Matrix.CreateScale(3f, 1f, 1f));
            portalShader.Shader.Parameters["aimDirection"].SetValue(Projectile.velocity);
            portalShader.Shader.Parameters["edgeFadeInSharpness"].SetValue(20.3f);
            portalShader.Shader.Parameters["aheadCircleMoveBackFactor"].SetValue(0.67f);
            portalShader.Shader.Parameters["aheadCircleZoomFactor"].SetValue(0.9f);
            portalShader.Shader.Parameters["spaceBrightness"].SetValue(1.5f);
            portalShader.Apply();

            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Pixel").Value;
            Vector2 textureArea = Projectile.Size / pixel.Size() * MaxScale;
            textureArea *= 1f + Cos(Main.GlobalTimeWrappedHourly * 15f + Projectile.identity) * 0.012f;

            gd.Textures[1] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/StarDistanceLookup").Value;
            gd.Textures[2] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise").Value;
            gd.Textures[3] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/DivineLight").Value;
            gd.Textures[4] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;
            gd.Textures[5] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/Spikes").Value;

            spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.IndianRed), Projectile.rotation, pixel.Size() * 0.5f, textureArea, 0, 0f);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
