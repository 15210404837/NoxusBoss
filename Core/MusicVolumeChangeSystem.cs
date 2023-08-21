using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class MusicVolumeChangeSystem : ModSystem
    {
        public static float MusicVolumeFactor
        {
            get;
            set;
        } = 1f;

        public override void OnModLoad()
        {
            On_Main.UpdateAudio += MuffleMusic;
        }

        private void MuffleMusic(On_Main.orig_UpdateAudio orig, Main self)
        {
            float originalMusicVolume = Main.musicVolume;
            Main.musicVolume *= MusicVolumeFactor;
            orig(self);
            Main.musicVolume = originalMusicVolume;
            MusicVolumeFactor = 1f;
        }
    }
}
