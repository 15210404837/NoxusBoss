using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        public static Vector2 StarPolarEquation(int pointCount, float angle)
        {
            float spacedAngle = angle;

            // There should be a star point that looks directly upward. However, that isn't the case for odd star counts with the equation below.
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

        public static Vector2 IKSolve2(Vector2 start, Vector2 end, float A, float B, bool flip)
        {
            float C = Vector2.Distance(start, end);
            float angle = Acos(Clamp((C * C + A * A - B * B) / (C * A * 2f), -1f, 1f)) * flip.ToDirectionInt();
            return start + (angle + start.AngleTo(end)).ToRotationVector2() * A;
        }

        public static int SecondsToFrames(float seconds) => (int)(seconds * 60f);

        public static float Sin01(float x) => Sin(x) * 0.5f + 0.5f;

        public static float Cos01(float x) => Cos(x) * 0.5f + 0.5f;

        public static float Convert01To010(float x) => Sin(Pi * Clamp(x, 0f, 1f));

        // When two periodic functions are summed, the resulting function is periodic if the ratio of the b/a is rational, given periodic functions f and g:
        // f(a * x) + g(b * x). However, if the ratio is irrational, then the result has no period.
        // This is desirable for somewhat random wavy fluctuations.
        // In this case, pi/1 (or simply pi) and e used, which are indeed irrational numbers.
        /// <summary>
        /// Calculates an aperiodic sine. This function only achieves this if <paramref name="a"/> and <paramref name="b"/> are irrational numbers.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <param name="a">The first irrational coefficient.</param>
        /// <param name="b">The second irrational coefficient.</param>
        public static float AperiodicSin(float x, float dx = 0f, float a = Pi, float b = MathHelper.E)
        {
            return (Sin(x * a + dx) + Sin(x * b + dx)) * 0.5f;
        }
    }
}
