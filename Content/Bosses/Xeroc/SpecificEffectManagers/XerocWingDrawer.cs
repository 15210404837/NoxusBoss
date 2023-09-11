using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria;
using CalamityMod;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Automators;

namespace NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers
{
    public class XerocWingDrawer : ModSystem
    {
        private static ManagedRenderTarget AfterimageTarget
        {
            get;
            set;
        }

        public static ManagedRenderTarget AfterimageTargetPrevious
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            Main.OnPreDraw += PrepareAfterimageTarget;
            Main.QueueMainThreadAction(() =>
            {
                AfterimageTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
                AfterimageTargetPrevious = new(true, RenderTargetManager.CreateScreenSizedTarget);
            });
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PrepareAfterimageTarget;
        }

        private void PrepareAfterimageTarget(GameTime obj)
        {
            // Don't waste resources if Xeroc is not present.
            if (XerocBoss.Myself is null)
                return;

            var gd = Main.instance.GraphicsDevice;

            // Prepare the render target for drawing.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            gd.SetRenderTarget(AfterimageTarget.Target);
            gd.Clear(Color.Transparent);

            // Draw the contents of the previous frame to the target.
            Main.spriteBatch.Draw(AfterimageTargetPrevious.Target, Vector2.Zero, Color.White);

            // Draw Xeroc's wings.
            int afterimageCount = (int)XerocBoss.Myself.ModNPC<XerocBoss>().AfterimageCount;
            XerocBoss.Myself.ModNPC<XerocBoss>().DrawWings(Vector2.Zero, 1f);
            for (int i = afterimageCount; i >= 1; i--)
            {
                Vector2 drawOffset = XerocBoss.Myself.oldPos[i] - XerocBoss.Myself.position;
                XerocBoss.Myself.ModNPC<XerocBoss>().DrawWings(drawOffset, Pow(1f - i / (float)afterimageCount, 3f));
            }

            // Draw the afterimage shader to the result.
            ApplyPsychedelicDiffusionEffects();

            // Return to the backbuffer.
            Main.spriteBatch.End();
            gd.SetRenderTarget(null);
        }

        public static void ApplyPsychedelicDiffusionEffects()
        {
            var gd = Main.instance.GraphicsDevice;
            gd.SetRenderTarget(AfterimageTargetPrevious.Target);
            gd.Clear(Color.Transparent);

            // Prepare the afterimage psychedelic shader.
            var afterimageShader = ShaderManager.GetShader("XerocPsychedelicAfterimageShader");
            afterimageShader.TrySetParameter("uScreenResolution", new Vector2(Main.screenWidth, Main.screenHeight));
            afterimageShader.TrySetParameter("warpSpeed", 0.0008f);
            afterimageShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"), 1);
            afterimageShader.Apply();

            Main.spriteBatch.Draw(AfterimageTarget.Target, Vector2.Zero, Color.White);
        }
    }
}
