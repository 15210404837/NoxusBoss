using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Dusts
{
    public class FlowerPieceDust : ModDust
    {
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.noLight = true;
            dust.alpha = 50;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.scale *= 0.95f;
            dust.velocity *= 0.94f;
            if (dust.scale < 0.32f)
                dust.active = false;

            return false;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor) => new Color(255, 255, 255, dust.alpha).MultiplyRGB(dust.color);
    }
}
