using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class XerocTrophyTile : ModTile
    {
        private static Asset<Texture2D> tileTexture;

        private static Asset<Texture2D> eyelidTexture;

        private static Asset<Texture2D> scleraTexture;

        private static Asset<Texture2D> pupilTexture;

        public static int DelaySinceLastBlinkSound
        {
            get;
            set;
        }

        public static readonly SoundStyle BlinkSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/XerocTrophyBlink") with { PitchVariance = 0.1f };

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.addTile(Type);

            AddMapEntry(new(120, 85, 60), Language.GetText("MapObject.Trophy"));
            DustType = 7;

            // Load texture assets. This way, tiles don't attempt to load them from the central registry via Request every time.
            if (Main.netMode != NetmodeID.Server)
            {
                tileTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/XerocTrophyTileFull");
                eyelidTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/XerocTrophyEyelid");
                scleraTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/XerocTrophySclera");
                pupilTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/XerocTrophyPupil");
            }
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX;
            int frameY = t.TileFrameY;

            // Everything is drawn by the top-left frame at once.
            if (frameX != 0 || frameY != 0)
                return false;

            // Draw the main tile texture.
            Texture2D mainTexture = tileTexture.Value;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffset;
            Color lightColor = Lighting.GetColor(i + 1, j + 1);
            spriteBatch.Draw(mainTexture, drawPosition, null, lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

            // Calculate the direction to the player, to determine how the pupil should be oriented.
            // This does not look at the nearest player, it explicitly looks at the current client at all times.
            // Under typical circumstances this would be a bit weird, and somewhat illogical, but since this is a Xeroc item I think it makes for a pretty cool, albeit
            // subtle detail. Makes it as though Xeroc is looking across multiple different realities at once or something.
            Vector2 worldPosition = new Point(i + 1, j + 1).ToWorldCoordinates();
            Vector2 offsetFromPlayer = Main.LocalPlayer.Center - worldPosition;

            // Calculate the pupil frame.
            int pupilFrame = 0;
            float pupilFrameTime = (Main.GlobalTimeWrappedHourly * 1.9f + i * 0.44f + j * 0.13f) % 15f;
            if (pupilFrameTime >= 14f)
                pupilFrame = (int)Remap(pupilFrameTime, 14f, 14.8f, 0f, 8f);

            DelaySinceLastBlinkSound = Clamp(DelaySinceLastBlinkSound - 1, 0, 30);
            if (DelaySinceLastBlinkSound <= 0 && (pupilFrame == 4 || pupilFrame == 5) && Main.instance.IsActive)
            {
                DelaySinceLastBlinkSound = 30;
                SoundEngine.PlaySound(BlinkSound, worldPosition);
            }

            // Draw the eye over the tile.
            float eyeScale = 0.37f;
            float pupilScaleFactor = Remap(offsetFromPlayer.Length(), 30f, 142f, Sin(Main.GlobalTimeWrappedHourly * 50f + i + j * 13f) * 0.012f + 0.6f, 0.9f);
            Texture2D sclera = scleraTexture.Value;
            Texture2D pupil = pupilTexture.Value;
            Texture2D eyelid = eyelidTexture.Value;
            Vector2 pupilOffset = (offsetFromPlayer * 0.1f).ClampMagnitude(0f, 24f) * new Vector2(1f, 0.4f) * eyeScale;
            Rectangle eyelidFrameRectangle = eyelid.Frame(1, 9, 0, pupilFrame);
            drawPosition += Vector2.One * 24f;
            Main.spriteBatch.Draw(sclera, drawPosition, null, Color.White, 0f, sclera.Size() * 0.5f, eyeScale, 0, 0f);
            Main.spriteBatch.Draw(pupil, drawPosition + pupilOffset, null, Color.White, 0f, pupil.Size() * 0.5f, eyeScale * pupilScaleFactor, 0, 0f);
            Main.spriteBatch.Draw(eyelid, drawPosition, eyelidFrameRectangle, Color.White, 0f, eyelidFrameRectangle.Size() * 0.5f, eyeScale, 0, 0f);

            return false;
        }
    }
}
