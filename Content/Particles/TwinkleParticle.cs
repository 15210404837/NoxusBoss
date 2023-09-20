﻿using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class TwinkleParticle : Particle
    {
        public struct LockOnDetails
        {
            public Func<Vector2> LockOnCenter;

            public Vector2 LockOnOffset;

            public void Apply(ref Vector2 position)
            {
                if (LockOnCenter is null)
                    return;

                position = LockOnCenter() + LockOnOffset;
            }
        }

        public LockOnDetails LockOnThing;

        public int TotalStarPoints;

        public float Opacity = 1f;

        public Vector2 ScaleFactor;

        public override bool SetLifetime => true;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => true;

        public override string Texture => "NoxusBoss/Content/Particles/VerticalLightStreak";

        public TwinkleParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, int totalStarPoints, Vector2 scaleFactor, LockOnDetails lockOnDetails = default)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            ScaleFactor = scaleFactor;
            TotalStarPoints = totalStarPoints;
            Lifetime = lifetime;
            LockOnThing = lockOnDetails;
        }

        public override void Update()
        {
            Opacity = GetLerpValue(0f, 10f, Time, true) * GetLerpValue(Lifetime, 16f, Time, true);
            LockOnThing.Apply(ref Position);
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            int instanceCount = TotalStarPoints / 2;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomFlare = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare").Value;
            Vector2 scale = ScaleFactor * Opacity * 0.1f;
            scale *= Sin(Main.GlobalTimeWrappedHourly * 30f + Time * 0.08f) * 0.125f + 1f;

            // Draw the bloom flare.
            spriteBatch.Draw(bloomFlare, Position - Main.screenPosition, null, Color * Opacity, Rotation - Main.GlobalTimeWrappedHourly * 5f, bloomFlare.Size() * 0.5f, scale * 0.42f, 0, 0f);
            spriteBatch.Draw(bloomFlare, Position - Main.screenPosition, null, Color * Opacity, Rotation + Main.GlobalTimeWrappedHourly * 5f, bloomFlare.Size() * 0.5f, scale * 0.42f, 0, 0f);

            // Draw the points of the twinkle.
            for (int i = 0; i < instanceCount; i++)
            {
                float rotationOffset = Pi * i / instanceCount;
                float instanceRotation = Rotation + rotationOffset;
                Vector2 localScale = scale;

                if (rotationOffset != 0f)
                    localScale *= Pow(Sin(rotationOffset), 1.5f);

                for (float s = 1f; s > 0.3f; s -= 0.2f)
                    spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color * Opacity, instanceRotation, texture.Size() * 0.5f, new Vector2(1f, 0.6f) * localScale * s, 0, 0f);
            }
        }
    }
}
