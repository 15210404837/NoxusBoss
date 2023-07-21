using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using System.Linq;
using ReLogic.Content;
using Terraria.Graphics.Shaders;
using Microsoft.Xna.Framework.Content;

namespace NoxusBoss.Assets
{
    public class ShaderMonitor : ModSystem
    {
        public static Queue<string> CompilingFiles
        {
            get;
            private set;
        }

        public FileSystemWatcher ShaderWatcher
        {
            get;
            private set;
        }

        public static string EffectsPath
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            CompilingFiles = new();
            if (Main.netMode != NetmodeID.SinglePlayer)
                return;

            // Check to see if the user has a folder that corresponds to the shaders for this mod.
            // If this folder is not present, that means that they are not a developer and thusly this system would be irrelevant.
            string modSourcesPath = $"{Path.Combine(Program.SavePathShared, "ModSources")}\\{Mod.Name}".Replace("\\..\\tModLoader", string.Empty);
            if (!Directory.Exists(modSourcesPath))
                return;

            // Verify that the Assets/Effects directory exists.
            EffectsPath = $"{modSourcesPath}\\Assets\\Effects";
            if (!Directory.Exists(EffectsPath))
                return;

            // If the Assets/Effects directory exists, watch over it.
            ShaderWatcher = new(EffectsPath)
            {
                Filter = "*.fx",
                IncludeSubdirectories = false, // This is done to prevent the shader watcher from looking in the compiler folder.
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Security
            };
            ShaderWatcher.Changed += RecompileShader;
        }

        public override void PostUpdateEverything()
        {
            bool shaderIsCompiling = false;
            List<string> compiledFiles = new();
            string compilerDirectory = EffectsPath + "\\Compiler\\";
            while (CompilingFiles.TryDequeue(out string shaderPath))
            {
                // Take the contents of the new shader and copy them over to the compiler folder so that the XNB can be regenerated.
                string shaderPathInCompilerDirectory = compilerDirectory + Path.GetFileName(shaderPath);
                File.Delete(shaderPathInCompilerDirectory);
                File.WriteAllText(shaderPathInCompilerDirectory, File.ReadAllText(shaderPath));
                shaderIsCompiling = true;
                compiledFiles.Add(shaderPath);
            }

            if (shaderIsCompiling)
            {
                // Execute EasyXNB.
                Process easyXnb = new()
                {
                    StartInfo = new()
                    {
                        FileName = EffectsPath + "\\Compiler\\EasyXnb.exe",
                        WorkingDirectory = compilerDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                easyXnb.Start();
                easyXnb.WaitForExit();
                easyXnb.Kill();
            }

            for (int i = 0; i < compiledFiles.Count; i++)
            {
                // Copy over the XNB from the compiler, and delete the copy in the Compiler folder.
                string shaderPath = compiledFiles[i];
                string compiledXnbPath = EffectsPath + "\\Compiler\\" + Path.GetFileNameWithoutExtension(shaderPath) + ".xnb";
                string originalXnbPath = shaderPath.Replace(".fx", ".xnb");
                File.Delete(originalXnbPath);
                File.Copy(compiledXnbPath, originalXnbPath);
                File.Delete(compiledXnbPath);

                // Finally, load the new XNB into the game's shaders that reference it.
                string shaderPathInCompilerDirectory = compilerDirectory + Path.GetFileName(shaderPath);
                File.Delete(shaderPathInCompilerDirectory);
                Main.QueueMainThreadAction(() =>
                {
                    ContentManager tempManager = new(Main.instance.Content.ServiceProvider, EffectsPath);
                    string assetName = Path.GetFileNameWithoutExtension(originalXnbPath);
                    Effect newEffect = tempManager.Load<Effect>(assetName);
                    UpdateShaderReferences(compiledXnbPath, newEffect);

                    Main.NewText($"Shader with the file name '{Path.GetFileName(shaderPath)}' has been successfully recompiled.");
                });
            }
        }

        public override void OnModUnload()
        {
            ShaderWatcher?.Dispose();
        }

        private void RecompileShader(object sender, FileSystemEventArgs e)
        {
            if (CompilingFiles.Contains(e.FullPath))
                return;

            CompilingFiles.Enqueue(e.FullPath);
        }

        private void UpdateShaderReferences(string file, Effect recompiledEffect)
        {
            var modEffects = Mod.Assets.GetLoadedAssets().OfType<Asset<Effect>>().ToDictionary(x => x.Name);
            string key = @"Assets\Effects\" + Path.GetFileNameWithoutExtension(file);

            Effect originalEffect = null;
            if (modEffects.TryGetValue(key, out Asset<Effect> effect))
                originalEffect = effect.Value;

            // Go through reflection hell to acquire all of the shader references.
            FieldInfo shaderRefField = typeof(ShaderData).GetField("_shader", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo ownValueField = modEffects[key].GetType().GetField("ownValue", BindingFlags.Instance | BindingFlags.NonPublic);
            ownValueField.SetValue(modEffects[key], recompiledEffect);

            FieldInfo shaderDataField = typeof(ArmorShaderDataSet).GetField("_shaderData", BindingFlags.Instance | BindingFlags.NonPublic);
            var armorShaderDataList = (List<ArmorShaderData>)typeof(ArmorShaderDataSet).GetField("_shaderData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameShaders.Armor);
            var hairShaderDataList = (List<HairShaderData>)typeof(HairShaderDataSet).GetField("_shaderData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameShaders.Hair);
            var miscShaderDataList = GameShaders.Misc.Values.ToList();
            var filtersShaderDataList = ((Dictionary<string, Filter>)typeof(EffectManager<Filter>).GetField("_effects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Filters.Scene)).Values.ToList().Select(x => (ScreenShaderData)typeof(Filter).GetField("_shader", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(x));

            int entriesFound = 0;
            var shaderData = armorShaderDataList.Cast<ShaderData>().Union(hairShaderDataList.Cast<ShaderData>()).Union(miscShaderDataList.Cast<ShaderData>()).Union(filtersShaderDataList.Cast<ShaderData>());
            foreach (var shaderDataShader in shaderData)
            {
                if (originalEffect != null && shaderDataShader.Shader == originalEffect)
                {
                    entriesFound++;
                    shaderRefField.SetValue(shaderDataShader, new Ref<Effect>(recompiledEffect));
                }
            }
        }
    }
}
