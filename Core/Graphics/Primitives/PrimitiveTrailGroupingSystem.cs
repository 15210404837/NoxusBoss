using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NoxusBoss.Core.Graphics.Automators;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace NoxusBoss.Core.Graphics
{
    public class PrimitiveTrailGroupingSystem : ModSystem
    {
        private static readonly Dictionary<Type, PrimitiveTrailGroup> groups = new();

        public static PrimitiveTrailGroup GetGroup(Type t) => groups[t];

        public static PrimitiveTrailGroup GetGroup<T>() where T : ModType => GetGroup(typeof(T));

        public override void OnModLoad()
        {
        }

        public override void PostSetupContent()
        {
            // Load all primitive groups. This is done in PostSetupContent instead of OnModLoad because if the shader system isn't loaded this will fail.
            foreach (Type type in AssemblyManager.GetLoadableTypes(Mod.Code))
            {
                if (!type.IsAbstract && type.GetInterfaces().Contains(typeof(IDrawGroupedPrimitives)))
                {
                    IDrawGroupedPrimitives groupHandler = (IDrawGroupedPrimitives)FormatterServices.GetUninitializedObject(type);
                    groups[type] = new(groupHandler.DrawContext, groupHandler.Shader, groupHandler.MaxIndices, groupHandler.MaxVertices, groupHandler.PrepareShader);
                }
            }
        }

        public static bool DrawGroupWithDrawContext(PrimitiveGroupDrawContext drawContext)
        {
            bool anythingWasDrawn = false;
            foreach (var group in groups.Values)
            {
                if (!group.DrawContext.HasFlag(drawContext))
                    continue;

                anythingWasDrawn = true;
                group.Draw(group.DrawContext.HasFlag(PrimitiveGroupDrawContext.Pixelated));
            }

            return anythingWasDrawn;
        }

        public override void OnModUnload()
        {
            foreach (var group in groups.Values)
                group.Dispose();
        }
    }
}
