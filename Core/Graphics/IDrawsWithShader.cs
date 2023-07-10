using Microsoft.Xna.Framework.Graphics;

namespace NoxusBoss.Core.Graphics
{
    public interface IDrawsWithShader
    {
        public float LayeringPriority => 0f;

        public bool DrawAdditiveShader => false;

        public void Draw(SpriteBatch spriteBatch);
    }
}
