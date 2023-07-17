using CalamityMod.Rarities;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class DebugXerocSpawner : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Debug Deity of Light Spawner");
            Tooltip.SetDefault("Summons a nameless deity of light");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 34;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ModContent.RarityType<Violet>();
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<XerocBoss>());

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int xerocID = ModContent.NPCType<XerocBoss>();

                // If the player is not in multiplayer, spawn Xeroc.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(player.whoAmI, xerocID);

                // If the player is in multiplayer, request a boss spawn.
                else
                    NetMessage.SendData(MessageID.SpawnBoss, number: player.whoAmI, number2: xerocID);
            }

            return true;
        }
    }
}
