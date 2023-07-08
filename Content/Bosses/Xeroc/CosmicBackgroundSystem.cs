using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class CosmicBackgroundSystem : ModSystem
    {
        public static ManagedRenderTarget KalisetFractal
        {
            get;
            internal set;
        }

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(PrepareTarget);
        }

        internal static void PrepareTarget()
        {
            int width = 2048;
            int height = 2048;
            if (Main.gfxQuality >= 0.5f)
            {
                width *= 2;
                height *= 2;
            }

            int iterations = 14;

            // This is stored as a render target and not a PNG in the mod's source because the fractal needs to contain information that exceeds the traditional range of 0-1 color values.
            // It could theoretically be loaded into a binary file in some way but at that point you're going to need to translate it into some GPU-friendly object, like a render target.
            // It's easiest to just create it dynamically here.
            // There are a LOT of calculations needed to generate the entire texture though, hence the usage of background threads.
            KalisetFractal = new(false, (_, _2) =>
            {
                return new(Main.instance.GraphicsDevice, width, height, false, SurfaceFormat.Single, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);
            });

            // This number is highly important to the resulting structure of the fractal, and is very sensitive (as is typically the case with objects from Chaos Theory).
            // Many numbers will give no fractal at all, pure white, or pure black. But tweaking it to be just right gives some amazing patterns.
            // Feel free to tweak this if you want to see what it does to the texture.
            float julia = 0.584f;

            new Thread(_ =>
            {
                float[] kalisetData = new float[width * height];

                // Evolve the system based on the Kaliset.
                // Over time it will achieve very, very chaotic behavior similar to a fractal and as such is incredibly reliable for
                // getting pseudo-random change over time.
                for (int i = 0; i < width * height; i++)
                {
                    int x = i % width;
                    int y = i / width;
                    float previousDistance = 0f;
                    float totalChange = 0f;
                    Vector2 p = new(x / (float)width - 0.5f, y / (float)height - 0.5f);

                    // Repeat the iterative function of 'abs(z) / dot(z) - c' multiple times to generate the fractal patterns.
                    // The higher the amount of iterations, the greater amount of detail. Do note that too much detail can lead to grainy artifacts
                    // due to individual pixels being unusually bright next to their neighbors, as the fractal inevitably becomes so detailed that the
                    // texture cannot encompass all of its features.
                    for (int j = 0; j < iterations; j++)
                    {
                        p = new Vector2(Math.Abs(p.X), Math.Abs(p.Y)) / Vector2.Dot(p, p);
                        p.X -= julia;
                        p.Y -= julia;

                        float distance = p.Length();
                        totalChange += Math.Abs(distance - previousDistance);
                        previousDistance = distance;
                    }

                    // Sometimes the results of the above iterative process will send the distance so far off that the numbers explode into the NaN or Infinity range.
                    // The GPU won't know what to do with this and will just act like it's a black pixel, which we don't want.
                    // As such, this check exists to put a hard limit on the values sent into the fractal texture. Something beyond 1000 shouldn't be making a difference anyway.
                    // At that point the pixel it spits out from the shader should be a pure white.
                    if (float.IsNaN(totalChange) || float.IsInfinity(totalChange) || totalChange >= 1000f)
                        totalChange = 1000f;

                    kalisetData[i] = totalChange;
                }

                Main.QueueMainThreadAction(() => KalisetFractal.Target.SetData(kalisetData));
            }).Start();
        }

        public static void Draw(float intensity)
        {
            if (intensity <= 0f)
                return;

            Vector2 screenArea = new(Main.instance.GraphicsDevice.DisplayMode.Width, Main.instance.GraphicsDevice.DisplayMode.Width);
            Vector2 scale = screenArea / TextureAssets.MagicPixel.Value.Size();

            Main.instance.GraphicsDevice.Textures[1] = KalisetFractal.Target;

            MiscShaderData backgroundShader = GameShaders.Misc["NoxusBoss:CosmicBackgroundShader"];
            backgroundShader.Shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
            backgroundShader.Shader.Parameters["zoom"].SetValue(0.11f);
            backgroundShader.Shader.Parameters["brightness"].SetValue(intensity);
            backgroundShader.Shader.Parameters["scrollSpeedFactor"].SetValue(0.0015f);
            backgroundShader.Shader.Parameters["frontStarColor"].SetValue(Color.Coral.ToVector3() * 0.5f);
            backgroundShader.Shader.Parameters["backStarColor"].SetValue(Color.Yellow.ToVector3() * 0.4f);
            backgroundShader.Apply();

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Vector2.Zero, null, Color.White * 0.25f, 0f, Vector2.Zero, scale, 0, 0f);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}
