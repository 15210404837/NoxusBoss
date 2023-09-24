namespace NoxusBoss.Common.Easings
{
    public class ExpEasing : EasingCurve
    {
        public static readonly ExpEasing Default = new();

        public ExpEasing(float exponent = 10f)
        {
            InCurve = new(interpolant =>
            {
                return Pow(2f, exponent * interpolant - exponent);
            });
            OutCurve = new(interpolant =>
            {
                return 1f - Pow(2f, -exponent * interpolant);
            });
            InOutCurve = new(interpolant =>
            {
                if (interpolant < 0.5f)
                    return Pow(2f, exponent * interpolant * 2f - exponent);
                return 2f - Pow(2f, exponent * interpolant * -2f + exponent);
            });
        }
    }
}
