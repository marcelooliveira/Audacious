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
    public class PowerUp : PhysicalObject
    {
        float scrollRows;
        protected int tickInMS = 20;
        protected TimeSpan accumElapsedGameTime = TimeSpan.FromSeconds(0);
        public event EventHandler OffScreen;
        protected Vector2? onWindowPosition;
        protected TimeSpan lastShotTime = TimeSpan.FromSeconds(0);
        protected TimeSpan accumShootingTime = TimeSpan.FromSeconds(0);
        ButtonState lastButtonState = ButtonState.Released;
        bool reloaded = true;
        Player player;
        public Vector2 StartPosition;
        float acceleration = .1f;

        Vector2 direction = new Vector2(0, 1);
        public Vector2 Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        protected float speed = .75f;
        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        PowerUpState state = PowerUpState.Shield;
        public PowerUpState State
        {
            get { return state; }
            set { state = value; }
        }

        public PowerUp(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, Player player)
        : base(content, spriteBatch, deviceScreenSize, screenPad)
        {
            Size = new Vector2(2, 2);
            StartPosition =
            Position = position;
            this.player = player;
        }

        protected Texture2D powerUpSpriteSheet;
        public override void LoadContent()
        {
            powerUpSpriteSheet = powerUpSpriteSheet ?? ContentHelper.Instance.GetContent<Texture2D>("PowerUpSpriteSheet");
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime, int tickCount, float scrollRows)
        {
            this.scrollRows = scrollRows;

            if (screenPad.GetState().Buttons.X == ButtonState.Released && screenPad.GetState().Buttons.X != lastButtonState)
            {
                reloaded = true;
            }
            
            lastButtonState = screenPad.GetState().Buttons.X;

            accumShootingTime = accumShootingTime.Add(gameTime.ElapsedGameTime);

            if (onWindowPosition != null)
            {
                if (accumElapsedGameTime >= TimeSpan.FromMilliseconds(tickInMS))
                {
                    if (speed < 1f)
                        Speed += acceleration;

                    accumElapsedGameTime = TimeSpan.FromSeconds(0);
                    onWindowPosition = onWindowPosition + speed * new Vector2(0, 1) + direction;
                }
                accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);
                Position = (onWindowPosition.Value / scrollRowHeight) + new Vector2(0, scrollRows);
            }

            if (IsOffScreen())
            {
                OnOffScreen(new EventArgs());
            }
        }

        public bool IsOffScreen()
        {
            var thisRectangle = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            var windowRectangle = new Rectangle(0, 0, (int)windowTilesSize.X, (int)windowTilesSize.Y);
            Rectangle intersectArea = Rectangle.Intersect(thisRectangle, windowRectangle);
            var isOffScreen = intersectArea.X * intersectArea.Y == 0;
            return isOffScreen;
        }
        
        protected virtual void OnOffScreen(EventArgs e)
        {
            if (OffScreen != null)
                OffScreen(this, e);
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
            if (onWindowPosition == null)
            {
                onWindowPosition = Position * tileWidth
                - new Vector2(0, scrollRowHeight * scrollRows);
            }

            var powerUpRectangle = new Rectangle((int)state * powerUpSpriteSheet.Height, 0, (int)(Size.X * tileWidth), (int)(Size.Y * tileWidth));
            spriteBatch.Draw(powerUpSpriteSheet
                , DestinationRectangle()
                , powerUpRectangle
                , Color.White);
        }

        public void Hit(Vector2 bulletPosition)
        {
            var powerUpStateIndex = (int)State;
            powerUpStateIndex++;
            if (powerUpStateIndex > 2)
                powerUpStateIndex = 0;

            State = (PowerUpState)powerUpStateIndex;

            Speed = -3f;

            direction = new Vector2(this.Position.X - bulletPosition.X, 1);

            Messenger.Default.Send(new PowerUpStateChangedMessage { PowerUp = this });
        }

        public void RestorePosition()
        {
            Position = StartPosition;
        }
    }

    public enum PowerUpState
    {
        Shield = 0,
        Invulnerable = 1,
        Invisible = 2        
    }

    public class PowerUpStateChangedMessage { public PowerUp PowerUp { get; set; } }
}
