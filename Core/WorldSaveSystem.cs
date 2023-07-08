using System.IO;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core
{
    public class WorldSaveSystem : ModSystem
    {
        // This field is used for performance reasons, since it could be a bit unideal to be doing file existence checks many tiles every frame.
        private static bool? hasDefeatedXerocInAnyWorldField;

        public static bool HasDefeatedEgg
        {
            get;
            set;
        }

        public static bool HasMetXeroc
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
                    pathWriter.WriteLine("The contents of this file don't matter, just that the file exists. Delete it if you want Xeroc to not be marked is defeated.");
                    pathWriter.Close();
                }
            }
        }

        public static string XerocDefeatConfirmationFilePath => Main.SavePath + "\\XerocDefeatConfirmation.txt";

        public override void OnWorldLoad()
        {
            HasDefeatedEgg = false;
            HasMetXeroc = false;
            NoxusEggCutsceneSystem.HasSummonedNoxus = false;
        }

        public override void OnWorldUnload()
        {
            HasDefeatedEgg = false;
            HasMetXeroc = false;
            NoxusEggCutsceneSystem.HasSummonedNoxus = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (HasDefeatedEgg)
                tag["HasDefeatedEgg"] = true;
            if (NoxusEggCutsceneSystem.HasSummonedNoxus)
                tag["HasSummonedNoxus"] = true;
            if (HasMetXeroc)
                tag["HasMetXeroc"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            HasDefeatedEgg = tag.ContainsKey("HasDefeatedEgg");
            NoxusEggCutsceneSystem.HasSummonedNoxus = tag.ContainsKey("HasSummonedNoxus");
            HasMetXeroc = tag.ContainsKey("HasMetXeroc");
        }

        public override void NetSend(BinaryWriter writer)
        {
            BitsByte b1 = new();
            b1[0] = HasDefeatedEgg;
            b1[1] = HasMetXeroc;
            b1[2] = NoxusEggCutsceneSystem.HasSummonedNoxus;

            writer.Write(b1);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte b1 = reader.ReadByte();
            HasDefeatedEgg = b1[0];
            HasMetXeroc = b1[1];
            NoxusEggCutsceneSystem.HasSummonedNoxus = b1[2];
        }
    }
}
