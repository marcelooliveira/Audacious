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
    public class Bonus : PhysicalObject
    {
        float scrollRows;
        int hitCount = 0;
        char bonusCode;

        BonusState interMediateState = BonusState.Unknown;
        public BonusState InterMediateState
        {
            get { return interMediateState; }
            set { interMediateState = value; }
        }

        BonusState state = BonusState.Unknown;
        public BonusState State
        {
            get { return state; }
            set { state = value; }
        }

        float lives = 1f;
        public float Lives
        {
            get { return lives; }
            set { lives = value; }
        }

        public Bonus(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, Char bonusCode)
        : base(content, spriteBatch, deviceScreenSize, screenPad)
        {
            Size = new Vector2(2, 2);
            Position = position;
            this.bonusCode = bonusCode;
            switch (bonusCode)
            {
                case 'A':
                    interMediateState = BonusState.FiveHundredPoints;
                    break;
                case 'B':
                    interMediateState = BonusState.Freeze;
                    break;
                case 'C':
                    interMediateState = BonusState.ExtraLife;
                    break;
                case 'D':
                    interMediateState = BonusState.Barrier;
                    break;
                case 'E':
                    interMediateState = BonusState.KillAllInScreen;
                    break;
            }
        }

        protected Texture2D bonusSpriteSheet;
        public override void LoadContent()
        {
            bonusSpriteSheet = bonusSpriteSheet ?? ContentHelper.Instance.GetContent<Texture2D>("BonusSpriteSheet");
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime, int tickCount, float scrollRows)
        {
            this.scrollRows = scrollRows;
        }

        protected virtual Rectangle DestinationRectangle()
        {
            var destinationRectangle = new Rectangle(
                      (int)(TopLeftCorner.X + Position.X * tileWidth)
                    , (int)(TopLeftCorner.Y + (Position.Y - scrollRows) * tileWidth)
                    , (int)(Size.X * tileWidth)
                    , (int)(Size.Y * tileWidth));
            return destinationRectangle;
        }

        public override void Draw(GameTime gameTime, int tickCount, float scrollRows)
        {
            var bonusRectangle = new Rectangle((int)state * bonusSpriteSheet.Height, 0, (int)(Size.X * tileWidth), (int)(Size.Y * tileWidth));
            spriteBatch.Draw(bonusSpriteSheet
                , DestinationRectangle()
                , bonusRectangle
                , Color.White);
        }

        public void Hit(Bullet bullet)
        {
            var temp = bullet.Damage;
            bullet.Damage -= this.Lives;
            this.lives -= temp;
            if (this.lives <= 0)
            {
                state = interMediateState;
                Messenger.Default.Send(new BonusStateChangedMessage { Bonus = this });
            }
        }
    }

    public enum BonusState
    {
        Unknown = 0,
        Used = 1,
        FiveHundredPoints = 2,
        Freeze = 3,
        ExtraLife = 4,
        Barrier = 5,
        KillAllInScreen = 6
    }


    public class BonusStateChangedMessage { public Bonus Bonus { get; set; } }
}
