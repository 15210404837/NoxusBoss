using CalamityMod.Systems;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers
{
    public class XerocMusicScene : BaseMusicSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)10;

        public override int NPCType => ModContent.NPCType<XerocBoss>();

        public override int MusicDistance => 100000000;

        public override int VanillaMusic => XerocBoss.Myself?.ModNPC.Music ?? 0;

        public override int? MusicModMusic => VanillaMusic;

        public override int OtherworldMusic => VanillaMusic;
    }
}
