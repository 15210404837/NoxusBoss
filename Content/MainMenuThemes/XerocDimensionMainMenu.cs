﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.MainMenuThemes
{
    public class XerocDimensionMainMenu : ModMenu
    {
        public static ModMenu Instance
        {
            get;
            private set;
        }

        public override void Load()
        {
            Instance = this;
        }

        public override string DisplayName => "Light Dimension";

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            Main.spriteBatch.Draw(XerocDimensionSkyGenerator.XerocDimensionTarget.Target, Vector2.Zero, Color.White);
            return true;
        }

        public override bool IsAvailable => WorldSaveSystem.HasDefeatedXerocInAnyWorld;
    }
}
