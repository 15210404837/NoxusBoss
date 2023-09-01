using System;

namespace NoxusBoss.Core.Graphics
{
    [Flags]
    public enum PrimitiveGroupDrawContext
    {
        Pixelated = 0b0000001,
        AfterProjectiles = 0b0000010,
        AfterNPCs = 0b0000100,
        Manual = 0b0001000
    }
}
