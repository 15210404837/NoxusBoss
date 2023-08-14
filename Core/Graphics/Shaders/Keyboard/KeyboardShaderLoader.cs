using System;
using ReLogic.Peripherals.RGB;
using Terraria.GameContent.RGB;
using Terraria;
using Terraria.ModLoader;
using NoxusBoss.Content.Bosses.Noxus;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria.ID;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Core.Graphics.Shaders.Keyboard
{
    public class KeyboardShaderLoader : ModSystem
    {
        public class SimpleCondition : CommonConditions.ConditionBase
        {
            private readonly Func<Player, bool> _condition;

            public SimpleCondition(Func<Player, bool> condition) => _condition = condition;

            public override bool IsActive() => _condition(CurrentPlayer);
        }

        private static readonly List<ChromaShader> loadedShaders = new();

        public static bool HasLoaded
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Allow for custom boss tracking with the keyboard shader system.
            On_NPC.UpdateRGBPeriheralProbe += TrackCustomBosses;
        }

        public override void PostUpdateWorld()
        {
            if (HasLoaded || Main.netMode == NetmodeID.Server)
                return;

            // Register shaders.
            Color fogColor = Color.Lerp(Color.MediumPurple, Color.DarkGray, 0.85f);
            RegisterShader(new NoxusKeyboardShader(Color.MediumPurple, Color.Black, fogColor), NoxusKeyboardShader.IsActive, ShaderLayer.Boss);
            RegisterShader(new XerocKeyboardShader(), XerocKeyboardShader.IsActive, ShaderLayer.Boss);

            HasLoaded = true;
        }

        public override void OnWorldLoad()
        {
            // Manually remove all shaders from the central registry.
            foreach (ChromaShader loadedShader in loadedShaders)
                Main.Chroma.UnregisterShader(loadedShader);
        }

        private void TrackCustomBosses(On_NPC.orig_UpdateRGBPeriheralProbe orig)
        {
            orig();

            // Noxus.
            if (EntropicGod.Myself is not null || NPC.AnyNPCs(ModContent.NPCType<NoxusEgg>()))
                CommonConditions.Boss.HighestTierBossOrEvent = ModContent.NPCType<EntropicGod>();

            // Xeroc.
            if (XerocBoss.Myself is not null)
                CommonConditions.Boss.HighestTierBossOrEvent = ModContent.NPCType<XerocBoss>();
        }

        private static void RegisterShader(ChromaShader keyboardShader, ChromaCondition condition, ShaderLayer layer)
        {
            Main.QueueMainThreadAction(() =>
            {
                Main.Chroma.RegisterShader(keyboardShader, condition, layer);
                loadedShaders.Add(keyboardShader);
            });
        }
    }
}
