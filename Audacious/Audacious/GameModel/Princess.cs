using GalaSoft.MvvmLight.Messaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using ScreenControlsSample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audacious.GameModel
{
    public class Princess : PhysicalObject
    {
        Texture2D princessSpriteSheet;
        public static Vector2 ScreenSize = new Vector2(800f, 480f);
        public static Vector2 GameScreenSize = new Vector2(512, 384);
        int firstTickCount;
        bool isWalking = false;

        bool canShoot = true;
        public bool CanShoot
        {
            get { return canShoot; }
            set { canShoot = value; }
        }

        Vector2 savedPosition = new Vector2(8, 18);
        public Vector2 SavedPosition
        {
            get { return savedPosition; }
            set { savedPosition = value; }
        }

        public Princess(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad)
            : base(content, spriteBatch, deviceScreenSize, screenPad)
        {
            Position = new Vector2(windowTilesSize.X / 2 - Size.X / 2, 3);
        }

        public override void LoadContent()
        {
            Size = new Vector2(2.5f, 2.5f);
            princessSpriteSheet = princessSpriteSheet ?? ContentHelper.Instance.GetContent<Texture2D>("PrincessSpriteSheet");
        }

        public override void Update(GameTime gameTime, int tickCount, float scrollRows)
        {
            if (firstTickCount == 0)
                firstTickCount = tickCount;

            var deltaTicks = tickCount - firstTickCount;

            if ((deltaTicks / 8) % 2 == 1 && Position.Y < windowTilesSize.Y / 2 - 1)
            {
                isWalking = true;
                Position = new Vector2(
                    windowTilesSize.X / 2 - Size.X / 2
                        + (1f / (tileWidth * 8))
                    , Position.Y + (1f / (tileWidth * 2)));
            }
            else
            {
                isWalking = false;
            }
        }

        public override void Draw(GameTime gameTime, int tickCount, float scrollRows)
        {
            Rectangle playerRectangle;
            playerRectangle = new Rectangle(0, 0, (int)(Size.X * tileWidth), (int)(Size.Y * tileWidth));
            var frameIndex = (tickCount / 2) % 2;
            spriteBatch.Draw(
                princessSpriteSheet, 
                TopLeftCorner + Position * tileWidth
                + new Vector2(0, isWalking ? frameIndex * 2 : 0), 
                playerRectangle, 
                Color.White);
        }
    }
}
