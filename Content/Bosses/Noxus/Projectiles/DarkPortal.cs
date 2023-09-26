﻿using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Noxus.Projectiles
{
    public class DarkPortal : ModProjectile, IDrawsWithShader
    {
        public float MaxScale => Projectile.ai[0];

        public ref float Time => ref Projectile.localAI[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public static int MaxUpdates => 1;

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 600;
            Projectile.height = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9600;
            Projectile.MaxUpdates = MaxUpdates;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            // Decide the current scale.
            Projectile.scale = GetLerpValue(0f, MaxUpdates * 25f, Time, true) * GetLerpValue(Lifetime, Lifetime - MaxUpdates * 16f, Time, true);
            Projectile.Opacity = Pow(Projectile.scale, 2.6f);
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Shoot projectiles if Noxus' egg is present or the Entropic God is using its dedicated portal attack.
            bool canShootGas = NPC.AnyNPCs(ModContent.NPCType<NoxusEgg>());
            bool canShootComet = EntropicGod.Myself is not null && EntropicGod.Myself.ModNPC<EntropicGod>().CurrentAttack == EntropicGod.EntropicGodAttackType.OrganizedPortalCometBursts;
            if (Time == (int)(Lifetime * MaxUpdates * 0.5f) - 10f && (canShootGas || canShootComet))
            {
                SoundEngine.PlaySound(SoundID.Item103, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (canShootGas)
                    {
                        Vector2 gasShootVelocity = Projectile.velocity.RotatedByRandom(0.16f) * 8f;

                        // Shoot in the opposite direction if the player has gone behind the portal.
                        Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                        if (Vector2.Dot(closest.Center - Projectile.Center, gasShootVelocity) < 0f)
                            gasShootVelocity *= -1f;

                        NewProjectileBetter(Projectile.Center + gasShootVelocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * 0.9f, gasShootVelocity, ModContent.ProjectileType<NoxusGas>(), EntropicGod.NoxusGasDamage, 0f);
                    }
                    else if (canShootComet)
                    {
                        Vector2 cometShootVelocity = Projectile.velocity.RotatedByRandom(0.16f) * 3.7f;
                        NewProjectileBetter(Projectile.Center + cometShootVelocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * 0.9f, cometShootVelocity, ModContent.ProjectileType<DarkComet>(), EntropicGod.NoxusGasDamage, 0f);
                    }
                }

                // Release a bunch of gas particles.
                for (int i = 0; i < 30; i++)
                    NoxusGasMetaball.CreateParticle(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), Projectile.velocity.RotatedByRandom(0.68f) * Main.rand.NextFloat(19f), Main.rand.NextFloat(13f, 56f));
            }

            if (Time >= Lifetime)
            {
                SoundEngine.PlaySound(EntropicGod.TwinkleSound with { Volume = 0.3f, MaxInstances = 20 }, Projectile.Center);
                TwinkleParticle twinkle = new(Projectile.Center, Vector2.Zero, Color.LightCyan, 30, 6, Vector2.One * MaxScale * 1.3f);
                GeneralParticleHandler.SpawnParticle(twinkle);
                Projectile.Kill();
            }

            if (Projectile.scale > 0.7f && Time < Lifetime - 60f && Projectile.FinalExtraUpdate())
            {
                // Create particles that converge in on the portal.
                for (int i = 0; i < 3; i++)
                {
                    Vector2 lightAimPosition = Projectile.Center + Projectile.velocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * Projectile.scale * 50f + Main.rand.NextVector2Circular(10f, 10f);
                    Vector2 lightSpawnPosition = Projectile.Center + Projectile.velocity * 75f + Projectile.velocity.RotatedByRandom(2.83f) * Main.rand.NextFloat(700f);
                    Vector2 lightVelocity = (lightAimPosition - lightSpawnPosition) * 0.06f;
                    SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, 0.33f, Color.Pink, 19, 0.04f, 3f, 8f);
                    GeneralParticleHandler.SpawnParticle(light);
                }

                // Create smoke particles that fly out of the portal.
                for (int i = 0; i < 2; i++)
                {
                    Vector2 smokeSpawnPosition = Projectile.Center + Projectile.velocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * Projectile.scale * 120f + Main.rand.NextVector2Circular(20f, 20f);
                    MediumMistParticle light = new(smokeSpawnPosition, Projectile.velocity * Main.rand.NextFloat(0.5f, 28f), Color.Fuchsia, Color.Transparent, Main.rand.NextFloat(0.7f, 1.75f), 150f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            var portalShader = ShaderManager.GetShader("PortalShader");
            portalShader.TrySetParameter("generalColor", Color.White.ToVector3());
            portalShader.TrySetParameter("circleStretchInterpolant", Projectile.scale);
            portalShader.TrySetParameter("transformation", Matrix.CreateScale(3f, 1f, 1f));
            portalShader.TrySetParameter("aimDirection", Projectile.velocity);
            portalShader.TrySetParameter("edgeFadeInSharpness", 20.3f);
            portalShader.TrySetParameter("aheadCircleMoveBackFactor", 0.67f);
            portalShader.TrySetParameter("aheadCircleZoomFactor", 0.9f);
            portalShader.TrySetParameter("spaceTextureZoom", Vector2.One);
            portalShader.SetTexture(StarDistanceLookup, 1);
            portalShader.SetTexture(TurbulentNoise, 2);
            portalShader.SetTexture(VoidTexture, 3);
            portalShader.SetTexture(PerlinNoise, 4);
            portalShader.SetTexture(SpikesTexture, 5);
            portalShader.Apply();

            Vector2 textureArea = Projectile.Size / WhitePixel.Size() * MaxScale;
            textureArea *= 1f + Cos(Main.GlobalTimeWrappedHourly * 15f + Projectile.identity) * 0.012f;
            spriteBatch.Draw(WhitePixel, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.MediumPurple), Projectile.rotation, WhitePixel.Size() * 0.5f, textureArea, 0, 0f);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
