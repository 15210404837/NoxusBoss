using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;

namespace NoxusBoss.Content.Bosses
{
    public class NoxusScreenShaderData : ScreenShaderData
    {
        public NoxusScreenShaderData(string passName)
            : base(passName)
        {
        }

        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(Color.Transparent);
            base.Apply();
        }
    }
}
