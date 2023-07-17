using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics;
using Terraria.ModLoader;
using Terraria;
using CalamityMod;
using Terraria.Graphics.Shaders;

namespace NoxusBoss.Content.Bosses.Xeroc
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

        public static ManagedRenderTarget BlurBuffer
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
            XerocBoss.Myself.ModNPC<XerocBoss>().DrawWings();

            // Draw the afterimage to the intermediate buffer with the wing shader.
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
            var afterimageShader = GameShaders.Misc["NoxusBoss:XerocPsychedelicAfterimageShader"];
            afterimageShader.Shader.Parameters["uScreenResolution"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            afterimageShader.SetShaderTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"));
            afterimageShader.Apply();

            Main.spriteBatch.Draw(AfterimageTarget.Target, Vector2.Zero, Color.White);
        }
    }
}
