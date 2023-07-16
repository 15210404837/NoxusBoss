using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics;
using Terraria.ModLoader;
using Terraria;
using System.Collections.Generic;
using System.Linq;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class XerocRobePatternGenerator : ModSystem
    {
        public static ManagedRenderTarget PatternTarget
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            Main.OnPreDraw += PreparePatternTarget;
            Main.QueueMainThreadAction(() => PatternTarget = new(false, (_, _2) =>
            {
                return new(Main.instance.GraphicsDevice, 512, 512, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.DiscardContents);
            }));
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PreparePatternTarget;
        }

        private void PreparePatternTarget(GameTime obj)
        {
            var gd = Main.instance.GraphicsDevice;

            // Prepare the render target for drawing.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            gd.SetRenderTarget(PatternTarget.Target);
            gd.Clear(Color.DarkBlue);

            // Draw the eyes and background to the pattern target.
            DrawBackground();
            DrawEyes();

            // Return to the backbuffer.
            Main.spriteBatch.End();
            gd.SetRenderTarget(null);
        }

        public static void DrawBackground()
        {
            Texture2D cosmicTexture = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Cosmos").Value;
            Main.spriteBatch.Draw(cosmicTexture, Vector2.Zero, null, new(14, 14, 14), 0f, Vector2.Zero, 5f, 0, 0f);
        }

        public static void DrawEyes()
        {
            int eyeCount = 39;
            ulong eyeSeed = 8135uL;
            List<Vector2> existingEyePositions = new();

            for (int i = 0; i < eyeCount; i++)
            {
                float x = Lerp(54f, PatternTarget.Target.Width - 54f, RandomFloat(ref eyeSeed)) * 5f;
                float y = Lerp(54f, PatternTarget.Target.Height - 54f, RandomFloat(ref eyeSeed)) * 5f;
                Vector2 eyeDrawPosition = new(x, y);

                // Calculate random information about the eyes, such as their rotation, variant, and scale.
                int eyeVariant = RandomInt(ref eyeSeed, 3);
                int eyeFrameOffset = RandomInt(ref eyeSeed, 20);
                float eyeRotation = RandomFloat(ref eyeSeed) * TwoPi;
                float eyeScale = Lerp(1.5f, 4.1f, RandomFloat(ref eyeSeed));

                if (existingEyePositions.Any(e => eyeDrawPosition.WithinRange(e, 350f)))
                    continue;

                // Calculate texture and frame information from the aforementioned stuff.
                int eyeFrameCount;
                Texture2D eyeTexture;
                switch (eyeVariant)
                {
                    case 0:
                        eyeFrameCount = 30;
                        eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/EyeAnimation1").Value;
                        break;
                    case 1:
                        eyeFrameCount = 44;
                        eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/EyeAnimation2").Value;
                        break;
                    case 2:
                    default:
                        eyeFrameCount = 24;
                        eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/EyeAnimation3").Value;
                        break;
                }

                // Draw the eye.
                Rectangle eyeFrame = eyeTexture.Frame(1, eyeFrameCount, 0, (int)(Main.GlobalTimeWrappedHourly * 15f + eyeFrameOffset) % eyeFrameCount);
                Main.spriteBatch.Draw(eyeTexture, eyeDrawPosition, eyeFrame, Color.White with { A = 20 }, eyeRotation, eyeFrame.Size() * 0.5f, eyeScale, 0, 0f);

                // Store the eye position in a list so that other eyes know to avoid getting too close to it and causing overlap.
                existingEyePositions.Add(eyeDrawPosition);
            }
        }
    }
}
