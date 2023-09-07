using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc;
using ReLogic.Utilities;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class SoundMufflingSystem : ModSystem
    {
        public static float MuffleFactor
        {
            get;
            set;
        } = 1f;

        public static List<SoundStyle> ExemptedSoundStyles => new()
        {
            XerocBoss.CosmicLaserStartSound,
            XerocBoss.CosmicLaserLoopSound,
        };

        public override void OnModLoad()
        {
            On_SoundPlayer.Play_Inner += ReduceVolume;
        }

        private SlotId ReduceVolume(On_SoundPlayer.orig_Play_Inner orig, SoundPlayer self, ref SoundStyle style, Vector2? position, SoundUpdateCallback updateCallback)
        {
            SoundStyle copy = style;
            if (MuffleFactor < 0.999f && !ExemptedSoundStyles.Any(s => s.IsTheSameAs(copy)))
                style.Volume *= MuffleFactor;

            SlotId result = orig(self, ref style, position, updateCallback);
            style.Volume = copy.Volume;

            return result;
        }
    }
}
