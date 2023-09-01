﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class PrimitivePixelationSystem : ModSystem
    {
        private static bool primsWereDrawnLastFrame;

        public static ManagedRenderTarget PixelationTarget
        {
            get;
            private set;
        }

        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() => PixelationTarget = new(true, RenderTargetManager.CreateScreenSizedTarget));
            On_Main.CheckMonoliths += PreparePixelationTarget;
            On_Main.DoDraw_DrawNPCsOverTiles += DrawPixelationTarget;
        }

        private void PreparePixelationTarget(On_Main.orig_CheckMonoliths orig)
        {
            bool primsExist = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active)
                    continue;

                bool modProjectileHasInterface = p.ModProjectile is not IDrawPixelated;
                bool modProjectileUsesGroupWithPixelation = p.ModProjectile is IDrawGroupedPrimitives primGroup && primGroup.DrawContext.HasFlag(PrimitiveGroupDrawContext.Pixelated);
                if (!modProjectileHasInterface && !modProjectileUsesGroupWithPixelation)
                    continue;

                primsExist = true;
                break;
            }

            if (!primsExist)
            {
                primsWereDrawnLastFrame = false;
                orig();
                return;
            }

            // Start a spritebatch, as a Begin call does not exist before the method that's being detoured.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

            // Go to the pixelation target.
            var gd = Main.instance.GraphicsDevice;
            gd.SetRenderTarget(PixelationTarget.Target);
            gd.Clear(Color.Transparent);

            // Draw prims to the render target.
            primsWereDrawnLastFrame = false;
            DrawPixelatedProjectiles();
            DrawPrimitiveGroups();

            // Return to the backbuffer.
            gd.SetRenderTarget(null);

            // Prepare the sprite batch for the next draw cycle.
            Main.spriteBatch.End();

            orig();
        }

        private void DrawPixelationTarget(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            // Simply call orig if no prims were drawn as an optimization.
            if (!primsWereDrawnLastFrame)
            {
                orig(self);
                return;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            // Apply the pixelation shader.
            var pixelationShader = ShaderManager.GetShader("PixelationShader");
            pixelationShader.TrySetParameter("pixelationFactor", Vector2.One * 3f / PixelationTarget.Target.Size());
            pixelationShader.Apply();

            Main.spriteBatch.Draw(PixelationTarget.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            orig(self);
        }

        private static void DrawPixelatedProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.ModProjectile is not IDrawPixelated primDrawer)
                    continue;

                primDrawer.DrawWithPixelation();
                primsWereDrawnLastFrame = true;
            }
        }

        private static void DrawPrimitiveGroups()
        {
            if (PrimitiveTrailGroupingSystem.DrawGroupWithDrawContext(PrimitiveGroupDrawContext.Pixelated))
                primsWereDrawnLastFrame = true;
        }
    }
}
