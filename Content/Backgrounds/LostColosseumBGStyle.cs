﻿using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Backgrounds
{
    public class LostColosseumBGStyle : ModUndergroundBackgroundStyle
    {
        // The player will never naturally see this.
        public override void FillTextureArray(int[] textureSlots)
        {
            for (int i = 0; i <= 3; i++)
                textureSlots[i] = BackgroundTextureLoader.GetBackgroundSlot("CalamityMod/Backgrounds/AstralUG" + i.ToString());
        }
    }

    public class LostColosseumSurfaceBGStyle : ModSurfaceBackgroundStyle
    {
        public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
        {
            b -= 250f;

            int frameOffset = (int)(Main.GameUpdateCount / 10U) % 4;
            int backgroundFrameIndex = 251 + frameOffset;

            return BackgroundTextureLoader.GetBackgroundSlot($"Terraria/Images/Background_{backgroundFrameIndex}");
        }

        public override void Load()
        {
            // Load the background with the large lake animation frames.
            for (int i = 251; i <= 254; i++)
                BackgroundTextureLoader.AddBackgroundTexture(Mod, $"Terraria/Images/Background_{i}");
        }

        public override void ModifyFarFades(float[] fades, float transitionSpeed)
        {
            // This just fades in the background and fades out other backgrounds.
            for (int i = 0; i < fades.Length; i++)
            {
                if (i == Slot)
                {
                    fades[i] += transitionSpeed;
                    if (fades[i] > 1f)
                        fades[i] = 1f;
                }
                else
                {
                    fades[i] -= transitionSpeed;
                    if (fades[i] < 0f)
                        fades[i] = 0f;
                }
            }
        }
    }
}
