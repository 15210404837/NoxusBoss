using System;
using System.Collections.Generic;
using System.Linq;

namespace NoxusBoss.Common.Easings
{
    public class PiecewiseCurve
    {
        protected struct CurveSegment
        {
            internal float StartingHeight;

            internal float EndingHeight;

            internal float AnimationStart;

            internal float AnimationEnd;

            internal EasingCurve Curve;

            internal EasingType CurveType;
        }

        protected List<CurveSegment> segments = new();

        public PiecewiseCurve Add(EasingCurve curve, EasingType curveType, float endingHeight, float animationEnd, float? startingHeight = null)
        {
            float animationStart = segments.Any() ? segments.Last().AnimationEnd : 0f;
            startingHeight ??= segments.Any() ? segments.Last().EndingHeight : 0f;
            if (animationEnd <= 0f || animationEnd > 1f)
                throw new InvalidOperationException("A piecewise animation curve segment cannot have a domain outside of 0-1.");

            // Add the new segment.
            segments.Add(new()
            {
                StartingHeight = startingHeight.Value,
                EndingHeight = endingHeight,
                AnimationStart = animationStart,
                AnimationEnd = animationEnd,
                Curve = curve,
                CurveType = curveType
            });

            // Return the piecewise curve that called this method to allow method chaining.
            return this;
        }

        public float Evaluate(float interpolant)
        {
            // Clamp the interpolant into the valid range.
            interpolant = Clamp(interpolant, 0f, 1f);

            // Calculate the local interpolant relative to the segment that the base interpolant fits into.
            CurveSegment segmentToUse = segments.Find(s => interpolant >= s.AnimationStart && interpolant <= s.AnimationEnd);
            float curveLocalInterpolant = GetLerpValue(segmentToUse.AnimationStart, segmentToUse.AnimationEnd, interpolant, true);

            // Calculate the segment value based on the local interpolant.
            return segmentToUse.Curve.Evaluate(segmentToUse.CurveType, segmentToUse.StartingHeight, segmentToUse.EndingHeight, curveLocalInterpolant);
        }
    }
}
