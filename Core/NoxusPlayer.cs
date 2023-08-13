using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core
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

                dummyXeroc.NPCLoot();
                dummyXeroc.active = false;

                GiveXerocLootUponReenteringWorld = false;
            }
        }
    }
}
