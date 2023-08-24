using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class OgsculeOverlaySystem : ModSystem
    {
        public static Vector2 DisclaimerPosition
        {
            get;
            set;
        }

        public static Vector2 DisclaimerVelocity
        {
            get;
            set;
        }

        public override void OnModLoad() => Main.OnPostDraw += DrawOgscule;

        public override void OnModUnload() => Main.OnPostDraw -= DrawOgscule;

        public static bool OgsculeRulesOverTheUniverse => Main.zenithWorld && WorldSaveSystem.OgsculeRulesOverTheUniverse;

        private void DrawOgscule(GameTime obj)
        {
            if (!OgsculeRulesOverTheUniverse)
            {
                DisclaimerPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - Vector2.UnitY * 200f;
                DisclaimerVelocity = new Vector2(4f, 4f);
                return;
            }

            // Draw the ogscule overlay.
            Main.spriteBatch.Begin();

            float scale = Sin(Main.GlobalTimeWrappedHourly * 3.5f) * 0.03f + 0.55f;
            Color gay = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 4f % 1f, 0.5f, 0.45f);
            Texture2D ogscule = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Ogscule").Value;
            Vector2 ogsculeScale = new Vector2(Main.screenWidth, Main.screenHeight) / ogscule.Size();
            Main.spriteBatch.Draw(ogscule, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, null, Color.White * 0.35f, 0f, ogscule.Size() * 0.5f, ogsculeScale, 0, 0f);
            Main.spriteBatch.DrawString(FontAssets.DeathText.Value, "This ogscule effect is not added by the base Calamity Mod. Don't annoy them about it.", DisclaimerPosition, gay, 0f, Vector2.Zero, scale, 0, 0f);

            // Update the ogscule position.
            DisclaimerPosition += DisclaimerVelocity;
            if (DisclaimerPosition.X <= 0f || DisclaimerPosition.X >= Main.screenWidth - 940f)
                DisclaimerVelocity = Vector2.Reflect(DisclaimerVelocity, Vector2.UnitX);
            if (DisclaimerPosition.Y <= 0f || DisclaimerPosition.Y >= Main.screenHeight - 32f)
                DisclaimerVelocity = Vector2.Reflect(DisclaimerVelocity, Vector2.UnitY);
            DisclaimerPosition = Vector2.Clamp(DisclaimerPosition, new Vector2(1f, 1f), new Vector2(Main.screenWidth - 941f, Main.screenHeight - 33f));

            Main.spriteBatch.End();
        }
    }

    public class OgsculeOverlayScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)100;

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Ogscule");

        public override bool IsSceneEffectActive(Player player) => OgsculeOverlaySystem.OgsculeRulesOverTheUniverse;
    }
}
