using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class LocalScreenSplitBurnAfterimageSystem : ModSystem
    {
        private static bool takeSnapshotNextFrame;

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
            if (!takeSnapshotNextFrame)
                return;

            var gd = Main.instance.GraphicsDevice;

            // Draw the contents of the screen split to the burn target.
            Texture2D invisible = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            gd.SetRenderTarget(BurnTarget.Target);
            gd.Clear(Color.Transparent);

            LocalScreenSplitShaderData.PrepareShaderParameters(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/BurnNoise").Value);
            Filters.Scene["NoxusBoss:LocalScreenSplit"].GetShader().Shader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(invisible, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, new Vector2(Main.screenWidth, Main.screenHeight), 0, 0f);

            // Return to the backbuffer.
            Main.spriteBatch.End();
            gd.SetRenderTarget(null);

            takeSnapshotNextFrame = false;
        }

        private void DrawBurnEffect(GameTime obj)
        {
            if (BurnTimer >= BurnLifetime)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);

            float opacity = GetLerpValue(BurnLifetime, BurnLifetime * 0.65f, BurnTimer, true) * GetLerpValue(0f, 9f, BurnTimer, true) * 0.132f;
            Main.spriteBatch.Draw(BurnTarget.Target, Vector2.Zero, Color.RosyBrown * opacity);
            Main.spriteBatch.Draw(BurnTarget.Target, Vector2.Zero, Color.Orange with { A = 0 } * opacity * 0.3f);
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
