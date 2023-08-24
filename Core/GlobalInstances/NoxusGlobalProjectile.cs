using CalamityMod.Projectiles.Melee;
using NoxusBoss.Content.Subworlds;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalProjectile : GlobalProjectile
    {
        public override bool PreAI(Projectile projectile)
        {
            // Prevent tombs from cluttering things up in the eternal garden.
            if (EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
            {
                bool isTomb = projectile.type is ProjectileID.Tombstone or ProjectileID.Gravestone or ProjectileID.RichGravestone1 or ProjectileID.RichGravestone2 or
                    ProjectileID.RichGravestone3 or ProjectileID.RichGravestone4 or ProjectileID.RichGravestone4 or ProjectileID.Headstone or ProjectileID.Obelisk or
                    ProjectileID.GraveMarker or ProjectileID.CrossGraveMarker or ProjectileID.Headstone;
                if (isTomb)
                    projectile.active = false;

                // Disallow the crystal crusher ray in the eternal garden as well, since it can be used to break tiles.
                if (projectile.type == ModContent.ProjectileType<CrystylCrusherRay>())
                    projectile.active = false;
            }

            return true;
        }
    }
}
