using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc.Projectiles;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers
{
    public class XerocClockDeathDeathScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => AnyProjectiles(ModContent.ProjectileType<ClockConstellation>());

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:XerocClockDeathDeathSky", isActive);
        }
    }

    public class XerocClockDeathDeathScreenShaderData : ScreenShaderData
    {
        public static float OutlineIntensity
        {
            get;
            set;
        }

        public static Vector2 ClockCenter
        {
            get;
            set;
        }

        public XerocClockDeathDeathScreenShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Update(GameTime gameTime)
        {
            var clocks = AllProjectilesByID(ModContent.ProjectileType<ClockConstellation>());
            if (clocks.Any())
            {
                ClockCenter = clocks.First().Center;
                OutlineIntensity = clocks.First().Opacity;
            }
            else
                OutlineIntensity = Clamp(OutlineIntensity - 0.08f, 0f, 1f);
        }

        public override void Apply()
        {
            Main.instance.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/FireNoise").Value;
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise").Value;
            Shader.Parameters["clockCenter"].SetValue(WorldSpaceToScreenUV(ClockCenter));
            UseColor(Color.LightCoral);
            UseSecondaryColor(Color.Orange * 0.4f);
            UseIntensity(OutlineIntensity);
            base.Apply();
        }
    }
}
