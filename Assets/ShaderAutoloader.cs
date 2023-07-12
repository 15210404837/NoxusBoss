using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Noxus;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Core.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Assets
{
    public class ShaderAutoloader : ModSystem
    {
        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            foreach (var path in Mod.GetFileNames().Where(f => f.Contains("Assets/Effects/")))
            {
                string shaderName = Path.GetFileNameWithoutExtension(path);
                string clearedPath = Path.Combine(Path.GetDirectoryName(path), shaderName).Replace(@"\", @"/");
                Ref<Effect> shader = new(Mod.Assets.Request<Effect>(clearedPath, AssetRequestMode.ImmediateLoad).Value);
                GameShaders.Misc[$"{Mod.Name}:{shaderName}"] = new MiscShaderData(shader, "AutoloadPass");
            }

            // This is kind of hideous but I'm not sure how to best handle these screen shaders. Perhaps some marker in the file name?
            Ref<Effect> s = new(Mod.Assets.Request<Effect>("Assets/Effects/LocalizedDistortionShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:NoxusEggSky"] = new Filter(new NoxusEggScreenShaderData(s, "AutoloadPass"), EffectPriority.VeryHigh);

            Filters.Scene["NoxusBoss:NoxusSky"] = new Filter(new GenericScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f), EffectPriority.VeryHigh);
            SkyManager.Instance["NoxusBoss:NoxusSky"] = new NoxusSky();

            Ref<Effect> s2 = new(Mod.Assets.Request<Effect>("Assets/Effects/XerocScreenTearShader", AssetRequestMode.ImmediateLoad).Value);
            SkyManager.Instance["NoxusBoss:XerocSky"] = new XerocSky();
            Filters.Scene["NoxusBoss:XerocSky"] = new Filter(new XerocScreenShaderData(s2, "AutoloadPass"), EffectPriority.VeryHigh);

            Ref<Effect> s3 = new(Mod.Assets.Request<Effect>("Assets/Effects/RadialScreenShoveShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:LightWaveScreenShove"] = new Filter(new RadialScreenShoveShaderData(s3, "AutoloadPass"), EffectPriority.VeryHigh);

            Ref<Effect> s4 = new(Mod.Assets.Request<Effect>("Assets/Effects/ScreenSplitShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:LocalScreenSplit"] = new Filter(new LocalScreenSplitShaderData(s4, "AutoloadPass"), EffectPriority.VeryHigh);

            Ref<Effect> s5 = new(Mod.Assets.Request<Effect>("Assets/Effects/XerocClockDeathZoneShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene["NoxusBoss:XerocClockDeathDeathSky"] = new Filter(new XerocClockDeathDeathScreenShaderData(s5, "AutoloadPass"), EffectPriority.VeryHigh);

            Ref<Effect> s6 = new(Mod.Assets.Request<Effect>("Assets/Effects/SpreadTelegraphInvertedShader", AssetRequestMode.ImmediateLoad).Value);
            ScreenShaderData telegraphShader = new(s6, "AutoloadPass");
            Filters.Scene["NoxusBoss:SpreadTelegraphInverted"] = new Filter(telegraphShader, EffectPriority.VeryHigh);
            Filters.Scene["NoxusBoss:SpreadTelegraphInverted"].Load();
        }
    }
}
