using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class PaleDuckweedParticle : Particle
    {
        public int UniqueID;

        public int Frame;

        public float Direction;

        public float Opacity;

        public override int FrameVariants => 3;

        public override bool SetLifetime => true;

        public override bool UseAdditiveBlend => true;

        public override bool UseCustomDraw => true;

        public override string Texture => "NoxusBoss/Content/Particles/PaleDuckweedParticle";

        public PaleDuckweedParticle(Vector2 position, Vector2 velocity, Color color, int lifetime)
        {
            Frame = Main.rand.Next(FrameVariants);
            Position = position;
            Velocity = velocity;
            Color = color;
            Lifetime = lifetime;
            Direction = Main.rand.NextBool().ToDirectionInt();
            UniqueID = Main.rand.Next(100000);
            Scale = Main.rand.NextFloat(0.45f, 0.7f);
        }

        public override void Update()
        {
            // Fade in and out based on the lifetime of the duckweed.
            Opacity = GetLerpValue(0f, 120f, Time, true) * GetLerpValue(Lifetime, Lifetime - 60f, Time, true);

            // Rise upward in water and bob in place above it.
            if (Collision.WetCollision(Position - Vector2.One * Scale * 6f, (int)(Scale * 12f), (int)(Scale * 12f)))
            {
                if (Collision.SolidCollision(Position + Vector2.UnitX * Direction * 100f, 1, 1))
                    Direction *= -1f;

                Velocity.X = Lerp(Velocity.X, Direction * Lerp(0.3f, 0.7f, UniqueID % 9f / 9f), 0.025f);
                Velocity.Y = Clamp(Velocity.Y - 0.008f, -0.4f, 0.4f);
            }
            else
            {
                Velocity.X *= 0.985f;
                Velocity.Y = Clamp(Velocity.Y + 0.1f, -1f, 5f);
            }

            // Emit a pale light.
            Lighting.AddLight(Position, Color.ToVector3() * 0.33f);
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D backglow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = texture.Frame(1, FrameVariants, 0, Frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects spriteDirection = Direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw a weakly pulsating backglow behind the duckweed.
            float pulse = (Main.GlobalTimeWrappedHourly * 0.5f + UniqueID * 0.21586f) % 1f;
            spriteBatch.Draw(backglow, Position - Main.screenPosition, null, Color * Opacity * 0.32f, Rotation, backglow.Size() * 0.5f, Scale * 0.3f, 0, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * Opacity * (1f - pulse) * 0.61f, Rotation, origin, Scale * (pulse * 1.1f + 1f), spriteDirection, 0f);
            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, origin, Scale, spriteDirection, 0f);
        }
    }
}
