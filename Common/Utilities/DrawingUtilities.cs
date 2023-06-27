using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Reflection;
using Terraria;
using Terraria.Chat;
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
        /// Sets a <see cref="SpriteBatch"/>'s <see cref="BlendState"/> arbitrarily.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="blendState">The blend state to use.</param>
        public static void SetBlendState(this SpriteBatch spriteBatch, BlendState blendState)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, blendState, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

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
    }
}
