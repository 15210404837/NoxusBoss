﻿using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace NoxusBoss.Core.Graphics
{
    public class MetaballManager : ModSystem
    {
        private static readonly List<Metaball> metaballs = new();

        public override void OnModLoad()
        {
            Main.OnPreDraw += PrepareMetaballTargets;
            foreach (Type t in AssemblyManager.GetLoadableTypes(Mod.Code))
            {
                if (!t.IsSubclassOf(typeof(Metaball)) || t.IsAbstract)
                    continue;

                metaballs.Add((Metaball)Activator.CreateInstance(t));
            }

            On_Main.DrawProjectiles += DrawMetaballsAfterProjectiles;
            On_Main.DrawNPCs += DrawMetaballsBeforeNPCs;
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PrepareMetaballTargets;
            Main.QueueMainThreadAction(() =>
            {
                foreach (Metaball metaball in metaballs)
                    metaball?.Dispose();
            });
        }

        private void PrepareMetaballTargets(GameTime obj)
        {
            // Prepare the sprite batch for drawing. Metaballs may restart the sprite batch if their implementation requires it.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);

            var gd = Main.instance.GraphicsDevice;
            foreach (Metaball metaball in metaballs)
            {
                // Don't bother wasting resources if the metaballs are not in used at the moment.
                if (!metaball.AnythingToDraw)
                    continue;

                // Update the metaball.
                if (!Main.gamePaused)
                    metaball.Update();

                // Prepare the sprite batch in accordance to the needs of the metaball instance. By default this does nothing, 
                metaball.PrepareSpriteBatch(Main.spriteBatch);

                // Draw the raw contents of the metaball to each of its render targets.
                foreach (ManagedRenderTarget target in metaball.LayerTargets)
                {
                    gd.SetRenderTarget(target);
                    gd.Clear(Color.Transparent);
                    metaball.DrawInstances();
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            }

            // Return the backbuffer and end the sprite batch.
            gd.SetRenderTarget(null);
            Main.spriteBatch.End();
        }

        private void DrawMetaballsAfterProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawMetaballs(MetaballDrawLayerType.AfterProjectiles);
            Main.spriteBatch.End();
        }

        private void DrawMetaballsBeforeNPCs(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            if (!behindTiles)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                DrawMetaballs(MetaballDrawLayerType.BeforeNPCs);
                Main.spriteBatch.ExitShaderRegion();
            }
            orig(self, behindTiles);
        }

        internal static void DrawMetaballs(MetaballDrawLayerType layerType)
        {
            var metaballShader = ShaderManager.GetShader("MetaballEdgeShader");
            var gd = Main.instance.GraphicsDevice;

            foreach (Metaball metaball in metaballs.Where(m => m.DrawContext == layerType && m.AnythingToDraw))
            {
                for (int i = 0; i < metaball.LayerTargets.Count; i++)
                {
                    metaballShader.TrySetParameter("layerSize", metaball.Layers[i].Size());
                    metaballShader.TrySetParameter("screenSize", new Vector2(Main.screenWidth, Main.screenHeight));
                    metaballShader.TrySetParameter("layerOffset", metaball.FixedInPlace ? Vector2.Zero : Main.screenPosition / new Vector2(Main.screenWidth, Main.screenHeight));
                    metaballShader.TrySetParameter("edgeColor", metaball.EdgeColor.ToVector4());
                    metaballShader.TrySetParameter("singleFrameScreenOffset", (Main.screenLastPosition - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight));
                    metaballShader.SetTexture(metaball.Layers[i], 1);
                    metaballShader.Apply();

                    Main.spriteBatch.Draw(metaball.LayerTargets[i], Main.screenLastPosition - Main.screenPosition, Color.White);
                }
            }
        }
    }
}
