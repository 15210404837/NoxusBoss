using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics
{
    public abstract class Metaball
    {
        internal List<ManagedRenderTarget> LayerTargets = new();

        public abstract List<Texture2D> Layers
        {
            get;
        }

        public abstract MetaballDrawLayerType DrawContext
        {
            get;
        }

        public abstract Color EdgeColor
        {
            get;
        }

        public virtual bool FixedInPlace => false;

        public virtual void Update() { }

        public virtual bool PrepareSpriteBatch(SpriteBatch spriteBatch) => false;

        public abstract void DrawInstances();

        public Metaball()
        {
            // No render target creation on servers.
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() =>
            {
                for (int i = 0; i < Layers.Count; i++)
                    LayerTargets.Add(new(true, RenderTargetManager.CreateScreenSizedTarget));
            });
        }

        public void Dispose()
        {
            for (int i = 0; i < LayerTargets.Count; i++)
                LayerTargets[i]?.Dispose();
        }
    }
}
