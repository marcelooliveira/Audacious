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
    public class Player : PhysicalObject
    {
        Texture2D playerSpriteSheet;
        Texture2D destructionSpriteSheet;
        SoundEffectInstance fireSoundInstance;
        public static Vector2 ScreenSize = new Vector2(800f, 480f);
        public static Vector2 GameScreenSize = new Vector2(512, 384);
        public float Lives = 3f;
        int respawnTimeOut = 20;
        int blinkTickStart = 0;
                
        bool canShoot = true;
        public bool CanShoot
        {
            get { return canShoot; }
            set { canShoot = value; }
        }

        PlayerState state = PlayerState.Alive;
        public PlayerState State
        {
            get { return state; }
            set { state = value; }
        }

        Vector2 savedPosition = new Vector2(8, 18);
        public Vector2 SavedPosition
        {
            get { return savedPosition; }
            set { savedPosition = value; }
        }

        public Player(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad)
        : base(content, spriteBatch, deviceScreenSize, screenPad)
        {
        }

        public override void LoadContent()
        {
            Size = new Vector2(2f, 2f);
            playerSpriteSheet = playerSpriteSheet ?? ContentHelper.Instance.GetContent<Texture2D>("PlayerSpriteSheet");
            destructionSpriteSheet = destructionSpriteSheet ?? ContentHelper.Instance.GetContent<Texture2D>("PlayerDestructionSpriteSheet");
            fireSoundInstance = fireSoundInstance ?? ContentHelper.Instance.GetSoundEffectInstance("Fire");
        }

        public override void Update(GameTime gameTime, int tickCount, float scrollRows)
        {
            if (Position.Y >= windowTilesSize.Y)
            {
                ProcessDeath(tickCount, true);
            }

            if (blinkTickStart == 0 && state == PlayerState.Dead)
            {
                blinkTickStart = tickCount;
            }
            else if (state == PlayerState.Dead && tickCount > blinkTickStart + respawnTimeOut)
            {
                State = PlayerState.Alive;
            }
        }

        public void ProcessDeath(int tickCount, bool respawn = false)
        {
            State = PlayerState.Dead;
            CanShoot = true;
            fireSoundInstance.Play();
            Lives--;
            blinkTickStart = tickCount;

            if (respawn)
                Respawn(tickCount);

            Messenger.Default.Send(new PlayerDeathMessage { RemainingLives = (int)Lives });
        }

        private void Respawn(int tickCount)
        {
            Position.X = windowTilesSize.X / 2;
            Position.Y = windowTilesSize.Y - this.Size.Y - 1;
        }

        public override void Draw(GameTime gameTime, int tickCount, float scrollRows)
        {
            Rectangle playerRectangle;
            switch(State)
            {
                case GameModel.PlayerState.Dead:
                    var destructionStepCount = destructionSpriteSheet.Width / (tileWidth * Size.Y);
                    playerRectangle = new Rectangle((int)((tickCount - blinkTickStart) * (int)(Size.X * tileWidth)), 0, (int)(Size.X * tileWidth), (int)(Size.Y * tileWidth));
                    spriteBatch.Draw(destructionSpriteSheet, TopLeftCorner + Position * tileWidth + new Vector2(0, (tickCount % 2) * 4), playerRectangle, Color.White);
                    break;
                default:
                    var frameIndex = 0;
                    if (screenPad.LeftStick.X + screenPad.LeftStick.Y == 0)
                        frameIndex = (tickCount / 2) % 2;
                    else
                        frameIndex = 
                            (frameIndex 
                            + (int)(Position.X)
                            + (int)(Position.Y)) % 2;
                    playerRectangle = new Rectangle(frameIndex * (int)(Size.X * tileWidth), 0, (int)(Size.X * tileWidth), (int)(Size.Y * tileWidth));
                    spriteBatch.Draw(playerSpriteSheet, TopLeftCorner + Position * tileWidth + new Vector2(0, frameIndex * 2), playerRectangle, Color.White);
                    break;
            }
        }

        public override CollisionResult TestCollision(PhysicalObject that, Vector2 thatNewPosition, float scrollRows)
        {
            return base.TestCollision(that, thatNewPosition, scrollRows);
        }

        public void ProcessGamePad(ScreenPad screenPad, PhysicalObject gameMap, float scrollRows)
        {
            var candidatePos = this.Position;
            var candidateXPos = this.Position;
            var candidateYPos = this.Position;

            if (Math.Abs(screenPad.LeftStick.X) + Math.Abs(screenPad.LeftStick.Y) > 0)
            {
                screenPad.LeftStick.Normalize();
                candidatePos = this.Position + new Vector2(screenPad.LeftStick.X, -screenPad.LeftStick.Y) * .25f;
                candidateXPos = this.Position + new Vector2(screenPad.LeftStick.X, 0) * .25f;
                candidateYPos = this.Position + new Vector2(0, -screenPad.LeftStick.Y) * .25f;
            }

            var collisionResult = gameMap.TestCollision(this, candidatePos, scrollRows);
            var collisionXResult = gameMap.TestCollision(this, candidateXPos, scrollRows);
            var collisionYResult = gameMap.TestCollision(this, candidateYPos, scrollRows);
            if (collisionResult.CollisionType == CollisionType.None)
            {
                this.Position = candidatePos;
            }
            else if (collisionXResult.CollisionType == CollisionType.None)
            {
                this.Position = candidateXPos;
            }
            else if (collisionYResult.CollisionType == CollisionType.None)
            {
                this.Position = candidateYPos;
            }
        }

        public void Initialize()
        {
            Respawn();
            Lives = 3f;
            savedPosition = 
            Position =
                new Vector2(8, 18);
        }

        public void SavePosition()
        {
            savedPosition = this.Position;
        }

        public void Respawn()
        {
            CanShoot = true;
            State = PlayerState.Alive;
            Position = SavedPosition;
        }
    }

    public enum PlayerState
    {
        Alive,
        Dead,
        Terminated,
        Combo
    }

    public class PlayerDeathMessage
    {
        public int RemainingLives { get; set; }
    }
}
