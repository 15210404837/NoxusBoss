using System.Reflection;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Items.MiscOPTools;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Typeless
{
    public class NoxusSprayerGas : ModProjectile
    {
        public bool PlayerHasMadeIncalculableMistake
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 9;
            Projectile.timeLeft = Projectile.MaxUpdates * 20;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Create gas.
            float particleScale = GetLerpValue(0f, 32f, Time, true);
            var particle = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.9f, 0.9f), Color.MediumPurple, 15, particleScale, particleScale * 0.4f, 0.05f, true, 0f, true)
            {
                Rotation = Main.rand.NextFloat(TwoPi)
            };
            GeneralParticleHandler.SpawnParticle(particle);

            // Get rid of the player if the spray was reflected by Xeroc and it touches the player.
            if (PlayerHasMadeIncalculableMistake && Projectile.Hitbox.Intersects(Main.player[Projectile.owner].Hitbox) && Main.netMode == NetmodeID.SinglePlayer && Time >= 20f)
            {
                Player player = Main.player[Projectile.owner];
                for (int j = 0; j < 20; j++)
                {
                    float gasSize = player.width * Main.rand.NextFloat(0.1f, 0.8f);
                    NoxusGasMetaball.CreateParticle(player.Center + Main.rand.NextVector2Circular(40f, 40f), Main.rand.NextVector2Circular(4f, 4f), gasSize);
                }
                typeof(SubworldSystem).GetField("current", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                typeof(SubworldSystem).GetField("cache", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                NoxusSprayPlayerDeletionSystem.PlayerWasDeleted = true;
            }

            DeleteEverything();

            Time++;
        }

        public void DeleteEverything()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || !n.Hitbox.Intersects(Projectile.Hitbox) || NoxusSprayer.NPCsToNotDelete.Contains(n.type))
                    continue;

                // Reflect the spray if the player has misused it by daring to try and delete Xeroc.
                if (NoxusSprayer.NPCsThatReflectSpray.Contains(n.type))
                {
                    if (!PlayerHasMadeIncalculableMistake && n.Opacity >= 0.02f)
                    {
                        PlayerHasMadeIncalculableMistake = true;
                        Projectile.velocity *= -0.6f;
                        Projectile.netUpdate = true;
                    }
                    continue;
                }

                n.active = false;

                for (int j = 0; j < 20; j++)
                {
                    float gasSize = n.width * Main.rand.NextFloat(0.1f, 0.8f);
                    NoxusGasMetaball.CreateParticle(n.Center + Main.rand.NextVector2Circular(40f, 40f), Main.rand.NextVector2Circular(4f, 4f), gasSize);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
