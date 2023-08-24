using Microsoft.Xna.Framework.Graphics;

namespace NoxusBoss.Core.Graphics.Automators
{
    public interface IDrawsWithShader
    {
        public float LayeringPriority => 0f;

        public bool DrawAdditiveShader => false;

        public void Draw(SpriteBatch spriteBatch);
    }
}
