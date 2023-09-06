using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Particles;
using NoxusBoss.Content.Subworlds;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class TreeOfLife : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(60, 81, 60));
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // Draw the main tile texture.
            Texture2D mainTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/TreeOfLifeButReal").Value;

            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y + 20f) + drawOffset;
            Color lightColor = Lighting.GetColor(i, j);
            float treeRotation = Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.01f + 0.03f;
            float scale = 1f;
            spriteBatch.Draw(mainTexture, drawPosition, null, lightColor, treeRotation, mainTexture.Size() * new Vector2(0.5f, 1f), scale, 0, 0f);

            // Draw fruits on the leaves.
            ulong fruitSeed = (ulong)(i * 3 + j * 7);
            Texture2D fruitTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/FruitOfLife").Value;
            Vector2[] fruitOffsets = new Vector2[]
            {
                new Vector2(-104f, -120f),
                new Vector2(-71f, -177f),
                new Vector2(-51f, -204f),
                new Vector2(-35f, -172f),
                new Vector2(12f, -148f),
                new Vector2(30f, -175f),
                new Vector2(67f, -139f)
            };

            if (EternalGardenUpdateSystem.LifeFruitDroppedFromTree)
                return false;

            bool xerocIsPresent = XerocBoss.Myself is not null && XerocBoss.Myself.Opacity >= 1f;
            foreach (Vector2 fruitOffset in fruitOffsets)
            {
                Color fruitColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.2f + fruitOffset.X * 0.07f) % 1f, 0.92f, 0.92f);
                float fruitScale = Lerp(0.55f, 1.02f, RandomFloat(ref fruitSeed)) * scale;
                float fruitRotation = Sin(fruitOffset.X * 0.6f + Main.GlobalTimeWrappedHourly * 5.6f) * 0.15f - 0.25f;
                Vector2 properFruitOffset = fruitOffset.RotatedBy(treeRotation) * scale;
                spriteBatch.Draw(fruitTexture, drawPosition + properFruitOffset, null, fruitColor, fruitRotation, fruitTexture.Size() * new Vector2(0.5f, 0f), fruitScale, 0, 0f);

                // Drop the fruit if Xeroc has arrived.
                if (xerocIsPresent)
                {
                    Vector2 fruitWorldPosition = new Point(i, j).ToWorldCoordinates() + Vector2.UnitY * 20f + properFruitOffset;
                    FruitOfLifeParticle fruit = new(fruitWorldPosition, Vector2.UnitY.RotatedBy(fruitRotation) * Main.rand.NextFloat(2.5f, 3.2f), fruitColor, Main.rand.Next(420, 540), fruitScale);
                    GeneralParticleHandler.SpawnParticle(fruit);
                    EternalGardenUpdateSystem.LifeFruitDroppedFromTree = true;
                }
            }
            return false;
        }
    }
}
