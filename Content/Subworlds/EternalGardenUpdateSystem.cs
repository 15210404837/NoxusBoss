using CalamityMod;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using static NoxusBoss.Content.Subworlds.EternalGarden;

namespace NoxusBoss.Content.Subworlds
{
    public class EternalGardenUpdateSystem : ModSystem
    {
        public static bool WasInSubworldLastUpdateFrame
        {
            get;
            private set;
        }

        public override void PostUpdateEverything()
        {
            // Reset the text opacity when the game is being played. It will increase up to full opacity during subworld transition drawing.
            TextOpacity = 0f;

            // Verify whether things are in the subworld.
            // TODO -- This might need to be done in another hook. Need to check whether this one is serverside only.
            bool inGarden = SubworldSystem.IsActive<EternalGarden>();
            if (WasInSubworldLastUpdateFrame != inGarden)
            {
                // A major flaw with respect to subworld data transfer is the fact that Calamity's regular OnWorldLoad hooks clear everything.
                // This works well and good for Calamity's purposes, but it causes serious issues when going between subworlds. The result of this is
                // ordered as follows:

                // 1. Exit world. Store necessary data for subworld transfer.
                // 2. Load necessary stuff for subworld and wait.
                // 3. Enter subworld. Load data from step 1.
                // 4. Call OnWorldLoad, resetting everything from step 3.

                // In order to address this, a final step is introduced:
                // 5. Load data from step 3 again on the first frame of entity updating.
                if (inGarden)
                    LoadWorldDataFromTag();

                WasInSubworldLastUpdateFrame = inGarden;
            }

            // Everything beyond this point applies solely to the subworld.
            if (!WasInSubworldLastUpdateFrame)
                return;

            // Keep it perpetually night time if Xeroc is not present.
            if (XerocBoss.Myself is null)
            {
                Main.dayTime = false;
                Main.time = 16200f;
            }

            // Keep the wind strong, so that the plants sway around.
            // This swiftly ceases if Xeroc is present, as though nature is fearful of him.
            if (XerocBoss.Myself is null)
                Main.windSpeedTarget = Lerp(0.88f, 1.32f, CalamityUtils.AperiodicSin(Main.GameUpdateCount * 0.02f) * 0.5f + 0.5f);
            else
                Main.windSpeedTarget = 0f;
            Main.windSpeedCurrent = Lerp(Main.windSpeedCurrent, Main.windSpeedTarget, 0.03f);
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (!WasInSubworldLastUpdateFrame)
                return;

            tileColor = Color.Wheat * 0.3f;
            backgroundColor = new(4, 6, 14);

            // Make everything bright if Xeroc is present.
            tileColor = Color.Lerp(tileColor, Color.White, XerocSky.HeavenlyBackgroundIntensity);
        }
    }
}
