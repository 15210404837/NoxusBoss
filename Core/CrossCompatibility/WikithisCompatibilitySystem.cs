using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class WikithisCompatibilitySystem : ModSystem
    {
        public static bool CompatibilityEnabled => false;

        public const string WikiURL = "https://terrariamods.wiki.gg/wiki/{}";

        public override void PostSetupContent()
        {
            // Wikithis is clientside, and should not be accessed on servers.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Don't load anything if Wikithis is not enabled.
            if (Wikithis is null)
                return;

            // Don't load if compatibility is disabled.
            // TODO -- At the time of writing this, the wiki is far from finished. I am not comfortable sending players to something that is not ready for usage yet, and as such it is currently disabled.
            if (!CompatibilityEnabled)
                return;

            // Register the wiki URL.
            Wikithis.Call("AddModURL", Mod, WikiURL);

            // Register the wiki texture.
            Wikithis.Call("AddWikiTexture", Mod, ModContent.Request<Texture2D>("NoxusBoss/Core/CrossCompatibility/WikiThisIcon"));

            // Clear up name conflicts.
            static void EnemyRedirect(int npcID, string pageName)
            {
                Wikithis.Call("NPCIDReplacement", npcID, pageName);
            }
            EnemyRedirect(ModContent.NPCType<XerocBoss>(), "Nameless Deity of Light");
        }
    }
}
