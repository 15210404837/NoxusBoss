using System;
using System.Reflection;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Chat;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        private static readonly FieldInfo shaderTextureField = typeof(MiscShaderData).GetField("_uImage1", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo shaderTextureField2 = typeof(MiscShaderData).GetField("_uImage2", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo shaderTextureField3 = typeof(MiscShaderData).GetField("_uImage3", BindingFlags.NonPublic | BindingFlags.Instance);

        public static Rectangle MouseScreenRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 1, 1);

        /// <summary>
        /// Uses reflection to set the _uImage1. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField.SetValue(shader, texture);

        /// <summary>
        /// Uses reflection to set the _uImage2. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture2(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField2.SetValue(shader, texture);

        /// <summary>
        /// Uses reflection to set the _uImage3. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture3(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField3.SetValue(shader, texture);

        /// <summary>
        /// Reset's a <see cref="SpriteBatch"/>'s <see cref="BlendState"/> based to a typical <see cref="BlendState.AlphaBlend"/>.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="blendState">The blend state to use.</param>
        public static void ResetBlendState(this SpriteBatch spriteBatch) => spriteBatch.SetBlendState(BlendState.AlphaBlend);

        public static void DrawBloomLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            // Draw nothing if the start and end are equal, to prevent division by 0 problems.
            if (start == end)
                return;

            start -= Main.screenPosition;
            end -= Main.screenPosition;

            Texture2D line = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Lines/BloomLine").Value;
            float rotation = (end - start).ToRotation() + PiOver2;
            Vector2 scale = new Vector2(width, Vector2.Distance(start, end)) / line.Size();
            Vector2 origin = new(line.Width / 2f, line.Height);

            spriteBatch.Draw(line, start, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        public static void SwapToRenderTarget(this RenderTarget2D renderTarget, Color? flushColor = null)
        {
            // Local variables for convinience.
            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            // If we are in the menu, a server, or any of these are null, return.
            if (Main.gameMenu || Main.dedServ || renderTarget is null || graphicsDevice is null || spriteBatch is null)
                return;

            // Otherwise set the render target.
            graphicsDevice.SetRenderTarget(renderTarget);

            // "Flush" the screen, removing any previous things drawn to it.
            flushColor ??= Color.Transparent;
            graphicsDevice.Clear(flushColor.Value);
        }

        public static void BroadcastText(string text, Color color)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                Main.NewText(text, color);
            else if (Main.netMode == NetmodeID.Server)
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
        }

        public static Matrix GetCustomSkyBackgroundMatrix()
        {
            Matrix transformationMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
            transformationMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation *
                new Vector3(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? (-1f) : 1f, 1f);
            return transformationMatrix;
        }

        public static void DrawBloomLineTelegraph(Vector2 drawPosition, BloomLineDrawInfo drawInfo, bool resetSpritebatch = true, Vector2? resolution = null)
        {
            // Claim texture and shader data in easy to use local variables.
            Texture2D invisible = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;

            // Prepare all parameters for the shader in anticipation that they will go the GPU for shader effects.
            laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
            laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.004f);
            laserScopeEffect.Parameters["mainOpacity"].SetValue(drawInfo.Opacity);
            laserScopeEffect.Parameters["Resolution"].SetValue(resolution ?? Vector2.One * 425f);
            laserScopeEffect.Parameters["laserAngle"].SetValue(drawInfo.LineRotation);
            laserScopeEffect.Parameters["laserWidth"].SetValue(drawInfo.WidthFactor);
            laserScopeEffect.Parameters["laserLightStrenght"].SetValue(drawInfo.LightStrength);
            laserScopeEffect.Parameters["color"].SetValue(drawInfo.MainColor.ToVector3());
            laserScopeEffect.Parameters["darkerColor"].SetValue(drawInfo.DarkerColor.ToVector3());
            laserScopeEffect.Parameters["bloomSize"].SetValue(drawInfo.BloomIntensity);
            laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(drawInfo.BloomOpacity);
            laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

            // Prepare the sprite batch for shader drawing.
            if (resetSpritebatch)
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            laserScopeEffect.CurrentTechnique.Passes[0].Apply();

            // Draw the texture with the shader and flush the results to the GPU, clearing the shader effect for any successive draw calls.
            Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, 0f, invisible.Size() * 0.5f, drawInfo.Scale, SpriteEffects.None, 0f);
            if (resetSpritebatch)
                Main.spriteBatch.ExitShaderRegion();
        }

        public static void ShakeScreen(Vector2 shakeCenter, float shakePower, float intensityTaperEndDistance = 2300f, float intensityTaperStartDistance = 1476f)
        {
            float distanceToShake = Main.LocalPlayer.Distance(shakeCenter);
            float desiredScreenShakePower = GetLerpValue(intensityTaperEndDistance, intensityTaperStartDistance, distanceToShake, true) * shakePower;

            // If the desired screen shake power is less than what the player's shake intensity is, ignore it. It would be weird for it to suddenly
            // drop in intensity.
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = MathF.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, desiredScreenShakePower);
        }

        public static Vector2 WorldSpaceToScreenUV(Vector2 world)
        {
            // Calculate the coordinates relative to the raw screen size. This does not yet account for things like zoom.
            Vector2 baseUV = (world - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight);

            // Once the above normalized coordinates are calculated, apply the game view matrix to the result to ensure that zoom is incorporated into the result.
            // In order to achieve this it is necessary to firstly anchor the coordinates so that <0, 0> is the origin and not <0.5, 0.5>, and then convert back to
            // the original anchor point after the transformation is complete.
            return Vector2.Transform(baseUV - Vector2.One * 0.5f, Main.GameViewMatrix.TransformationMatrix with { M41 = 0f, M42 = 0f }) + Vector2.One * 0.5f;
        }
    }
}
