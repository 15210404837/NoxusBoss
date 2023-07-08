using Microsoft.Xna.Framework;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        public static Vector2 StarPolarEquation(int pointCount, float angle)
        {
            float spacedAngle = angle;

            // There should be a star point that looks directly upward. However, that isn't the case for non-even star counts with the equation below.
            // To address this, a -90 degree rotation is performed.
            if (pointCount % 2 != 0)
                spacedAngle -= PiOver2;

            // Refer to desmos to view the resulting shape this creates. It's basically a black box of trig otherwise.
            float sqrt3 = 1.732051f;
            float numerator = Cos(Pi * (pointCount + 1f) / pointCount);
            float starAdjustedAngle = Asin(Cos(pointCount * spacedAngle)) * 2f;
            float denominator = Cos((starAdjustedAngle + PiOver2 * pointCount) / (pointCount * 2f));
            Vector2 result = angle.ToRotationVector2() * numerator / denominator / sqrt3;
            return result;
        }
    }
}
