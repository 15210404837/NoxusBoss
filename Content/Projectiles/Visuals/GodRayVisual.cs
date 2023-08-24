using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Dusts;
using NoxusBoss.Content.Subworlds;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ModLoader;

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

        public override void SetDefaults()
        {
            // Using the Height constant here has a habit of causing vanilla's out-of-world projectile deletion to kill this, due to how large it is.
            Projectile.width = Width;
            if (EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
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

        public void Draw(SpriteBatch spriteBatch)
        {
            // Collect the shader and draw data for later.
            var godRayShader = ShaderManager.GetShader("GodRayShader");
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 scale = new Vector2(Projectile.width, Height) / texture.Size();

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
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(MainColor) * 0.2f, Projectile.rotation, texture.Size() * new Vector2(0.5f, 1f), scale, 0, 0f);
        }

        // Manual drawing is not necessary.
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
