using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class XerocScreenShaderData : ScreenShaderData
    {
        public XerocScreenShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Apply()
        {
            Main.instance.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/DivineLight").Value;
            Shader.Parameters["seamAngle"].SetValue(XerocSky.SeamAngle);
            Shader.Parameters["seamSlope"].SetValue(XerocSky.SeamSlope);
            Shader.Parameters["seamBrightness"].SetValue(0.029f);
            Shader.Parameters["warpIntensity"].SetValue(0.016f);
            Shader.Parameters["offsetsAreAllowed"].SetValue(XerocSky.HeavenlyBackgroundIntensity <= 0.01f);
            UseOpacity(1f - XerocSky.HeavenlyBackgroundIntensity + 0.001f);
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(Color.Transparent);
            UseIntensity(XerocSky.SeamScale * GetLerpValue(0.5f, 0.1f, XerocSky.HeavenlyBackgroundIntensity, true));
            base.Apply();
        }
    }
}
