using System;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using ReLogic.Content;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
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

        /// <summary>
        /// Uses reflection to set the _uImage1. Its underlying data is private and the only way to change it publicly is via a method that only accepts paths to vanilla textures.
        /// </summary>
        /// <param name="shader">The shader</param>
        /// <param name="texture">The texture to use</param>
        public static void SetShaderTexture(this MiscShaderData shader, Asset<Texture2D> texture) => shaderTextureField.SetValue(shader, texture);

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

        /// <summary>
        /// Returns a color lerp that supports multiple colors.
        /// </summary>
        /// <param name="interpolant">The 0-1 incremental value used when interpolating.</param>
        /// <param name="colors">The various colors to interpolate across.</param>
        public static Color MulticolorLerp(float interpolant, params Color[] colors)
        {
            // Ensure that the interpolant is within the valid 0-1 range.
            interpolant %= 0.999f;

            // Determine which two colors should be interpolated between based on which "slice" the interpolant falls between.
            int currentColorIndex = (int)(interpolant * colors.Length);
            Color currentColor = colors[currentColorIndex];
            Color nextColor = colors[(currentColorIndex + 1) % colors.Length];

            // Interpolate between the two colors. The interpolant is scaled such that it's within the 0-1 range relative to the slice.
            return Color.Lerp(currentColor, nextColor, interpolant * colors.Length % 1f);
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

        public static Vector2 WorldSpaceToScreenUV(Vector2 world)
        {
            // Calculate the coordinates relative to the raw screen size. This does not yet account for things like zoom.
            Vector2 baseUV = (world - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight);

            // Once the above normalized coordinates are calculated, apply the game view matrix to the result to ensure that zoom is incorporated into the result.
            // In order to achieve this it is necessary to firstly anchor the coordinates so that <0, 0> is the origin and not <0.5, 0.5>, and then convert back to
            // the original anchor point after the transformation is complete.
            return Vector2.Transform(baseUV - Vector2.One * 0.5f, Main.GameViewMatrix.TransformationMatrix with { M41 = 0f, M42 = 0f }) + Vector2.One * 0.5f;
        }

        public static List<Vector2> GetLaserControlPoints(this Projectile projectile, int samplesCount, float laserLength, Vector2? laserDirection = null)
        {
            // Calculate the start and end of the laser.
            // The resulting list will interpolate between these two values.
            Vector2 start = projectile.Center;
            Vector2 end = start + (laserDirection ?? projectile.velocity.SafeNormalize(Vector2.Zero)) * laserLength;

            // Generate 'samplesCount' evenly spaced control points.
            List<Vector2> controlPoints = new();
            for (int i = 0; i < samplesCount; i++)
                controlPoints.Add(Vector2.Lerp(start, end, i / (float)(samplesCount - 1f)));

            return controlPoints;
        }

        /// <summary>
        /// Calculates perspective matrices for usage by vertex shaders, notably in the context of primitive meshes.
        /// </summary>
        /// <param name="viewMatrix">The view matrix.</param>
        /// <param name="projectionMatrix">The projection matrix.</param>
        public static void CalculatePrimitivePerspectiveMatricies(out Matrix viewMatrix, out Matrix projectionMatrix)
        {
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

            // Screen bounds.
            int width = Main.instance.GraphicsDevice.Viewport.Width;
            int height = Main.instance.GraphicsDevice.Viewport.Height;

            // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
            viewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

            // Offset the matrix to the appropriate position.
            viewMatrix *= Matrix.CreateTranslation(0f, -height, 0f);

            // Flip the matrix around 180 degrees.
            viewMatrix *= Matrix.CreateRotationZ(MathHelper.Pi);

            // Account for the inverted gravity effect.
            if (Main.LocalPlayer.gravDir == -1f)
                viewMatrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

            // And account for the current zoom.
            viewMatrix *= zoomScaleMatrix;

            projectionMatrix = Matrix.CreateOrthographicOffCenter(0f, width * zoom.X, 0f, height * zoom.Y, 0f, 1f) * zoomScaleMatrix;
        }

        /// <summary>
        /// Draws a projectile as a series of afterimages. The first of these afterimages is centered on the center of the projectile's hitbox.<br />
        /// This function is guaranteed to draw the projectile itself, even if it has no afterimages and/or the Afterimages config option is turned off.
        /// </summary>
        /// <param name="proj">The projectile to be drawn.</param>
        /// <param name="mode">The type of afterimage drawing code to use. Vanilla Terraria has three options: 0, 1, and 2.</param>
        /// <param name="lightColor">The light color to use for the afterimages.</param>
        /// <param name="typeOneIncrement">If mode 1 is used, this controls the loop increment. Set it to more than 1 to skip afterimages.</param>
        /// <param name="texture">The texture to draw. Set to <b>null</b> to draw the projectile's own loaded texture.</param>
        /// <param name="drawCentered">If <b>false</b>, the afterimages will be centered on the projectile's position instead of its own center.</param>
        public static void DrawAfterimagesCentered(Projectile proj, int mode, Color lightColor, int typeOneIncrement = 1, Texture2D texture = null, bool drawCentered = true)
        {
            // Use the projectile's default texture if nothing is explicitly supplied.
            texture ??= TextureAssets.Projectile[proj.type].Value;

            // Calculate frame information for the projectile.
            int frameHeight = texture.Height / Main.projFrames[proj.type];
            int frameY = frameHeight * proj.frame;
            Rectangle rectangle = new(0, frameY, texture.Width, frameHeight);

            // Calculate the projectile's origin, rotation, and scale.
            Vector2 origin = rectangle.Size() * 0.5f;
            float rotation = proj.rotation;
            float scale = proj.scale;

            // Calculate the direction of the projectile as a SpriteEffects instance.
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (proj.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // If no afterimages are drawn due to an invalid mode being specified, ensure the projectile itself is drawn anyway at the end of this method.
            bool failedToDrawAfterimages = false;

            if (CalamityConfig.Instance.Afterimages)
            {
                Vector2 centerOffset = drawCentered ? proj.Size * 0.5f : Vector2.Zero;
                switch (mode)
                {
                    // Standard afterimages. No customizable features other than total afterimage count.
                    // Type 0 afterimages linearly scale down from 100% to 0% opacity. Their color and lighting is equal to the main projectile's.
                    case 0:
                        for (int i = 0; i < proj.oldPos.Length; ++i)
                        {
                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + Vector2.UnitY * proj.gfxOffY;
                            Color color = proj.GetAlpha(lightColor) * ((proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, rotation, origin, scale, spriteEffects, 0f);
                        }
                        break;

                    // Paladin's Hammer style afterimages. Can be optionally spaced out further by using the typeOneDistanceMultiplier variable.
                    // Type 1 afterimages linearly scale down from 66% to 0% opacity. They otherwise do not differ from type 0.
                    case 1:
                        // Safety check: the loop must increment
                        int increment = Math.Max(1, typeOneIncrement);
                        Color drawColor = proj.GetAlpha(lightColor);
                        int afterimageCount = ProjectileID.Sets.TrailCacheLength[proj.type];
                        int k = 0;
                        while (k < afterimageCount)
                        {
                            Vector2 drawPos = proj.oldPos[k] + centerOffset - Main.screenPosition + Vector2.UnitY * proj.gfxOffY;
                            if (k > 0)
                            {
                                float colorMult = afterimageCount - k;
                                drawColor *= colorMult / (afterimageCount * 1.5f);
                            }
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), drawColor, rotation, origin, scale, spriteEffects, 0f);
                            k += increment;
                        }
                        break;

                    // Standard afterimages with rotation. No customizable features other than total afterimage count.
                    // Type 2 afterimages linearly scale down from 100% to 0% opacity. Their color and lighting is equal to the main projectile's.
                    case 2:
                        for (int i = 0; i < proj.oldPos.Length; ++i)
                        {
                            float afterimageRot = proj.oldRot[i];
                            SpriteEffects sfxForThisAfterimage = proj.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + Vector2.UnitY * proj.gfxOffY;
                            Color color = proj.GetAlpha(lightColor) * ((proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, afterimageRot, origin, scale, sfxForThisAfterimage, 0f);
                        }
                        break;

                    default:
                        failedToDrawAfterimages = true;
                        break;
                }
            }

            // Draw the projectile itself. Only do this if no afterimages are drawn because afterimage 0 is the projectile itself.
            if (!CalamityConfig.Instance.Afterimages || ProjectileID.Sets.TrailCacheLength[proj.type] <= 0 || failedToDrawAfterimages)
            {
                Vector2 startPos = drawCentered ? proj.Center : proj.position;
                Main.spriteBatch.Draw(texture, startPos - Main.screenPosition + new Vector2(0f, proj.gfxOffY), rectangle, proj.GetAlpha(lightColor), rotation, origin, scale, spriteEffects, 0f);
            }
        }
    }
}
