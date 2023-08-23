using NoxusBoss.Content.Projectiles;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class PurifierExplosionScene : ModSceneEffect
    {
        public override int Music
        {
            get
            {
                if (TotalWhiteOverlaySystem.TimeSinceMonologueBegan >= 210)
                    return MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/PurifierElevatorMusic");

                return 0;
            }
        }

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<ThePurifierProj>()] >= 1)
                return true;
            if (!Main.gameMenu && WorldGen.generatingWorld)
                return true;

            return false;
        }
    }
}
