using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics.Shaders
{
    public class HighContrastScreenShakeShaderData : ScreenShaderData
    {
        public const string ShaderKey = "NoxusBoss:HighContrast";

        public static float ContrastIntensity
        {
            get;
            set;
        }

        public HighContrastScreenShakeShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public static void ToggleActivityIfNecessary()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            bool shouldBeActive = ContrastIntensity >= 0.01f && NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.01f;
            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Activate(ShaderKey);
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Deactivate(ShaderKey);
        }

        public override void Apply()
        {
            // This results in opposing forces between colors that causes the overall result to shift towards extremes, causing the contrast effect.
            float configIntensityInterpolant = GetLerpValue(0f, 0.45f, NoxusBossConfig.Instance.VisualOverlayIntensity, true);
            float oneOffsetContrast = ContrastIntensity * configIntensityInterpolant + 1f;
            float inverseForce = (1f - oneOffsetContrast) * 0.5f;
            Matrix contrastMatrix = new(
                oneOffsetContrast, 0f, 0f, 0f,
                0f, oneOffsetContrast, 0f, 0f,
                0f, 0f, oneOffsetContrast, 0f,
                inverseForce, inverseForce, inverseForce, 1f);

            Shader.Parameters["contrastMatrix"].SetValue(contrastMatrix);

            base.Apply();
        }
    }
}
