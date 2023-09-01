using NoxusBoss.Core.Graphics.Shaders;
using Terraria;

namespace NoxusBoss.Core.Graphics.Automators
{
    public interface IDrawGroupedPrimitives
    {
        public int MaxVertices
        {
            get;
        }

        public int MaxIndices
        {
            get;
        }

        public PrimitiveGroupDrawContext DrawContext
        {
            get;
        }

        public ManagedShader Shader
        {
            get;
        }

        public PrimitiveTrailGroup Group => PrimitiveTrailGroupingSystem.GetGroup(GetType());

        public void PrepareShader()
        {

        }

        public PrimitiveTrailInstance GenerateInstance()
        {
            // Don't bother trying to create an instance when the mod is being loaded.
            if (Main.gameMenu)
                return null;

            PrimitiveTrailInstance instance = new();
            Group?.Add(instance);

            return instance;
        }
    }
}
