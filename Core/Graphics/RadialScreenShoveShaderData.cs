using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace NoxusBoss.Core.Graphics
{
    public class RadialScreenShoveShaderData : ScreenShaderData
    {
        public RadialScreenShoveShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Apply()
        {
            Shader.Parameters["blurPower"].SetValue(NoxusBossConfig.Instance.VisualOverlayIntensity * 0.5f);
            Shader.Parameters["pulseTimer"].SetValue(Main.GlobalTimeWrappedHourly * 16f);
            Shader.Parameters["distortionPower"].SetValue(RadialScreenShoveSystem.DistortionPower * NoxusBossConfig.Instance.VisualOverlayIntensity * 0.08f);
            Shader.Parameters["distortionCenter"].SetValue(WorldSpaceToScreenUV(RadialScreenShoveSystem.DistortionCenter));
            base.Apply();
        }
    }
}
