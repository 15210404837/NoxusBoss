using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Subworlds;
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

        public static float EarRingingIntensity
        {
            get;
            set;
        }

        public static List<SoundStyle> ExemptedSoundStyles => new()
        {
            NoxusEgg.GlitchSound,

            XerocBoss.CosmicLaserStartSound,
            XerocBoss.CosmicLaserLoopSound,
            XerocBoss.EarRingingSound,
            XerocBoss.Phase3TransitionLoopSound,
        };

        public override void OnModLoad()
        {
            On_SoundPlayer.Play_Inner += ReduceVolume;
        }

        private SlotId ReduceVolume(On_SoundPlayer.orig_Play_Inner orig, SoundPlayer self, ref SoundStyle style, Vector2? position, SoundUpdateCallback updateCallback)
        {
            SoundStyle copy = style;

            if (XerocBoss.Myself is null)
                MuffleFactor = 1f;
            if (MuffleFactor < 0.999f && !ExemptedSoundStyles.Any(s => s.IsTheSameAs(copy)) && EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame && XerocBoss.Myself is not null)
                style.Volume *= MuffleFactor;

            SlotId result = orig(self, ref style, position, updateCallback);
            style.Volume = copy.Volume;

            return result;
        }
    }
}
