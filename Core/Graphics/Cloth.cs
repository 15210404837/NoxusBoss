using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace NoxusBoss.Core.Graphics
{
    public class Cloth
    {
        public class VerletPoint
        {
            public int XIndex;

            public int YIndex;

            public float InvariantMass;

            public bool IsFixed;

            public Vector3 Position;

            public Vector3 OldPosition;

            public VerletPoint(Vector3 position, float invariantMass, int x, int y)
            {
                Position = position;
                OldPosition = position;
                InvariantMass = invariantMass;
                XIndex = x;
                YIndex = y;
            }
        }

        public class VerletStick
        {
            public VerletPoint Start;

            public VerletPoint End;

            public float RestingLength;

            public float Stiffness;

            public VerletStick(VerletPoint start, VerletPoint end, float restLength, float stiffness)
            {
                Start = start;
                End = end;
                RestingLength = restLength;
                Stiffness = stiffness;
            }
        }

        internal List<VerletPoint> points;

        internal List<VerletStick> sticks;

        public static int ConstraintIterations => 10;

        public static float DeltaTime => 1f / 60f;

        public readonly short[] MeshIndexCache;

        public readonly int CellCountX;

        public readonly int CellCountY;

        public readonly float CellSizeX;

        public readonly float CellSizeY;

        public readonly float Gravity;

        public readonly float EnergyConservationDamping;

        public Cloth(Vector2 startingPoint, int cellCountX, int cellCountY, float cellSizeX, float cellSizeY, float gravity, float energyConservationDamping)
        {
            CellCountX = cellCountX;
            CellCountY = cellCountY;
            CellSizeX = cellSizeX;
            CellSizeY = cellSizeY;
            Gravity = gravity;
            EnergyConservationDamping = energyConservationDamping;
            points = new();
            sticks = new();

            // Generate points.
            for (int i = 0; i < cellCountX; i++)
            {
                for (int j = 0; j < cellCountY; j++)
                {
                    Vector2 normalizedOffset = new Vector2(i / (float)cellCountX - 0.5f, i / (float)cellCountY - 0.5f) * 2f;
                    Vector3 cellPosition = new Vector3(startingPoint, 0f) + new Vector3(normalizedOffset.X, 0f, normalizedOffset.Y) * new Vector3(CellCountX * cellSizeX, 1f, CellCountY * cellSizeY);
                    points.Add(new(cellPosition, 1f, i, j));
                }
            }

            // Create sticks between the points.
            // Add structural springs between adjacent points
            for (int y = 0; y < cellCountX; y++)
            {
                for (int x = 0; x < cellCountY; x++)
                {
                    int pointIndex = IndexFrom2DCoord(x, y);

                    if (x < cellCountX - 1)
                    {
                        int adjacentIndex = pointIndex + 1;
                        sticks.Add(new(points[pointIndex], points[adjacentIndex], cellSizeX, 1f));
                    }

                    if (y < cellCountY - 1)
                    {
                        int adjacentIndex = pointIndex + cellCountX;
                        sticks.Add(new(points[pointIndex], points[adjacentIndex], cellSizeY, 1f));
                    }
                }
            }

            // Generate indices for the mesh.
            MeshIndexCache = new short[cellCountX * cellCountY * 6];
            for (int i = 0; i < cellCountX * cellCountY; i++)
            {
                MeshIndexCache[i * 6] = (short)(i * 4);
                MeshIndexCache[i * 6 + 1] = (short)(i * 4 + 1);
                MeshIndexCache[i * 6 + 2] = (short)(i * 4 + 2);
                MeshIndexCache[i * 6 + 3] = (short)(i * 4);
                MeshIndexCache[i * 6 + 4] = (short)(i * 4 + 2);
                MeshIndexCache[i * 6 + 5] = (short)(i * 4 + 3);
            }
        }

        public void SetStickPosition(int x, int y, Vector2 position, bool fixInPlace)
        {
            int index = IndexFrom2DCoord(x, y);

            if (fixInPlace)
                points[index].IsFixed = true;
            points[index].Position = new Vector3(position, 0f);
        }

        public void ApplyForce(int x, int y, Vector3 force)
        {
            int index = IndexFrom2DCoord(x, y);
            points[index].Position += force;
        }

        public void Simulate(float scale, Vector3 ellipsoidPosition = default, Vector3 ellipsoidRadius = default)
        {
            // Apply gravity forces to the cloth.
            Vector3 gravityDirection = Vector3.UnitY * Gravity;
            foreach (VerletPoint point in points)
            {
                if (!point.IsFixed)
                    point.Position += gravityDirection * point.InvariantMass;
            }

            // Perform Verlet Integration to each point.
            foreach (VerletPoint point in points)
            {
                if (point.IsFixed)
                    continue;

                // Apply conservation of energy by make changes in motion "build up" and take a while to dissipate.
                Vector3 momentumForce = (point.Position - point.OldPosition) * EnergyConservationDamping;
                Vector3 newPosition = point.Position + momentumForce + momentumForce * DeltaTime;
                point.OldPosition = point.Position;
                point.Position = newPosition;
            }

            // Apply stick constraints, to ensure that the sticks don't expand forever.
            for (int i = 0; i < ConstraintIterations; i++)
            {
                foreach (VerletStick stick in sticks)
                {
                    Vector3 stickCenter = (stick.Start.Position + stick.End.Position) / 2;
                    Vector3 stickDirection = Vector3.Normalize(stick.Start.Position - stick.End.Position);
                    float length = (stick.Start.Position - stick.End.Position).Length();
                    float restingLength = stick.RestingLength * scale;

                    if (length > restingLength)
                    {
                        if (!stick.Start.IsFixed)
                            stick.Start.Position = stickCenter + stickDirection * restingLength * 0.5f;
                        if (!stick.End.IsFixed)
                            stick.End.Position = stickCenter - stickDirection * restingLength * 0.5f;
                    }
                }
            }

            // Apply ellipsoid collision if one is considered in the simulation.
            if (ellipsoidPosition.Length() > 0.001f)
            {
                foreach (VerletPoint point in points)
                {
                    // Ensure that are points exist somewhere on the Z axis.
                    if (Abs(point.Position.Z) < 10f)
                    {
                        point.Position.Z = Sign(point.Position.Z) * 10f;
                        if (point.Position.Z == 0f)
                            point.Position.Z = 10f;

                        point.OldPosition.Z = point.Position.Z;
                    }

                    // Check collision with the ellipsoid.
                    Vector3 relativePos = point.Position - ellipsoidPosition;
                    relativePos /= ellipsoidRadius;
                    float ellipsoidSqr = Vector3.Dot(relativePos, relativePos);

                    // Resolve the collision if it happened.
                    if (ellipsoidSqr < 1f)
                    {
                        Vector3 normalizedRelativePosition = Vector3.Normalize(relativePos);
                        point.Position = ellipsoidPosition + normalizedRelativePosition * ellipsoidRadius;
                    }
                }
            }
        }

        public VertexPositionNormalTexture[] GenerateMesh()
        {
            List<VertexPositionNormalTexture> vertices = new();

            VertexPositionNormalTexture generateVertex(int x, int y)
            {
                int index = IndexFrom2DCoord(x, y);
                Vector3 normal = Vector3.UnitZ;
                VerletPoint point = points[index];

                // Calculate the normal of the vertex based on the cross product of the two rectangle edges.
                if (x >= 1 && y >= 1)
                {
                    int leftIndex = IndexFrom2DCoord(x - 1, y);
                    int topIndex = IndexFrom2DCoord(x, y - 1);
                    VerletPoint leftPoint = points[leftIndex];
                    VerletPoint topPoint = points[topIndex];
                    Vector3 leftOffset = leftPoint.Position - point.Position;
                    Vector3 topOffset = topPoint.Position - point.Position;
                    normal = Vector3.Normalize(Vector3.Cross(leftOffset, topOffset));

                    if (normal.Z < 0f)
                        normal *= -1f;
                }

                return new(point.Position, normal, new Vector2(x / (float)(CellCountX - 1f), y / (float)(CellCountY - 1f)));
            }

            // Calculate vertices as a quadrilateral formed from the given point and its neighbors.
            for (int x = 0; x < CellCountX - 1; x++)
            {
                for (int y = 0; y < CellCountY - 1; y++)
                {
                    VertexPositionNormalTexture topLeft = generateVertex(x, y);
                    VertexPositionNormalTexture topRight = generateVertex(x + 1, y);
                    VertexPositionNormalTexture bottomLeft = generateVertex(x, y + 1);
                    VertexPositionNormalTexture bottomRight = generateVertex(x + 1, y + 1);

                    vertices.Add(topLeft);
                    vertices.Add(topRight);
                    vertices.Add(bottomRight);
                    vertices.Add(bottomLeft);
                }
            }

            return vertices.ToArray();
        }

        public int IndexFrom2DCoord(int x, int y) => y * CellCountX + x;
    }
}
