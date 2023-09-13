using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Core.Graphics.Automators
{
    public class ManagedRenderTarget : IDisposable
    {
        private RenderTarget2D target;

        internal bool WaitingForFirstInitialization
        {
            get;
            private set;
        } = true;

        internal RenderTargetCreationCondition CreationCondition
        {
            get;
            private set;
        }

        public bool IsUninitialized => target is null || target.IsDisposed;

        public bool IsDisposed
        {
            get;
            private set;
        }

        public bool ShouldResetUponScreenResize
        {
            get;
            private set;
        }

        public RenderTarget2D Target
        {
            get
            {
                if (IsUninitialized)
                {
                    target = CreationCondition(Main.screenWidth, Main.screenHeight);
                    WaitingForFirstInitialization = false;
                }

                return target;
            }
            private set => target = value;
        }

        public int Width => Target.Width;

        public int Height => Target.Height;

        public delegate RenderTarget2D RenderTargetCreationCondition(int screenWidth, int screenHeight);

        public ManagedRenderTarget(bool shouldResetUponScreenResize, RenderTargetCreationCondition creationCondition)
        {
            ShouldResetUponScreenResize = shouldResetUponScreenResize;
            CreationCondition = creationCondition;
            RenderTargetManager.ManagedTargets.Add(this);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            target?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Recreate(int screenWidth, int screenHeight)
        {
            Dispose();
            IsDisposed = false;

            target = CreationCondition(screenWidth, screenHeight);
        }

        // These extension methods don't apply to ManagedRenderTarget instances, even with the implicit conversion operator. As such, it is implemented manually.
        public Vector2 Size() => Target.Size();

        public void SwapToRenderTarget(Color? flushColor = null) => Target.SwapToRenderTarget(flushColor);

        public void CopyContentsFrom(RenderTarget2D from) => Target.CopyContentsFrom(from);

        // This allows for easy shorthand conversions from ManagedRenderTarget to RenderTarget2D without having to manually type out ManagedTarget.Target all the time.
        // This is functionally equivalent to accessing the getter manually and will activate all of the relevant checks within said getter.
        public static implicit operator RenderTarget2D(ManagedRenderTarget targetWrapper) => targetWrapper.Target;
    }
}
