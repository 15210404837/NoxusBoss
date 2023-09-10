﻿using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class LightPortal : ModProjectile, IDrawsWithShader
    {
        public float MaxScale => Projectile.ai[0];

        public ref float Time => ref Projectile.localAI[0];

        public ref float Lifetime => ref Projectile.ai[1];

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
            Projectile.scale = GetLerpValue(0f, 15f, Time, true) * GetLerpValue(Lifetime, Lifetime - 16f, Time, true);
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

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            var portalShader = ShaderManager.GetShader("PortalShader");
            portalShader.TrySetParameter("generalColor", Color.White.ToVector3());
            portalShader.TrySetParameter("circleStretchInterpolant", Projectile.scale);
            portalShader.TrySetParameter("transformation", Matrix.CreateScale(3f, 1f, 1f));
            portalShader.TrySetParameter("aimDirection", Projectile.velocity);
            portalShader.TrySetParameter("edgeFadeInSharpness", 20.3f);
            portalShader.TrySetParameter("aheadCircleMoveBackFactor", 0.67f);
            portalShader.TrySetParameter("aheadCircleZoomFactor", 0.9f);
            portalShader.TrySetParameter("spaceBrightness", GetLerpValue(0.7f, 0.82f, ScreenEffectSystem.FlashIntensity, true) * 150f + 1.5f);
            portalShader.TrySetParameter("spaceTextureZoom", Vector2.One * 0.55f);
            portalShader.TrySetParameter("spaceTextureOffset", Vector2.UnitX * Projectile.identity * 0.156f);
            portalShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/LemniscateDistanceLookup"), 1);
            portalShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"), 2);
            portalShader.SetTexture(ModContent.Request<Texture2D>(Texture), 3);
            portalShader.SetTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"), 4);
            portalShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/Spikes"), 5);
            portalShader.Apply();

            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Pixel").Value;
            Vector2 textureArea = Projectile.Size / pixel.Size() * MaxScale;
            textureArea *= 1f + Cos(Main.GlobalTimeWrappedHourly * 15f + Projectile.identity) * 0.013f;
            spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.IndianRed), Projectile.rotation, pixel.Size() * 0.5f, textureArea, 0, 0f);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
