using System;
using System.Collections.Generic;
using CalamityMod.Items.SummonItems;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Projectiles;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core
{
    public class NoxusGlobalItem : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            // Replace Terminus' projectile with a custom one that has nothing to do with Boss Rush.
            if (item.type == ModContent.ItemType<Terminus>())
            {
                item.shoot = ModContent.ProjectileType<TerminusProj>();
                item.channel = false;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            #region Modular Tooltip Editing Code
            // This is a modular tooltip editor which loops over all tooltip lines of an item,
            // selects all those which match an arbitrary function you provide,
            // then edits them using another arbitrary function you provide.
            void ApplyTooltipEdits(IList<TooltipLine> lines, Func<Item, TooltipLine, bool> predicate, Action<TooltipLine> action)
            {
                foreach (TooltipLine line in lines)
                    if (predicate.Invoke(item, line))
                        action.Invoke(line);
            }

            // This function produces simple predicates to match a specific line of a tooltip, by number/index.
            Func<Item, TooltipLine, bool> LineNum(int n) => (Item i, TooltipLine l) => l.Mod == "Terraria" && l.Name == $"Tooltip{n}";

            // This function is shorthand to invoke ApplyTooltipEdits using the above predicates.
            void EditTooltipByNum(int lineNum, Action<TooltipLine> action) => ApplyTooltipEdits(tooltips, LineNum(lineNum), action);
            #endregion

            #region Tooltip Edits

            // Alter the Terminus' text.
            if (item.type == ModContent.ItemType<Terminus>())
            {
                EditTooltipByNum(0, line => line.Text = Language.GetTextValue("Mods.NoxusBoss.Items.Terminus.BaseTooltip"));
                EditTooltipByNum(1, line =>
                {
                    if (WorldSaveSystem.HasDefeatedNoxus)
                    {
                        line.OverrideColor = new(240, 76, 76);
                        line.Text = Language.GetTextValue("Mods.NoxusBoss.Items.Terminus.OpenedTooltip");
                    }
                    else
                    {
                        line.OverrideColor = new(239, 174, 174);
                        line.Text = Language.GetTextValue("Mods.NoxusBoss.Items.Terminus.UnopenedTooltip");
                    }
                });
            }
            #endregion
        }

        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // Let Terminus use its closed eye form if Noxus is not yet defeated.
            if (item.type == ModContent.ItemType<Terminus>() && !WorldSaveSystem.HasDefeatedNoxus)
            {
                Texture2D texture = ModContent.Request<Texture2D>("NoxusBoss/Content/Items/TerminusClosedEye").Value;
                spriteBatch.Draw(texture, position, null, Color.White, 0f, origin, scale, 0, 0);
                return false;
            }

            return true;
        }

        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            // Let Terminus use its closed eye form if Noxus is not yet defeated.
            if (item.type == ModContent.ItemType<Terminus>() && !WorldSaveSystem.HasDefeatedNoxus)
            {
                Texture2D texture = ModContent.Request<Texture2D>("NoxusBoss/Content/Items/TerminusClosedEye").Value;
                spriteBatch.Draw(texture, item.position - Main.screenPosition, null, lightColor, 0f, Vector2.Zero, 1f, 0, 0);
                return false;
            }

            return true;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            // Make the Terminus only usable after Noxus. Also disallow it being usable to create multiple instances of Terminus in the world. That'd be weird.
            if (item.type == ModContent.ItemType<Terminus>())
                return WorldSaveSystem.HasDefeatedNoxus && player.ownedProjectileCounts[ModContent.ProjectileType<TerminusProj>()] <= 0;

            return true;
        }
    }
}
