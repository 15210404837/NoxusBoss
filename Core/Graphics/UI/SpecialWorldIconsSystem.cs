using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace NoxusBoss.Core.Graphics.UI
{
    public class SpecialWorldIconsSystem : ModSystem
    {
        public override void OnModLoad()
        {
            Main.QueueMainThreadAction(() =>
            {
                On.Terraria.GameContent.UI.Elements.UIWorldListItem.GetIcon += UsePostXerocIcon;
            });
        }

        private Asset<Texture2D> UsePostXerocIcon(On.Terraria.GameContent.UI.Elements.UIWorldListItem.orig_GetIcon orig, UIWorldListItem self)
        {
            WorldFileData worldData = (WorldFileData)typeof(UIWorldListItem).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self);
            byte[] worldBinary = FileUtilities.ReadAllBytes(worldData.Path.Replace(".wld", ".twld"), worldData.IsCloudSave);

            // Verify that the world header is not corrupted.
            if (worldBinary[0] != 0x1F || worldBinary[1] != 0x8B)
                return orig(self);

            // Acquire world tag data and check if Xeroc is marked as dead within it.
            using (MemoryStream stream = new(worldBinary))
            {
                var tag = TagIO.FromStream(stream);
                bool xerocHasBeenDefeated = ContainsKeyRecursive(tag, "HasDefeatedXeroc");
                if (xerocHasBeenDefeated)
                    return ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/UI/IconPostXeroc", AssetRequestMode.ImmediateLoad);
            }

            return orig(self);
        }

        private static bool ContainsKeyRecursive(TagCompound tag, string key)
        {
            if (tag.ContainsKey(key))
                return true;

            var childTags = tag.Where(kv => kv.Value is TagCompound).Select(kv => (TagCompound)kv.Value);
            var childTagLists = tag.Where(kv => kv.Value is List<TagCompound>).Select(kv => (List<TagCompound>)kv.Value);

            // Check all associated tags within this tag to see if they contain the desired key.
            if (childTags.Any(t => ContainsKeyRecursive(t, key)))
                return true;
            if (childTagLists.Any(t => t.Any(t2 => ContainsKeyRecursive(t2, key))))
                return true;

            return false;
        }
    }
}
