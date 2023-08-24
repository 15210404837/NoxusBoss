using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Core.GlobalItems
{
    public class DialogPlayer : ModPlayer
    {
        public bool HasTalkedToCultist
        {
            get;
            set;
        }

        public List<ulong> SeenCultistDialogIDs
        {
            get;
            set;
        } = new();

        public override void SaveData(TagCompound tag)
        {
            tag["SeenCultistDialogIDs"] = SeenCultistDialogIDs;
            tag["HasTalkedToCultist"] = HasTalkedToCultist;
        }

        public override void LoadData(TagCompound tag)
        {
            SeenCultistDialogIDs = tag.GetList<ulong>("SeenCultistDialogIDs").ToList();
            HasTalkedToCultist = tag.GetBool("HasTalkedToCultist");
        }
    }
}
