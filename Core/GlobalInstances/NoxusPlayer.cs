using System.Collections.Generic;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Particles;
using NoxusBoss.Content.Projectiles.Visuals;
using NoxusBoss.Content.Subworlds;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusPlayer : ModPlayer
    {
        public bool GiveXerocLootUponReenteringWorld;

        private readonly Dictionary<string, object> localValues = new();

        public delegate void ResetEffectsDelegate(NoxusPlayer p);

        public static event ResetEffectsDelegate ResetEffectsEvent;

        private void VerifyValueExists<T>(string key) where T : struct
        {
            if (!localValues.TryGetValue(key, out object value) || value is not T)
                localValues[key] = default(T);
        }

        public T GetValue<T>(string key) where T : struct
        {
            VerifyValueExists<T>(key);
            return (T)localValues[key];
        }

        public void SetValue<T>(string key, T value) where T : struct
        {
            VerifyValueExists<T>(key);
            localValues[key] = value;
        }

        public override void ResetEffects()
        {
            ResetEffectsEvent?.Invoke(this);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["GiveXerocLootUponReenteringWorld"] = GiveXerocLootUponReenteringWorld;
        }

        public override void LoadData(TagCompound tag)
        {
            GiveXerocLootUponReenteringWorld = tag.TryGet("GiveXerocLootUponReenteringWorld", out bool result) && result;
        }

        public override void PostUpdate()
        {
            // Give the player loot if they're entitled to it.
            if (GiveXerocLootUponReenteringWorld)
            {
                NPC dummyXeroc = new();
                dummyXeroc.SetDefaults(ModContent.NPCType<XerocBoss>());
                dummyXeroc.Center = Player.Center - Vector2.UnitY * 600f;
                for (int i = 0; i < 600; i++)
                {
                    if (!Collision.SolidCollision(dummyXeroc.Center, 1, 1))
                        break;

                    dummyXeroc.position.Y++;
                }

                Main.BestiaryTracker.Kills.RegisterKill(dummyXeroc);

                dummyXeroc.NPCLoot();
                dummyXeroc.active = false;

                GiveXerocLootUponReenteringWorld = false;
                WorldSaveSystem.HasDefeatedXeroc = true;
            }

            // Create pale duckweed in the water if the player is in the eternal garden.
            if (EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame && XerocBoss.Myself is null && Main.rand.NextBool(3))
            {
                for (int tries = 0; tries < 50; tries++)
                {
                    Vector2 potentialSpawnPosition = Player.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(200f, 1500f);
                    if (Collision.SolidCollision(potentialSpawnPosition, 1, 1) || !Collision.WetCollision(potentialSpawnPosition, 1, 1))
                        continue;

                    Vector2 spawnVelocity = -Vector2.UnitY.RotatedByRandom(0.82f) * Main.rand.NextFloat(0.5f, 1.35f);
                    Color duckweedColor = Color.Lerp(Color.Wheat, Color.Red, Main.rand.NextFloat(0.52f));
                    PaleDuckweed duckweed = new(potentialSpawnPosition, spawnVelocity, duckweedColor, 540);
                    GeneralParticleHandler.SpawnParticle(duckweed);
                    break;
                }
            }

            // Create wind if the player is in the eternal garden.
            if (Main.myPlayer == Player.whoAmI && EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame && XerocBoss.Myself is null && Main.rand.NextBool(3))
            {
                Vector2 windVelocity = Vector2.UnitX * Main.windSpeedTarget * Main.rand.NextFloat(10f, 14f);
                for (int tries = 0; tries < 50; tries++)
                {
                    Vector2 potentialSpawnPosition = Player.Center + new Vector2(Sign(windVelocity.X) * -Main.rand.NextFloat(950f, 1150f), Main.rand.NextFloatDirection() * 900f);
                    if (Collision.SolidCollision(potentialSpawnPosition, 1, 120) || Collision.WetCollision(potentialSpawnPosition, 1, 120))
                        continue;

                    Projectile.NewProjectile(new EntitySource_WorldEvent(), potentialSpawnPosition, windVelocity, ModContent.ProjectileType<WindStreakVisual>(), 0, 0f, Player.whoAmI);
                    break;
                }
            }
        }
    }
}
