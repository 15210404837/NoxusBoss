using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Music
{
    public class MusicVolumeManipulationSystem : ModSystem
    {
        public static float MusicMuffleFactor
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On_Main.UpdateAudio += MakeMusicShutUp;
        }

        private void MakeMusicShutUp(On_Main.orig_UpdateAudio orig, Main self)
        {
            if (MusicMuffleFactor >= 0.99f)
            {
                Main.musicFade[Main.curMusic] = 0f;
                Main.musicFade[Main.newMusic] = 0f;
                Main.curMusic = 0;
                Main.newMusic = 0;
            }

            if (MusicMuffleFactor < 0.99f)
                orig(self);

            if (MusicMuffleFactor >= 0.0001f)
            {
                for (int i = 0; i < Main.musicFade.Length; i++)
                {
                    float volume = Main.musicFade[i] * Main.musicVolume * Clamp(1f - MusicMuffleFactor, 0f, 1f);
                    float tempFade = Main.musicFade[i];

                    if (volume <= 0f && tempFade <= 0f)
                        continue;

                    for (int j = 0; j < 50; j++)
                    {
                        Main.audioSystem.UpdateCommonTrackTowardStopping(i, volume, ref tempFade, Main.musicFade[i] >= 0.5f);
                        Main.musicFade[i] = tempFade;
                    }
                }
                Main.audioSystem.UpdateAudioEngine();

                // Make the music muffle factor naturally dissipate.
                MusicMuffleFactor = Clamp(MusicMuffleFactor * 0.93f - 0.03f, 0f, 1f);
            }
        }
    }
}
