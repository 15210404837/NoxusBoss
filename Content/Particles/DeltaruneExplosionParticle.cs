using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class DeltaruneExplosionParticle : Particle
    {
        public override int FrameVariants => 16;

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => false;

        public override string Texture => "NoxusBoss/Content/Particles/DeltaruneExplosionParticle";

        public DeltaruneExplosionParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Variant = (int)(LifetimeCompletion * FrameVariants);
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, FrameVariants, 0, Variant);
            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color, Rotation, frame.Size() * 0.5f, Scale, 0, 0f);
        }
    }
}
