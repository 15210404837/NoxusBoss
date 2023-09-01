using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Dusts;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.Subworlds.EternalGardenUpdateSystem;

namespace NoxusBoss.Content.Projectiles.Visuals
{
    public class GodRayVisual : ModProjectile, IDrawsWithShader
    {
        public const int Width = 450;

        public const int Height = 11000;

        // Wheat and orange are also pretty cool, but perhaps not the most in line with Xeroc's aesthetic.
        public static Color MainColor => Color.Coral;

        public static Color ColorAccent => Color.Magenta;

        public override string Texture => "NoxusBoss/Assets/ExtraTextures/Pixel";

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3200;

        public override void SetDefaults()
        {
            // Using the Height constant here has a habit of causing vanilla's out-of-world projectile deletion to kill this, due to how large it is.
            Projectile.width = Width;
            if (WasInSubworldLastUpdateFrame)
                Projectile.width = (int)(Projectile.width * 3.2f);

            Projectile.height = 1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900000;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Fade in or out depending on if Xeroc is present.
            bool fadeIn = XerocBoss.Myself is null;
            Projectile.Opacity = Clamp(Projectile.Opacity + fadeIn.ToDirectionInt() * 0.0079f, 0f, 1f);

            if (Projectile.Opacity <= 0f && !fadeIn)
                Projectile.Kill();

            // Emit light.
            Vector2 rayDirection = Vector2.UnitY.RotatedBy(Projectile.rotation);
            DelegateMethods.v3_1 = Color.LightCoral.ToVector3() * 1.4f;
            PlotTileLine(Projectile.Bottom - rayDirection * 2500f, Projectile.Bottom + rayDirection, Projectile.width, DelegateMethods.CastLightOpen_StopForSolids);

            // Decide rotation.
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            // Emit small light dust in the ray.
            Vector2 lightSpawnPosition = Vector2.Zero;

            // Try to keep the light outside of tiles.
            for (int i = 0; i < 15; i++)
            {
                lightSpawnPosition = Projectile.Bottom - rayDirection * Main.rand.NextFloat(160f, 1800f);
                lightSpawnPosition += rayDirection.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * Projectile.width * 0.3f;
                if (!WorldGen.SolidTile(lightSpawnPosition.ToTileCoordinates()))
                    break;
            }

            Dust light = Dust.NewDustPerfect(lightSpawnPosition, ModContent.DustType<FlowerPieceDust>());
            light.color = Color.Lerp(MainColor, ColorAccent, Main.rand.NextFloat(0.5f));
            light.color = Color.Lerp(light.color, Color.Wheat, 0.6f);
            light.velocity = Main.rand.NextVector2Circular(2f, 2f);
            light.scale = 0.75f;
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            // Collect the shader and draw data for later.
            var godRayShader = ShaderManager.GetShader("GodRayShader");
            Texture2D pixel = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 textureArea = new Vector2(Projectile.width, Height) / pixel.Size();

            // Apply the god ray shader.
            godRayShader.TrySetParameter("noise1Zoom", 0.18f);
            godRayShader.TrySetParameter("noise2Zoom", 0.14f);
            godRayShader.TrySetParameter("edgeFadePower", 2f);
            godRayShader.TrySetParameter("edgeTaperDistance", 0.15f);
            godRayShader.TrySetParameter("animationSpeed", 0.1f);
            godRayShader.TrySetParameter("noiseOpacityPower", 2f);
            godRayShader.TrySetParameter("bottomBrightnessIntensity", 0.2f);
            godRayShader.TrySetParameter("colorAccent", ColorAccent.ToVector4() * Projectile.Opacity * 0.215f);
            godRayShader.SetTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"), 1);
            godRayShader.Apply();

            // Draw a large white rectangle based on the hitbox of the ray.
            // The shader will transform the rectangle into the ray.
            float brightnessFadeIn = GetLerpValue(15f, XerocWaitDelay * 0.67f, TimeSpentInCenter, true);
            float brightnessFadeOut = GetLerpValue(XerocWaitDelay - 4f, XerocWaitDelay - 16f, TimeSpentInCenter, true);
            float brightnessInterpolant = brightnessFadeIn * brightnessFadeOut;
            float brightness = Lerp(0.2f, 0.5f, brightnessInterpolant);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(pixel, drawPosition, null, Projectile.GetAlpha(MainColor) * brightness, Projectile.rotation, pixel.Size() * new Vector2(0.5f, 1f), textureArea, 0, 0f);

            // Draw the vignette over the player's screen.
            DrawVignette(brightnessInterpolant);
        }

        public void DrawVignette(float brightnessInterpolant)
        {
            // Draw a pixel over the player's screen and then draw the vignette over it.
            var vignetteShader = ShaderManager.GetShader("CrackedVignetteShader");
            vignetteShader.TrySetParameter("animationSpeed", 0.05f);
            vignetteShader.TrySetParameter("vignettePower", Lerp(5f, 3.4f, brightnessInterpolant));
            vignetteShader.TrySetParameter("vignetteBrightness", Lerp(3f, 20f, brightnessInterpolant));
            vignetteShader.TrySetParameter("primaryColor", Color.Lerp(Color.Red, Color.Orange, 0.25f).ToVector4() * 1.2f);
            vignetteShader.TrySetParameter("secondaryColor", Color.White.ToVector4() * 0.54f);
            vignetteShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/CrackedNoise"), 1);
            vignetteShader.Apply();

            Texture2D pixel = ModContent.Request<Texture2D>(Texture).Value;
            Color vignetteColor = Projectile.GetAlpha(Color.White) * brightnessInterpolant * GetLerpValue(800f, 308f, Distance(Projectile.Center.X, Main.LocalPlayer.Center.X)) * 0.67f;
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 textureArea = screenArea / pixel.Size();
            Main.spriteBatch.Draw(pixel, screenArea * 0.5f, null, vignetteColor, 0f, pixel.Size() * 0.5f, textureArea, 0, 0f);
        }

        // Manual drawing is not necessary.
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
