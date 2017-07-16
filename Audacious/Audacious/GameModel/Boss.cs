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
    public class Boss : PhysicalObject
    {
        Texture2D bossSpriteSheet;
        Texture2D bossHitSpriteSheet;
        Texture2D destructionSpriteSheet;
        SoundEffectInstance fireSoundInstance;
        SoundEffectInstance hitSoundEffectInstance;
        public static Vector2 ScreenSize = new Vector2(800f, 480f);
        public static Vector2 GameScreenSize = new Vector2(512, 384);
        public float Lives = 20f;
        int walkDirectionX = 0;
        int respawnTimeOut = 20;
        int blinkTickStart = 0;
        Vector2 windowTilesSize;
        int levelNumber = 1;
        BossMovement bossMovement = BossMovement.WalkHorizontal;
        TimeSpan accumElapsedGameTime = TimeSpan.FromSeconds(0);
        int hitTimeoutInMS = 500;
        TimeSpan accumHitGameTime = TimeSpan.FromSeconds(1000);

        PlayerState state = PlayerState.Alive;
        public PlayerState State
        {
            get { return state; }
            set { state = value; }
        }

        public Boss(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 windowTilesSize, int levelNumber, BossMovement bossMovement)
        : base(content, spriteBatch, deviceScreenSize, screenPad)
        {
            this.windowTilesSize = windowTilesSize;
            this.levelNumber = levelNumber;
            this.bossMovement = bossMovement;
            bossSpriteSheet = bossSpriteSheet ?? GetTexture(string.Format("Boss{0}SpriteSheet", levelNumber));
            bossHitSpriteSheet = bossHitSpriteSheet ?? GetTexture(string.Format("Boss{0}HitSpriteSheet", levelNumber));
        }

        public override void LoadContent()
        {
            Size = new Vector2(6, 6);
            Position = new Vector2(14, 4);
            destructionSpriteSheet = destructionSpriteSheet ?? ContentHelper.Instance.GetContent<Texture2D>("BossDestructionSpriteSheet");
            hitSoundEffectInstance = hitSoundEffectInstance ?? ContentHelper.Instance.GetSoundEffectInstance(this.GetType().Name + "Hit");
            fireSoundInstance = fireSoundInstance ?? ContentHelper.Instance.GetSoundEffectInstance("Fire");
        }

        public override void Update(GameTime gameTime, int tickCount, float scrollRows)
        {
            UpdateBlink(tickCount);

            if (state == PlayerState.Alive)
            {
                switch(bossMovement)
                {
                    case BossMovement.WalkHorizontal:
                        WalkHorizontal(gameTime, tickCount);
                        break;
                    case BossMovement.FloatSenoidHorizontal:
                        FloatSenoidHorizontal(gameTime, tickCount);
                        break;
                    default:
                        WalkHorizontal(gameTime, tickCount);
                        break;

                }                
            }

            accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);

            if (accumHitGameTime.TotalMilliseconds < hitTimeoutInMS)
                accumHitGameTime = accumHitGameTime.Add(gameTime.ElapsedGameTime);
        }

        private void WalkRandomHorizontal(GameTime gameTime, int tickCount)
        {
            if (tickCount % 20 == 0)
            {
                walkDirectionX = (walkDirectionX + (new Random().Next(10) % 2) + 1) % 3 - 1;
            }

            if (tickCount % 4 == 0)
            {
                var newPosX = Position.X + walkDirectionX;
                if (newPosX > 2 && newPosX < windowTilesSize.X - 2 - this.Size.X)
                {
                    Position.X = newPosX;
                }
                else
                {
                    walkDirectionX *= -1;
                }
            }
        }

        private void WalkHorizontal(GameTime gameTime, int tickCount)
        {
            var x = (float)(-this.Size.X / 2
                + (windowTilesSize.X / 2)
                + (windowTilesSize.X / 4) * Math.Sin(Math.PI * (accumElapsedGameTime.TotalMilliseconds / 2000f)));

            Position = new Vector2(
                x
                , (float)(this.Size.Y / 2));
        }

        private void FloatSenoidHorizontal(GameTime gameTime, int tickCount)
        {
            var x = (float)(-this.Size.X / 2
                + (windowTilesSize.X / 2)
                + (windowTilesSize.X / 4) * Math.Sin(Math.PI * (accumElapsedGameTime.TotalMilliseconds/ 2000f)));

            Position = new Vector2(
                x
                , (float)(this.Size.Y / 2 + Math.Sin(2 * Math.PI * (accumElapsedGameTime.TotalMilliseconds / 2000f))));
        }

        public override void Draw(GameTime gameTime, int tickCount, float scrollRows)
        {
            var bossFrameCount = bossSpriteSheet.Width / (tileWidth * Size.Y);
            var destructionFrameCount = destructionSpriteSheet.Width / destructionSpriteSheet.Height;

            Rectangle bossRectangle = new Rectangle((int)(tickCount % bossFrameCount * Size.X * tileWidth)
                , 0
                , (int)(Size.X * tileWidth)
                , (int)(Size.Y * tileWidth));

            Rectangle destructionRectangle = new Rectangle((int)(tickCount % destructionFrameCount * destructionSpriteSheet.Height)
                , 0
                , (int)(destructionSpriteSheet.Height)
                , (int)(destructionSpriteSheet.Height));

            switch (State)
            {
                case PlayerState.Dead:
                    if (tickCount <= blinkTickStart + respawnTimeOut)
                    {
                        spriteBatch.Draw(destructionSpriteSheet
                            , TopLeftCorner
                            + Position * tileWidth
                                - new Vector2(0, scrollRowHeight * scrollRows)
                            , destructionRectangle
                            , Color.White);
                    }
                    break;
                case PlayerState.Alive:
                    if (accumHitGameTime.TotalMilliseconds < hitTimeoutInMS)
                        DrawHitBoss(scrollRows, bossRectangle);
                    else
                        DrawAliveBoss(scrollRows, bossRectangle);
                    break;
            }
        }

        private void DrawAliveBoss(float scrollRows, Rectangle bossRectangle)
        {
            spriteBatch.Draw(bossSpriteSheet,
                TopLeftCorner
                + Position * tileWidth
                - new Vector2(0, scrollRowHeight * scrollRows),
                bossRectangle,
                Color.White);
        }

        private void DrawHitBoss(float scrollRows, Rectangle bossRectangle)
        {
            spriteBatch.Draw(bossHitSpriteSheet,
                TopLeftCorner
                + Position * tileWidth
                - new Vector2(0, scrollRowHeight * scrollRows),
                bossRectangle,
                Color.White);
        }

        public void ProcessGamePad(ScreenPad screenPad, PhysicalObject gameMap)
        {
            //var candidatePos = this.Position;
            //var candidateXPos = this.Position;
            //var candidateYPos = this.Position;

            //if (Math.Abs(screenPad.LeftStick.X) + Math.Abs(screenPad.LeftStick.Y) > 0)
            //{
            //    screenPad.LeftStick.Normalize();
            //    candidatePos = this.Position + new Vector2(screenPad.LeftStick.X, -screenPad.LeftStick.Y) * .5f;
            //    candidateXPos = this.Position + new Vector2(screenPad.LeftStick.X, 0) * .5f;
            //    candidateYPos = this.Position + new Vector2(0, -screenPad.LeftStick.Y) * .5f;
            //}

            //var collisionResult = gameMap.TestCollision(this, candidatePos);
            //var collisionXResult = gameMap.TestCollision(this, candidateXPos);
            //var collisionYResult = gameMap.TestCollision(this, candidateYPos);
            //if (collisionResult.CollisionType == CollisionType.None)
            //{
            //    this.Position = candidatePos;
            //}
            //else if (collisionXResult.CollisionType == CollisionType.None)
            //{
            //    this.Position = candidateXPos;
            //}
            //else if (collisionYResult.CollisionType == CollisionType.None)
            //{
            //    this.Position = candidateYPos;
            //}
        }

        public void Hit()
        {
            Lives--;
            if (Lives <= 0)
            {
                State = PlayerState.Dead;
                fireSoundInstance.Play();
            }
            else
            {
                hitSoundEffectInstance.Volume = .4f;
                hitSoundEffectInstance.Play();
                accumHitGameTime = TimeSpan.FromMilliseconds(0);
            }
        }

        protected void UpdateBlink(int tickCount)
        {
            if (blinkTickStart == 0 && state == PlayerState.Dead)
            {
                blinkTickStart = tickCount;
                //NewMessenger.Default.Send(new BossDeathMessage { Boss = this });
            }
            else if (state == PlayerState.Dead && tickCount > blinkTickStart + respawnTimeOut)
            {
                state = PlayerState.Terminated;
                //blinkTickStart = 0;
                NewMessenger.Default.Send(new BossDeathMessage { Boss = this });
            }
        }
    }

    public enum BossMovement
    {
        WalkHorizontal,
        FloatSenoidHorizontal
    }

    public class BossDeathMessage { public Boss Boss { get; set; } }
}
