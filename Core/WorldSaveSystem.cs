using System.IO;
using NoxusBoss.Content.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core
{
    public class WorldSaveSystem : ModSystem
    {
        // This field is used for performance reasons, since it could be a bit unideal to be doing file existence checks many times every frame.
        private static bool? hasDefeatedXerocInAnyWorldField;

        // Toasty's QoL mod needs fields to access things. TECHNICALLY it's possible to attempt to get the compiler-generated backer field with hardcoded
        // strings and wacky reflection, but that's kind of unpleasant and something I'd rather not do.
        private static bool hasDefeatedXeroc;

        private static bool hasDefeatedNoxus;

        public static int XerocDeathCount
        {
            get;
            set;
        }

        public static bool HasDefeatedEgg
        {
            get;
            set;
        }

        public static bool HasDefeatedNoxus
        {
            get => hasDefeatedNoxus;
            set
            {
                if (hasDefeatedNoxus != value)
                {
                    if (value && !Main.gameMenu && !Main.zenithWorld)
                        NoxusDefeatAnimationSystem.Start();

                    hasDefeatedNoxus = value;
                }
            }
        }

        public static bool HasDefeatedXeroc
        {
            get => hasDefeatedXeroc;
            set => hasDefeatedXeroc = value;
        }

        public static bool HasMetXeroc
        {
            get;
            set;
        }

        public static bool HasPlacedCattail
        {
            get;
            set;
        }

        public static bool HasDefeatedXerocInAnyWorld
        {
            get
            {
                hasDefeatedXerocInAnyWorldField ??= hasDefeatedXerocInAnyWorldField = File.Exists(XerocDefeatConfirmationFilePath);
                return hasDefeatedXerocInAnyWorldField.Value;
            }
            set
            {
                hasDefeatedXerocInAnyWorldField = value;
                if (!value)
                    File.Delete(XerocDefeatConfirmationFilePath);
                else
                {
                    var pathWriter = File.CreateText(XerocDefeatConfirmationFilePath);
                    pathWriter.WriteLine("The contents of this file don't matter, just that the file exists. Delete it if you want Xeroc to not be marked as defeated.");
                    pathWriter.Close();
                }
            }
        }

        public static bool OgsculeRulesOverTheUniverse
        {
            get;
            set;
        }

        public static string XerocDefeatConfirmationFilePath => Main.SavePath + "\\XerocDefeatConfirmation.txt";

        public override void OnWorldLoad()
        {
            XerocBoss.Myself = null;
            if (SubworldSystem.AnyActive())
                return;

            XerocDeathCount = 0;
            HasDefeatedEgg = false;
            hasDefeatedNoxus = false;
            HasDefeatedXeroc = false;
            HasMetXeroc = false;
            OgsculeRulesOverTheUniverse = false;
            HasPlacedCattail = false;
            NoxusEggCutsceneSystem.NoxusHasFallenFromSky = false;
        }

        public override void OnWorldUnload()
        {
            if (SubworldSystem.AnyActive())
                return;

            XerocDeathCount = 0;
            HasDefeatedEgg = false;
            hasDefeatedNoxus = false;
            HasDefeatedXeroc = false;
            HasMetXeroc = false;
            OgsculeRulesOverTheUniverse = false;
            HasPlacedCattail = false;
            NoxusEggCutsceneSystem.NoxusHasFallenFromSky = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (HasDefeatedEgg)
                tag["HasDefeatedEgg"] = true;
            if (hasDefeatedNoxus)
                tag["HasDefeatedNoxus"] = true;
            if (HasDefeatedXeroc)
                tag["HasDefeatedXeroc"] = true;
            if (NoxusEggCutsceneSystem.NoxusHasFallenFromSky)
                tag["NoxusHasFallenFromSky"] = true;
            if (HasMetXeroc)
                tag["HasMetXeroc"] = true;
            if (OgsculeRulesOverTheUniverse)
                tag["OgsculeRulesOverTheUniverse"] = true;
            if (HasPlacedCattail)
                tag["HasPlacedCattail"] = true;

            tag["XerocDeathCount"] = XerocDeathCount;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            HasDefeatedEgg = tag.ContainsKey("HasDefeatedEgg");
            hasDefeatedNoxus = tag.ContainsKey("HasDefeatedNoxus");
            HasDefeatedXeroc = tag.ContainsKey("HasDefeatedXeroc");
            NoxusEggCutsceneSystem.NoxusHasFallenFromSky = tag.ContainsKey("NoxusHasFallenFromSky");
            HasMetXeroc = tag.ContainsKey("HasMetXeroc");
            OgsculeRulesOverTheUniverse = tag.ContainsKey("OgsculeRulesOverTheUniverse");
            HasPlacedCattail = tag.ContainsKey("HasPlacedCattail");

            XerocDeathCount = tag.GetInt("XerocDeathCount");
        }

        public override void NetSend(BinaryWriter writer)
        {
            BitsByte b1 = new();
            b1[0] = HasDefeatedEgg;
            b1[1] = hasDefeatedNoxus;
            b1[2] = HasDefeatedXeroc;
            b1[3] = HasMetXeroc;
            b1[4] = OgsculeRulesOverTheUniverse;
            b1[5] = HasPlacedCattail;
            b1[6] = NoxusEggCutsceneSystem.NoxusHasFallenFromSky;

            writer.Write(b1);
            writer.Write(XerocDeathCount);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte b1 = reader.ReadByte();
            HasDefeatedEgg = b1[0];
            hasDefeatedNoxus = b1[1];
            HasDefeatedXeroc = b1[2];
            HasMetXeroc = b1[3];
            OgsculeRulesOverTheUniverse = b1[4];
            HasPlacedCattail = b1[5];
            NoxusEggCutsceneSystem.NoxusHasFallenFromSky = b1[6];

            XerocDeathCount = reader.ReadInt32();
        }
    }
}
