using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Noxus;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.CustomWorldSeeds
{
    public class NoxusWorldManager : ModSystem
    {
        public const string SeedName = "Noxus' Realm";

        public static bool Enabled => CustomWorldSeedManager.IsSeedActive(SeedName);

        public override void OnModLoad()
        {
            CustomWorldSeedManager.RegisterSeed(SeedName, "darknessfalls", "darkness falls");
            On_Main.DrawMenu += DrawNoxusSkyDuringWorldGen;
        }

        private void DrawNoxusSkyDuringWorldGen(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            if (Main.gameMenu && WorldGen.generatingWorld && Enabled)
            {
                // Keep the music silent.
                float newMusicFade = Main.musicFade[Main.curMusic];
                Main.audioSystem.UpdateAmbientCueTowardStopping(Main.curMusic, 1f, ref newMusicFade, 0f);
                Main.musicFade[Main.curMusic] = newMusicFade;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

                // Draw the Noxus sky.
                var sky = (NoxusSky)SkyManager.Instance["NoxusBoss:NoxusSky"];
                sky.Update(gameTime);
                sky.Draw(Main.spriteBatch, 0f, 5f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            }

            orig(self, gameTime);
        }
    }
}
