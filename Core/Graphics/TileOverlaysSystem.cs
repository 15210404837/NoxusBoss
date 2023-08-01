using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class TileOverlaysSystem : ModSystem
    {
        public static ManagedRenderTarget OverlayableTarget
        {
            get;
            private set;
        }

        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() => OverlayableTarget = new(true, RenderTargetManager.CreateScreenSizedTarget));
            Main.OnPreDraw += PrepareOverlayTarget;
            On_Main.DrawProjectiles += DrawOverlayTarget;
        }

        public override void OnModUnload()
        {
            Main.OnPreDraw -= PrepareOverlayTarget;
        }

        private void PrepareOverlayTarget(GameTime obj)
        {
            if (OverlayableTarget is null)
                return;

            var gd = Main.instance.GraphicsDevice;

            gd.SetRenderTarget(OverlayableTarget.Target);
            gd.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw all projectiles that have the relevant interface.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.ModProjectile is IDrawsOverTiles drawer)
                    drawer.Draw(Main.spriteBatch);
            }

            Main.spriteBatch.End();
            gd.SetRenderTarget(null);
        }

        private void DrawOverlayTarget(Terraria.On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Prepare the overlay shader and supply it with tile information.
            var shader = ShaderManager.GetShader("TileOverlayShader");
            shader.TrySetParameter("zoom", new Vector2(1.15f, 1.27f));
            shader.TrySetParameter("tileOverlayOffset", (Main.sceneTilePos - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight) * -1f);
            shader.TrySetParameter("inversionZoom", Main.GameViewMatrix.Zoom);
            shader.SetTexture(Main.instance.tileTarget, 1);
            shader.SetTexture(Main.instance.blackTarget, 2);
            shader.Apply();

            Main.spriteBatch.Draw(OverlayableTarget.Target, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
    }
}
