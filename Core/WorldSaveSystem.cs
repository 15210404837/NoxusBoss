using System.IO;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Core.Graphics;
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

        private static bool hasDefeatedNoxus;

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
            get;
            set;
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

            HasDefeatedEgg = false;
            hasDefeatedNoxus = false;
            HasDefeatedXeroc = false;
            HasMetXeroc = false;
            OgsculeRulesOverTheUniverse = false;
            HasPlacedCattail = false;
            NoxusEggCutsceneSystem.HasSummonedNoxus = false;
        }

        public override void OnWorldUnload()
        {
            if (SubworldSystem.AnyActive())
                return;

            HasDefeatedEgg = false;
            hasDefeatedNoxus = false;
            HasDefeatedXeroc = false;
            HasMetXeroc = false;
            OgsculeRulesOverTheUniverse = false;
            HasPlacedCattail = false;
            NoxusEggCutsceneSystem.HasSummonedNoxus = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (HasDefeatedEgg)
                tag["HasDefeatedEgg"] = true;
            if (hasDefeatedNoxus)
                tag["HasDefeatedNoxus"] = true;
            if (HasDefeatedXeroc)
                tag["HasDefeatedXeroc"] = true;
            if (NoxusEggCutsceneSystem.HasSummonedNoxus)
                tag["HasSummonedNoxus"] = true;
            if (HasMetXeroc)
                tag["HasMetXeroc"] = true;
            if (OgsculeRulesOverTheUniverse)
                tag["OgsculeRulesOverTheUniverse"] = true;
            if (HasPlacedCattail)
                tag["HasPlacedCattail"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            HasDefeatedEgg = tag.ContainsKey("HasDefeatedEgg");
            hasDefeatedNoxus = tag.ContainsKey("HasDefeatedNoxus");
            HasDefeatedXeroc = tag.ContainsKey("HasDefeatedXeroc");
            NoxusEggCutsceneSystem.HasSummonedNoxus = tag.ContainsKey("HasSummonedNoxus");
            HasMetXeroc = tag.ContainsKey("HasMetXeroc");
            OgsculeRulesOverTheUniverse = tag.ContainsKey("OgsculeRulesOverTheUniverse");
            HasPlacedCattail = tag.ContainsKey("HasPlacedCattail");
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
            b1[6] = NoxusEggCutsceneSystem.HasSummonedNoxus;

            writer.Write(b1);
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
            NoxusEggCutsceneSystem.HasSummonedNoxus = b1[6];
        }
    }
}
