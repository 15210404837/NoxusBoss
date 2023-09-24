namespace NoxusBoss.Common.Easings
{
    public class PolynomialEasing : EasingCurve
    {
        public PolynomialEasing(float exponent)
        {
            InCurve = new(interpolant =>
            {
                return Pow(interpolant, exponent);
            });
            OutCurve = new(interpolant =>
            {
                return 1f - Pow(1f - interpolant, exponent);
            });
            InOutCurve = new(interpolant =>
            {
                if (interpolant < 0.5f)
                    return Pow(2f, exponent - 1f) * Pow(interpolant, exponent);
                return 1f - Pow(interpolant * -2f + 2f, exponent) * 0.5f;
            });
        }
    }
}
