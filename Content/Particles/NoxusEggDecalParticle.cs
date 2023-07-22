using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class NoxusEggDecalParticle : Particle
    {
        public float Opacity = 1f;

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => false;

        public override bool Important => true;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public NoxusEggDecalParticle(Vector2 position, float rotation, Color color, int lifetime, float scale)
        {
            Position = position;
            Rotation = rotation;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Opacity = 1f - LifetimeCompletion;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Color c = Color * Opacity;
            float contrastInterpolant = GetLerpValue(0.6f, 0.8f, Opacity, true);

            var backgroundShader = ShaderManager.GetShader("MonochromeShader");
            backgroundShader.TrySetParameter("contrastInterpolant", contrastInterpolant);
            backgroundShader.Apply();

            Texture2D texture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Noxus/NoxusEgg").Value;
            Main.spriteBatch.Draw(texture, Position - Main.screenPosition, null, c, Rotation, texture.Size() * 0.5f, Scale, 0, 0f);
            spriteBatch.ExitShaderRegion();
        }
    }
}
