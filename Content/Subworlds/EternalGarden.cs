using System.Collections.Generic;
using SubworldLibrary;
using Terraria.IO;
using Terraria;
using Terraria.WorldBuilding;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using ReLogic.Content;
using CalamityMod.World;
using static NoxusBoss.Core.WorldSaveSystem;
using NoxusBoss.Assets.Fonts;
using Terraria.Localization;
using NoxusBoss.Core.Fixes;
using NoxusBoss.Core.CrossCompatibility;

namespace NoxusBoss.Content.Subworlds
{
    public class EternalGarden : Subworld
    {
        public class EternalGardenPass : GenPass
        {
            public EternalGardenPass() : base("Terrain", 1f) { }

            protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
            {
                // Set the progress text.
                progress.Message = "Forming the Eternal Garden.";

                // Define the position of the world lines.
                Main.worldSurface = Main.maxTilesY - 8;
                Main.rockLayer = Main.maxTilesY - 9;

                // Generate the garden.
                EternalGardenWorldGen.Generate();
            }
        }

        private static TagCompound savedWorldData;

        public static float TextOpacity
        {
            get;
            set;
        }

        public override int Width => 1200;

        public override int Height => 350;

        // This is mainly so that map data is saved across attempts.
        public override bool ShouldSave => true;

        public override List<GenPass> Tasks => new()
        {
            new EternalGardenPass()
        };

        public override bool ChangeAudio()
        {
            // Get rid of the jarring title screen music when moving between subworlds.
            if (Main.gameMenu)
            {
                Main.newMusic = 0;
                return true;
            }

            return false;
        }

        public override void DrawMenu(GameTime gameTime)
        {
            // Make the text appear.
            TextOpacity = Clamp(TextOpacity + 0.093f, 0f, 1f);

            // Give ominous text about how the player will "be tested" when entering the garden.
            // When exiting, the regular load details text is displayed.
            var font = FontRegistry.Instance.XerocText;
            string text = Language.GetTextValue($"Mods.{Mod.Name}.Dialog.XerocEnterGardenText");
            Color textColor = Color.LightCoral;
            if (!SubworldSystem.IsActive<EternalGarden>())
            {
                font = FontAssets.DeathText.Value;
                text = Main.statusText;
                textColor = Color.Black;
            }

            // Draw a pure-white background. Immediate loading is used for the texture because without it there's a tiny, jarring delay before the white background appears where the regular
            // title screen is revealed momentarily.
            Texture2D pixel = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Pixel", AssetRequestMode.ImmediateLoad).Value;
            Vector2 pixelScale = new Vector2(Main.screenWidth, Main.screenHeight) * 1.45f / pixel.Size();
            Main.spriteBatch.Draw(pixel, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, null, Color.White, 0f, pixel.Size() * 0.5f, pixelScale, 0, 0f);

            // Draw the text.
            Vector2 drawPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - font.MeasureString(text) * 0.5f;
            Main.spriteBatch.DrawString(font, text, drawPosition, textColor * TextOpacity);

            EternalGardenIntroBackgroundFix.ShouldDrawWhite = false;
        }

        public override void CopyMainWorldData()
        {
            // Re-initialize the save data tag.
            savedWorldData = new();

            // Ensure that world save data for Noxus and Xeroc are preserved.
            // Xeroc is obvious, the main world should know if Xeroc was defeated in the subworld.
            // Noxus' defeat is required to use the Terminus, so not having him marked as defeated and thus unable to use it to leave the subworld would be a problem.
            if (HasDefeatedEgg)
                savedWorldData["HasDefeatedEgg"] = true;
            if (HasDefeatedNoxus)
                savedWorldData["HasDefeatedNoxus"] = true;
            if (HasDefeatedXeroc)
                savedWorldData["HasDefeatedXeroc"] = true;
            if (HasMetXeroc)
                savedWorldData["HasMetXeroc"] = true;

            // Save difficulty data. This is self-explanatory.
            bool revengeanceMode = CommonCalamityVariables.RevengeanceModeActive;
            bool deathMode = CommonCalamityVariables.RevengeanceModeActive;
            if (revengeanceMode)
                savedWorldData["RevengeanceMode"] = revengeanceMode;
            if (deathMode)
                savedWorldData["DeathMode"] = deathMode;

            // Save death data. When the player returns to the subworld this will decide how many Starbearers will appear in the garden.
            savedWorldData["XerocDeathCount"] = XerocDeathCount;
        }

        public static void LoadWorldDataFromTag()
        {
            HasDefeatedEgg = savedWorldData.ContainsKey("HasDefeatedEgg");
            HasDefeatedNoxus = savedWorldData.ContainsKey("HasDefeatedNoxus");
            HasDefeatedXeroc = savedWorldData.ContainsKey("HasDefeatedXeroc");
            HasMetXeroc = savedWorldData.ContainsKey("HasMetXeroc");

            CommonCalamityVariables.RevengeanceModeActive = savedWorldData.ContainsKey("RevengeanceMode");
            CommonCalamityVariables.DeathModeActive = savedWorldData.ContainsKey("DeathMode");

            XerocDeathCount = savedWorldData.GetInt("XerocDeathCount");
        }

        public override void ReadCopiedMainWorldData() => LoadWorldDataFromTag();

        public override void CopySubworldData() => CopyMainWorldData();

        public override void ReadCopiedSubworldData() => ReadCopiedMainWorldData();
    }
}
