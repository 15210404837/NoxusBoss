using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class LocalScreenSplitShaderData : ScreenShaderData
    {
        public LocalScreenSplitShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public static void PrepareShaderParameters(Texture2D overlayTexture)
        {
            var shader = Filters.Scene["NoxusBoss:LocalScreenSplit"].GetShader().Shader;

            // Calculate the source positions of the split in UV coordinates.
            Vector2[] splitCenters = new Vector2[LocalScreenSplitSystem.MaxSplitCount];
            for (int i = 0; i < splitCenters.Length; i++)
                splitCenters[i] = WorldSpaceToScreenUV(LocalScreenSplitSystem.SplitCenters[i]);

            shader.Parameters["splitCenters"].SetValue(splitCenters);
            shader.Parameters["splitDirections"].SetValue(LocalScreenSplitSystem.SplitAngles.Select(a => a.ToRotationVector2().RotatedBy(PiOver2)).ToArray());
            shader.Parameters["splitWidths"].SetValue(LocalScreenSplitSystem.SplitWidths.Select(a => a / Main.screenWidth).ToArray());
            shader.Parameters["splitSlopes"].SetValue(LocalScreenSplitSystem.SplitSlopes);
            shader.Parameters["activeSplits"].SetValue(LocalScreenSplitSystem.SplitCompletionRatios.Select(a => a > 0f && a < 1f).ToArray());
            shader.Parameters["offsetsAreAllowed"].SetValue(CalamityConfig.Instance.Screenshake);
            shader.Parameters["splitBrightnessFactor"].SetValue(1.3f + Pow(CalamityUtils.Convert01To010(LocalScreenSplitSystem.SplitCompletionRatios.Average()), 2.5f) * 2.2f);
            shader.Parameters["splitTextureZoomFactor"].SetValue(0.75f);

            Main.instance.GraphicsDevice.Textures[1] = overlayTexture;
        }

        public override void Apply()
        {
            var overlay = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/DivineLight").Value;
            if (LocalScreenSplitSystem.UseCosmicEffect)
                overlay = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Cosmos").Value;

            PrepareShaderParameters(overlay);
            base.Apply();
        }
    }
}
