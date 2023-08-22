using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Core.Graphics.Shaders.Keyboard
{
    public class KeyboardDebugDrawer : ModSystem
    {
        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawDebugKeyboard;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawDebugKeyboard;
        }

        private void DrawDebugKeyboard(GameTime obj)
        {
            if (!NoxusBoss.DebugFeaturesEnabled || Main.gameMenu)
                return;

            Main.DebugDrawer.Begin(Main.GameViewMatrix.TransformationMatrix);

            Vector2 keyboardDrawPosition = new Vector2(Main.screenWidth - 400f, Main.screenHeight) * 0.5f + Vector2.UnitY * 100f;
            Main.Chroma.DebugDraw(Main.DebugDrawer, keyboardDrawPosition, 100f);

            Main.DebugDrawer.End();
        }
    }
}
