using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class InterfacedProjectileDrawSystem : ModSystem
    {
        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            On.Terraria.Main.DrawProjectiles += DrawInterfaceProjectiles;
        }

        private void DrawInterfaceProjectiles(On.Terraria.Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            DrawShaderProjectiles();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            DrawAdditiveProjectiles();

            Main.spriteBatch.End();
        }

        public static void DrawShaderProjectiles()
        {
            // Draw all projectiles that have the shader interface.
            List<IDrawsWithShader> orderedDrawers = Main.projectile.Take(Main.maxProjectiles).Where(p =>
            {
                return p.active && p.ModProjectile is IDrawsWithShader drawer;
            }).Select(p => p.ModProjectile as IDrawsWithShader).OrderBy(i => i.LayeringPriority).ToList();

            foreach (var drawer in orderedDrawers)
                drawer.Draw(Main.spriteBatch);
        }

        public static void DrawAdditiveProjectiles()
        {
            // Draw all projectiles that have the additive interface.
            List<IDrawAdditive> orderedDrawers = Main.projectile.Take(Main.maxProjectiles).Where(p =>
            {
                return p.active && p.ModProjectile is IDrawAdditive drawer;
            }).Select(p => p.ModProjectile as IDrawAdditive).ToList();

            foreach (var drawer in orderedDrawers)
                drawer.DrawAdditive(Main.spriteBatch);
        }
    }
}
