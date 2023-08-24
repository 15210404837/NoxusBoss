using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.SummonItems
{
    public class BossRushStarter : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 70;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Blue;
        }

        public override bool? UseItem(Player player)
        {
            if (BossRushEvent.BossRushActive)
                BossRushEvent.End();
            else
            {
                BossRushEvent.SyncStartTimer(BossRushEvent.StartEffectTotalTime);
                for (int doom = 0; doom < Main.maxNPCs; doom++)
                {
                    NPC n = Main.npc[doom];
                    if (!n.active)
                        continue;

                    // Will also correctly despawn EoW because none of his segments are boss flagged.
                    bool shouldDespawn = n.boss || n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsTail || n.type == ModContent.NPCType<Draedon>();
                    if (shouldDespawn)
                    {
                        n.active = false;
                        n.netUpdate = true;
                    }
                }

                BossRushEvent.BossRushStage = 0;
                BossRushEvent.BossRushActive = true;
                if (Main.netMode == NetmodeID.Server)
                {
                    CalamityNetcode.SyncWorld();

                    var netMessage = Mod.GetPacket();
                    netMessage.Write((byte)CalamityModMessageType.BossRushStage);
                    netMessage.Write(BossRushEvent.BossRushStage);
                    netMessage.Send();
                }
            }

            return true;
        }
    }
}
