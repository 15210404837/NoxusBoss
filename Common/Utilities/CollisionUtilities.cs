using Microsoft.Xna.Framework;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Determines if a typical hitbox rectangle is intersecting a circular hitbox.
        /// </summary>
        /// <param name="centerCheckPosition">The center of the circular hitbox.</param>
        /// <param name="radius">The radius of the circular hitbox.</param>
        /// <param name="targetHitbox">The hitbox of the target to check.</param>
        public static bool CircularHitboxCollision(Vector2 centerCheckPosition, float radius, Rectangle targetHitbox)
        {
            float topLeftDistance = Vector2.Distance(centerCheckPosition, targetHitbox.TopLeft());
            float topRightDistance = Vector2.Distance(centerCheckPosition, targetHitbox.TopRight());
            float bottomLeftDistance = Vector2.Distance(centerCheckPosition, targetHitbox.BottomLeft());
            float bottomRightDistance = Vector2.Distance(centerCheckPosition, targetHitbox.BottomRight());

            float distanceToClosestPoint = topLeftDistance;
            if (topRightDistance < distanceToClosestPoint)
                distanceToClosestPoint = topRightDistance;
            if (bottomLeftDistance < distanceToClosestPoint)
                distanceToClosestPoint = bottomLeftDistance;
            if (bottomRightDistance < distanceToClosestPoint)
                distanceToClosestPoint = bottomRightDistance;

            return distanceToClosestPoint <= radius;
        }
    }
}
