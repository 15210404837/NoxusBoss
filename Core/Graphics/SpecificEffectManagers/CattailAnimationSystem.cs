using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets.Fonts;
using ReLogic.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class CattailAnimationSystem : ModSystem
    {
        public static int AnimationTimer
        {
            get;
            private set;
        }

        public static int DelayUntilAnimationBegins => 720;

        public static int AnimationDuration => 960;

        public static readonly Color TextColor = new(74, 164, 37);

        public override void OnModLoad()
        {
            Main.OnPostDraw += DrawAnimationWrapper;
        }

        public override void OnModUnload()
        {
            Main.OnPostDraw -= DrawAnimationWrapper;
        }

        private void DrawAnimationWrapper(GameTime obj)
        {
            if (AnimationTimer <= 0)
                return;

            Main.spriteBatch.Begin();
            DrawAnimation();
            Main.spriteBatch.End();

            // Make the animation go on. Once it concludes it goes away.
            AnimationTimer++;
            if (AnimationTimer >= DelayUntilAnimationBegins + AnimationDuration)
                AnimationTimer = 0;
        }

        private static void DrawAnimation()
        {
            float animationCompletion = GetLerpValue(0f, AnimationDuration, AnimationTimer - DelayUntilAnimationBegins, true);
            float opacity = GetLerpValue(0f, 0.1f, animationCompletion, true) * GetLerpValue(1f, 0.9f, animationCompletion, true);

            // Don't bother if nothing would draw due to zero opacity.
            if (opacity <= 0f)
                return;

            // Draw the line overlay over the screen.
            Texture2D line = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/FadedLine").Value;
            Vector2 drawCenter = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - Vector2.UnitY * 80f;
            Vector2 lineScale = new(Main.screenWidth / line.Width * 1.8f, 1.4f);
            Main.spriteBatch.Draw(line, drawCenter, null, Color.Black * opacity * 0.75f, 0f, line.Size() * 0.5f, lineScale, 0, 0f);

            // Draw the special text.
            string text = Language.GetTextValue("Mods.NoxusBoss.Dialog.CattailText");
            DynamicSpriteFont font = FontRegistry.Instance.XerocText;
            float scale = 1.1f;
            float maxHeight = 300f;
            Vector2 textSize = font.MeasureString(text);
            if (textSize.Y > maxHeight)
                scale = maxHeight / textSize.Y;
            Vector2 textDrawPosition = drawCenter - textSize * scale * 0.5f;
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, textDrawPosition, TextColor * Pow(opacity, 1.6f), 0f, Vector2.Zero, new(scale), -1f, 2f);
        }

        public static void StartAnimation()
        {
            AnimationTimer = 1;
        }
    }
}
