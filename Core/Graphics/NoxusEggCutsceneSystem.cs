﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class NoxusEggCutsceneSystem : ModSystem
    {
        public static bool WillTryToSummonNoxusTonight
        {
            get;
            set;
        }

        public static bool HasSummonedNoxus
        {
            get;
            set;
        }

        public static bool NoxusBeganOrbitingPlanet => Main.hardMode;

        public static bool NoxusCanCommitSkydivingFromSpace => NPC.downedMoonlord;

        public static string PostWoFDefeatText => "A mysterious object has begun orbiting the planet...";

        public static string PostMLNightText => "A dark presence approaches...";

        public static string FinalMainBossDefeatText => $"{(HasSummonedNoxus ? "The" : "A")} dark presence is stirring...";

        public static List<Player> PlayersOnSurface
        {
            get
            {
                List<Player> surfacePlayers = new();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (!p.active || p.dead || !p.ShoppingZone_Forest || p.ZoneSkyHeight)
                        continue;

                    surfacePlayers.Add(p);
                }

                return surfacePlayers;
            }
        }

        public override void PreUpdateWorld()
        {
            // Randomly make Noxus appear.
            if ((int)Math.Round(Main.time) == 10 && !HasSummonedNoxus && NoxusCanCommitSkydivingFromSpace && Main.rand.NextBool(3) && PlayersOnSurface.Any() && !Main.dayTime)
            {
                BroadcastText(PostMLNightText, new(50, 255, 130));
                WillTryToSummonNoxusTonight = true;
            }

            // Randomly spawn Noxus.
            if (WillTryToSummonNoxusTonight && Main.rand.NextBool(7200) && PlayersOnSurface.Any() && !HasSummonedNoxus)
            {
                Player playerToSpawnNear = Main.rand.Next(PlayersOnSurface);
                NPC.NewNPC(new EntitySource_WorldEvent(), (int)playerToSpawnNear.Center.X, (int)playerToSpawnNear.Center.Y - 1200, ModContent.NPCType<NoxusEggCutscene>(), 1);

                HasSummonedNoxus = true;
            }

            // Try again later if Noxus couldn't spawn at night.
            if (Main.dayTime)
                WillTryToSummonNoxusTonight = false;
        }
    }
}
