using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class NoxusDefeatAnimationScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NoxusDefeatAnimationSystem.AnimationTimer >= 1;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override int Music => 0;
    }

    public class NoxusDefeatAnimationSystem : ModSystem
    {
        public static int AnimationTimer
        {
            get;
            private set;
        }

        public static int AnimationDelay
        {
            get;
            private set;
        }

        public static readonly string[] TextLines = new string[]
        {
            "You have done well.",
            "Now, seek the Terminus. Your final test is at hand.",
        };

        public static readonly SoundStyle AmbientSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NoxusDefeatXerocMessageAmbience") with { Volume = 1.5f };

        public override void OnModLoad() => Main.OnPostDraw += DrawAnimation;

        public override void OnModUnload() => Main.OnPostDraw -= DrawAnimation;

        private void DrawAnimation(GameTime obj)
        {
            // Don't do anything if the animation timer has not been started.
            if (AnimationDelay <= 0)
                return;

            // Don't do anything if the game isn't being focused on.
            if (!Main.instance.IsActive)
                return;

            float animationCompletion = GetLerpValue(0f, 420f, AnimationTimer, true);
            float whiteOverlayOpacity = GetLerpValue(0f, 0.19f, animationCompletion, true) * GetLerpValue(1f, 0.875f, animationCompletion, true);

            // Stop the animation once it has completed.
            if (animationCompletion >= 1f)
            {
                AnimationTimer = 0;
                AnimationDelay = 0;
                return;
            }

            Main.spriteBatch.Begin();

            // Draw the white overlay.
            Texture2D pixel = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Pixel").Value;
            Vector2 pixelScale = new Vector2(Main.screenWidth, Main.screenHeight) * 2f / pixel.Size();
            Main.spriteBatch.Draw(pixel, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, null, Color.White * whiteOverlayOpacity, 0f, pixel.Size() * 0.5f, pixelScale, 0, 0f);

            // Draw the text that directs the player to seek the Terminus.
            for (int i = 0; i < TextLines.Length; i++)
            {
                string line = TextLines[i];
                float appearDelay = i * 0.255f;
                float lineTextOpacity = GetLerpValue(appearDelay + 0.15f, appearDelay + 0.25f, animationCompletion, true) * GetLerpValue(0.95f, 0.85f, animationCompletion, true);
                Vector2 drawPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - FontAssets.DeathText.Value.MeasureString(line) * 0.5f + Vector2.UnitY * i * 50f;
                Main.spriteBatch.DrawString(FontAssets.DeathText.Value, line, drawPosition, Color.LightCoral * lineTextOpacity);
            }

            if (AnimationDelay < 480)
            {
                AnimationDelay++;
                if (AnimationDelay == 480)
                    SoundEngine.PlaySound(AmbientSound);
            }
            else
                AnimationTimer++;
            Main.spriteBatch.End();
        }

        public static void Start()
        {
            AnimationTimer = 1;
            AnimationDelay = 1;
            ShakeScreen(Main.LocalPlayer.Center - Vector2.UnitY * 450f, 9f);
            ScreenEffectSystem.SetChromaticAberrationEffect(Main.LocalPlayer.Center - Vector2.UnitY * 450f, 1.5f, 40);
        }
    }
}
