using NoxusBoss.Content.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class PurifierExplosionScene : ModSceneEffect
    {
        public override int Music => 0;

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
