using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class CosmicCrownMetaball : Metaball
    {
        public override MetaballDrawLayerType DrawContext => MetaballDrawLayerType.BeforeBlack;

        public override Color EdgeColor => Color.DarkSlateBlue;

        public override List<Texture2D> Layers => new()
        {
            ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/CosmicCrownLayer").Value
        };

        public override void DrawInstances()
        {
            foreach (NPC xeroc in Main.npc.Where(n => n.active && n.type == ModContent.NPCType<XerocBoss>()))
            {
                if (xeroc.Opacity >= 0.8f)
                    xeroc.ModNPC<XerocBoss>().DrawCrown();
            }
        }
    }
}
