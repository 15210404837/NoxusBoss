using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class LocalScreenSplitBurnAfterimageSystem : ModSystem
    {
        private static bool takeSnapshotNextFrame;

        public static bool EffectIsActive => NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.01f;

        public static int BurnTimer
        {
            get;
            private set;
        }

        public static int BurnLifetime
        {
            get;
            private set;
        }

        public static ManagedRenderTarget BurnTarget
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            Main.OnPreDraw += PrepareBurnSnapshotShader;
            Main.OnPostDraw += DrawBurnEffect;
            Main.QueueMainThreadAction(() => BurnTarget = new(true, RenderTargetManager.CreateScreenSizedTarget));
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PrepareBurnSnapshotShader;
        }

        private void PrepareBurnSnapshotShader(GameTime obj)
        {
            if (!takeSnapshotNextFrame || !EffectIsActive)
                return;

            var gd = Main.instance.GraphicsDevice;

            // Prepare for drawing to the burn target.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            gd.SetRenderTarget(BurnTarget);
            gd.Clear(Color.Transparent);

            // Draw the contents of the screen split to the burn target.
            LocalScreenSplitShaderData.PrepareShaderParameters(BurnNoise);
            Filters.Scene["NoxusBoss:LocalScreenSplit"].GetShader().Shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(InvisiblePixel, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, new Vector2(Main.screenWidth, Main.screenHeight), 0, 0f);

            // Return to the backbuffer.
            Main.spriteBatch.End();
            gd.SetRenderTarget(null);

            takeSnapshotNextFrame = false;
        }

        private void DrawBurnEffect(GameTime obj)
        {
            if (BurnTimer >= BurnLifetime || !EffectIsActive)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);

            float configurationOpacity = GetLerpValue(0.4f, 0.01f, NoxusBossConfig.Instance.VisualOverlayIntensity, true);
            float opacityFadeIn = GetLerpValue(0f, 9f, BurnTimer, true);
            float opacityFadeOut = GetLerpValue(BurnLifetime, BurnLifetime * 0.65f, BurnTimer, true);
            float opacity = opacityFadeIn * opacityFadeOut * configurationOpacity * 0.132f;
            Main.spriteBatch.Draw(BurnTarget, Vector2.Zero, Color.RosyBrown * opacity);
            Main.spriteBatch.Draw(BurnTarget, Vector2.Zero, Color.Orange with { A = 0 } * opacity * 0.3f);
            Main.spriteBatch.End();
        }

        public override void PostUpdateEverything()
        {
            BurnTimer = Clamp(BurnTimer + 1, 0, BurnLifetime);
        }

        public static void TakeSnapshot(int burnLifetime)
        {
            takeSnapshotNextFrame = true;
            BurnTimer = 0;
            BurnLifetime = burnLifetime;
        }
    }
}
