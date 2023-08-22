using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Content.Items;
using Terraria.DataStructures;
using System.Collections.Generic;
using Terraria.Graphics.Renderers;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using NoxusBoss.Content.Bosses.Xeroc;

namespace NoxusBoss.Core.Graphics
{
    public class DivineWingDrawer : ModSystem
    {
        private static bool disallowSpecialWingDrawing;

        private static bool anyoneIsUsingWings;

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
            On_LegacyPlayerRenderer.DrawPlayers += DrawWingsTarget;
            On_PlayerDrawLayers.DrawPlayer_09_Wings += DisallowWingDrawingIfNecessary;
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PrepareAfterimageTarget;
        }

        private void DrawWingsTarget(On_LegacyPlayerRenderer.orig_DrawPlayers orig, LegacyPlayerRenderer self, Camera camera, IEnumerable<Player> players)
        {
            if (anyoneIsUsingWings)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, camera.Rasterizer, null, camera.GameViewMatrix.TransformationMatrix);

                // Optionally apply the wing shader. This will have some weird multiplayer oddities in terms of other's wings being affected by the client's shader but whatever.
                // The alternative of creating render targets for every player is not a pleasant thought.
                if (Main.LocalPlayer.cWings != 0)
                    GameShaders.Armor.Apply(Main.LocalPlayer.cWings, Main.LocalPlayer);

                Main.spriteBatch.Draw(AfterimageTargetPrevious.Target, Main.screenLastPosition - Main.screenPosition, Color.White);
                Main.spriteBatch.End();
            }

            orig(self, camera, players);
        }

        private void DisallowWingDrawingIfNecessary(On_PlayerDrawLayers.orig_DrawPlayer_09_Wings orig, ref PlayerDrawSet drawinfo)
        {
            if (drawinfo.hideEntirePlayer || drawinfo.drawPlayer.dead)
                return;

            if (drawinfo.drawPlayer.wings == DivineWings.WingSlotID && disallowSpecialWingDrawing)
            {
                // Calculate various draw data for the outline.
                Vector2 playerPosition = drawinfo.Position - Main.screenPosition + new Vector2(drawinfo.drawPlayer.width / 2, drawinfo.drawPlayer.height - drawinfo.drawPlayer.bodyFrame.Height / 2) + Vector2.UnitY * 7f;
                Vector2 wingDrawPosition = (playerPosition + new Vector2(-9f, 2f) * drawinfo.drawPlayer.Directions).Floor();
                Texture2D outlineTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Items/DivineWings_WingsOutline").Value;
                Rectangle outlineFrame = outlineTexture.Frame(1, 4, 0, drawinfo.drawPlayer.wingFrame);
                Color outlineColor = Color.White * Pow(drawinfo.stealth * (1f - drawinfo.shadow), 3f);
                Vector2 outlineOrigin = outlineFrame.Size() * 0.5f;

                DrawData outline = new(outlineTexture, wingDrawPosition, outlineFrame, outlineColor, drawinfo.drawPlayer.bodyRotation, outlineOrigin, 1f, drawinfo.playerEffect, 0f)
                {
                    shader = drawinfo.cWings
                };
                drawinfo.DrawDataCache.Add(outline);
                return;
            }

            orig(ref drawinfo);
        }

        private void PrepareAfterimageTarget(GameTime obj)
        {
            // Check if anyone is using the special wings.
            anyoneIsUsingWings = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.wings != DivineWings.WingSlotID || !p.active || p.dead)
                    continue;

                anyoneIsUsingWings = true;
                break;
            }

            if (!ShaderManager.HasFinishedLoading || Main.gameMenu || !anyoneIsUsingWings)
                return;

            var gd = Main.instance.GraphicsDevice;

            // Prepare the render target for drawing.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            gd.SetRenderTarget(AfterimageTarget.Target);
            gd.Clear(Color.Transparent);

            // Draw the contents of the previous frame to the target.
            Main.spriteBatch.Draw(AfterimageTargetPrevious.Target, Vector2.Zero, Color.White);

            // Draw player wings.
            DrawPlayerWingsToTarget();

            // Draw the afterimage shader to the result.
            ApplyPsychedelicDiffusionEffects();

            // Return to the backbuffer.
            Main.spriteBatch.End();
            gd.SetRenderTarget(null);
        }

        public static void DrawPlayerWingsToTarget()
        {
            // Prepare the wing psychedelic shader.
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocWingNormalMap").Value;
            var wingShader = ShaderManager.GetShader("XerocPsychedelicWingShader");
            wingShader.TrySetParameter("colorShift", XerocBoss.WingColorShift);
            wingShader.TrySetParameter("lightDirection", Vector3.UnitZ);
            wingShader.TrySetParameter("normalMapCrispness", 0.86f);
            wingShader.TrySetParameter("normalMapZoom", new Vector2(0.7f, 0.4f));
            wingShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"), 1);
            wingShader.Apply();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.wings != DivineWings.WingSlotID || p.dead || !p.active)
                    continue;

                // Append the player's wings to the draw cache.
                PlayerDrawSet drawInfo = default;
                drawInfo.BoringSetup(p, new List<DrawData>(), new List<int>(), new List<int>(), p.TopLeft + Vector2.UnitY * p.gfxOffY, 0f, p.fullRotation, p.fullRotationOrigin);
                disallowSpecialWingDrawing = false;
                PlayerDrawLayers.DrawPlayer_09_Wings(ref drawInfo);
                disallowSpecialWingDrawing = true;

                // Draw the wings with the activated shader.
                foreach (DrawData wingData in drawInfo.DrawDataCache)
                    wingData.Draw(Main.spriteBatch);
            }
        }

        public static void ApplyPsychedelicDiffusionEffects()
        {
            if (!ShaderManager.HasFinishedLoading || !anyoneIsUsingWings)
                return;

            var gd = Main.instance.GraphicsDevice;
            gd.SetRenderTarget(AfterimageTargetPrevious.Target);
            gd.Clear(Color.Transparent);

            // Prepare the afterimage psychedelic shader.
            var afterimageShader = ShaderManager.GetShader("XerocPsychedelicAfterimageShader");
            afterimageShader.TrySetParameter("uScreenResolution", new Vector2(Main.screenWidth, Main.screenHeight));
            afterimageShader.TrySetParameter("warpSpeed", 0.00028f);
            afterimageShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"), 1);
            afterimageShader.Apply();

            Main.spriteBatch.Draw(AfterimageTarget.Target, Vector2.Zero, Color.White);
        }
    }
}
