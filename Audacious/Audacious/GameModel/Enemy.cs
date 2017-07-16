using GalaSoft.MvvmLight.Messaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using ScreenControlsSample;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audacious.GameModel
{
    public class Enemy : PhysicalObject
    {
        static SoundEffectInstance fireSoundInstance;
        protected int tickInMS = 20;
        protected int changeDirectionInMS = 500;
        protected TimeSpan accumElapsedGameTime = TimeSpan.FromSeconds(0);
        protected int onWindowTicks = 0;
        Texture2D destructionSpriteSheet;
        Texture2D comboSpriteSheet;
        int respawnTimeOut = 10;
        protected int shotTimeOutMs = 5000;
        TimeSpan accumShotTime = TimeSpan.FromMilliseconds(0);
        int blinkTickStart = 0;
        float scrollRows = 0;

        protected Vector2? onWindowPosition;
        public Vector2? OnWindowPosition
        {
            get
            {
                return onWindowPosition;
            }
            set
            {
                onWindowPosition = value;
                if (value != null)
                {
                    Position = (onWindowPosition.Value / scrollRowHeight) + new Vector2(0, this.scrollRows);
                }
            }
        }

        protected char charCode = 'a';
        public char CharCode
        {
            get
            {
                return charCode;
            }
            set
            {
                charCode = value;
            }
        }

        bool reloaded = true;
        public bool Reloaded
        {
            get { return reloaded; }
            set { reloaded = value; }
        }

        float lives = 1f;
        public float Lives
        {
            get { return lives; }
            set { lives = value; }
        }

        Vector2 direction = new Vector2(0, 1);
        public Vector2 Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        float speed = 1.5f;
        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        PlayerState state = PlayerState.Alive;
        public PlayerState State
        {
            get { return state; }
            set
            {
                state = value;
                if (state == PlayerState.Combo)
                {
                    direction = new Vector2(0, -1);
                    speed = 3;
                }
            }
        }

        int groupId = 0;
        public int GroupId
        {
            get { return groupId; }
            set { groupId = value; }
        }

        bool isBullet = false;
        public bool IsBullet
        {
            get { return isBullet; }
            set { isBullet = value; }
        }

        bool isFlying = false;
        public bool IsFlying
        {
            get { return isFlying; }
            set { isFlying = value; }
        }

        bool isPassingBy = true;
        public bool IsPassingBy
        {
            get { return isPassingBy; }
            set { isPassingBy = value; }
        }

        public Enemy(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad)
        {
            Size = new Vector2(2, 2);
            StartPosition =
            Position = position;
            this.groupId = groupId;
        }

        protected Texture2D enemySpriteSheet;
        public override void LoadContent()
        {
            enemySpriteSheet = enemySpriteSheet ?? GetTexture(this.GetType().Name + "SpriteSheet");
            destructionSpriteSheet = destructionSpriteSheet ?? GetTexture("DestructionSpriteSheet");
            comboSpriteSheet = comboSpriteSheet ?? GetTexture("ComboSpriteSheet");
            fireSoundInstance = fireSoundInstance ?? ContentHelper.Instance.GetContent<SoundEffect>("Fire").CreateInstance();
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime, int tickCount, float scrollRows)
        {
        }

        public virtual void Update(Microsoft.Xna.Framework.GameTime gameTime, int tickCount, float scrollRows, List<Enemy> onScreenEnemies, Map gameMap)
        {
            this.scrollRows = scrollRows;
            UpdateBlink(tickCount);

            if ((State == PlayerState.Alive || State == PlayerState.Combo) && onWindowPosition != null)
            {
                if (!reloaded && accumShotTime >= TimeSpan.FromMilliseconds(shotTimeOutMs))
                {
                    accumShotTime = TimeSpan.FromSeconds(0);
                    reloaded = true;
                }
                accumShotTime = accumShotTime.Add(gameTime.ElapsedGameTime);

                if (onWindowPosition.HasValue)
                {
                    if (accumElapsedGameTime >= TimeSpan.FromMilliseconds(tickInMS))
                    {
                        if (gameMap.State == MapState.Scrolling)
                        {
                            accumElapsedGameTime = TimeSpan.FromSeconds(0);
                            onWindowTicks++;
                            Vector2? candidateWindowPosition;

                            var selectedSpeed = (OnWindowPosition.Value.Y < this.Size.Y * tileWidth * 2) ? 1 : speed;

                            if (State == PlayerState.Combo)
                                candidateWindowPosition = onWindowPosition + speed * new Vector2(0, -1);
                            else
                                candidateWindowPosition = onWindowPosition + speed * direction;

                            var mapCollisionType = CheckMapCollision(gameMap, candidateWindowPosition);
                            if (mapCollisionType == CollisionType.None
                                || candidateWindowPosition.Value.Y < 0
                                || isFlying
                                || (IsPassingBy && mapCollisionType != CollisionType.Blocked)
                                || OnWindowPosition.Value.Y < this.Size.Y * tileWidth * 2)
                            {
                                var collidedWithEnemy = CheckEnemyCollision(onScreenEnemies, candidateWindowPosition);

                                if (!collidedWithEnemy || isFlying || OnWindowPosition.Value.Y < this.Size.Y * tileWidth * 2)
                                    onWindowPosition = candidateWindowPosition;
                                else
                                {
                                    onWindowPosition = new Vector2((int)(onWindowPosition.Value.X * 10) / 10f, (int)(onWindowPosition.Value.Y * 10) / 10f);
                                }
                            }
                        }
                    }

                    accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);
                    Position = (onWindowPosition.Value / scrollRowHeight) + new Vector2(0, scrollRows);
                    //Position = new Vector2((int)(Position.X * 10) / 10f, (int)(Position.Y * 10) / 10f);
                }
            }
        }

        private bool CheckEnemyCollision(List<Enemy> onScreenEnemies, Vector2? candidateWindowPosition)
        {
            var collided = false;
            if (!this.isBullet && this.State == PlayerState.Alive)
            {
                foreach (var enemy in onScreenEnemies)
                {
                    if (!enemy.isFlying && enemy != this && enemy.onWindowPosition != null)
                    {
                        var thisRectangle = new Rectangle((int)candidateWindowPosition.Value.X, (int)candidateWindowPosition.Value.Y, (int)Size.X * tileWidth, (int)Size.Y * tileWidth);
                        var thatRectangle = new Rectangle((int)enemy.onWindowPosition.Value.X, (int)enemy.onWindowPosition.Value.Y, (int)enemy.Size.X * tileWidth, (int)enemy.Size.Y * tileWidth);
                        Rectangle intersectArea = Rectangle.Intersect(thisRectangle, thatRectangle);
                        collided = intersectArea.X * intersectArea.Y > 0;

                        if (collided)
                        {
                            break;
                        }
                    }
                }
            }
            return collided;
        }

        private CollisionType CheckMapCollision(Map gameMap, Vector2? candidateWindowPosition)
        {
            CollisionResult mapCollisionResult = new CollisionResult { CollisionType = CollisionType.None };
            if (!this.isBullet)
            {
                var candidatePosition = (candidateWindowPosition.Value / scrollRowHeight);

                mapCollisionResult = gameMap.TestCollision(this, candidatePosition, gameMap.ScrollRows);
            }
            return mapCollisionResult.CollisionType;
        }

        public virtual void UpdateDirection(Player player, Map gameMap)
        {

        }

        protected void UpdateBlink(int tickCount)
        {
            if (blinkTickStart == 0 && (state == PlayerState.Dead || state == PlayerState.Combo))
            {
                blinkTickStart = tickCount;
            }
            else if (state == PlayerState.Dead && tickCount > blinkTickStart + respawnTimeOut)
            {
                Messenger.Default.Send(new EnemyDeathMessage { Enemy = this });
            }
        }

        public override void Draw(GameTime gameTime, int tickCount, float scrollRows)
        {
            if (onWindowPosition == null)
            {
                onWindowPosition = Position * tileWidth
                - new Vector2(0, scrollRowHeight * scrollRows);
            }

            Rectangle enemyRectangle;
            switch (State)
            {
                case GameModel.PlayerState.Combo:
                    var comboStepCount = comboSpriteSheet.Width / (tileWidth * Size.Y);
                    enemyRectangle = new Rectangle((int)(((tickCount - blinkTickStart) % comboStepCount) * (int)(Size.X * tileWidth)), 0, (int)(Size.X * tileWidth), (int)(Size.Y * tileWidth));
                    spriteBatch.Draw(comboSpriteSheet
                        , DestinationRectangle()
                        , enemyRectangle
                        , Color.White);
                    break;
                case GameModel.PlayerState.Dead:
                    if (tickCount <= blinkTickStart + respawnTimeOut)
                    {
                        var destructionStepCount = destructionSpriteSheet.Width / (tileWidth * Size.Y);
                        enemyRectangle = new Rectangle((int)(((tickCount - blinkTickStart) % destructionStepCount) * (int)(Size.X * tileWidth)), 0, (int)(Size.X * tileWidth), (int)(Size.Y * tileWidth));
                        spriteBatch.Draw(destructionSpriteSheet
                            , DestinationRectangle()
                            , enemyRectangle
                            , Color.White);
                    }
                    break;
                default:
                    DrawAliveEnemy(gameTime, tickCount, scrollRows);
                    break;
            }
        }

        protected virtual void DrawAliveEnemy(GameTime gameTime, int tickCount, float scrollRows)
        {
            if (IsSpriteVisible())
            {
                var spriteCount = enemySpriteSheet.Width / enemySpriteSheet.Height;
                var bulletStep = tickCount % spriteCount;
                spriteBatch.Draw(enemySpriteSheet
                    , DestinationRectangle()
                    , new Rectangle((int)(bulletStep * Size.X * tileWidth), 0, (int)Size.X * tileWidth, (int)Size.Y * tileWidth)
                    , Color.White);
            }
        }

        public bool IsSpriteVisible()
        {
            if (onWindowPosition == null)
                return false;

            var windowRectangle = new Rectangle(
                (int)TopLeftCorner.X
                , (int)TopLeftCorner.Y
                , (int)windowTilesSize.X * tileWidth
                , (int)windowTilesSize.Y * tileWidth);

            return windowRectangle.Intersects(DestinationRectangle());
        }

        protected virtual Rectangle DestinationRectangle()
        {
            var destinationRectangle = new Rectangle(
                      (int)(TopLeftCorner.X + onWindowPosition.Value.X)
                    , (int)(TopLeftCorner.Y + onWindowPosition.Value.Y)
                    , (int)(Size.X * tileWidth)
                    , (int)(Size.Y * tileWidth));

            return destinationRectangle;
        }

        protected Rectangle ShadowDestinationRectangle()
        {
            var destinationRectangle = new Rectangle(
                      (int)(TopLeftCorner.X + onWindowPosition.Value.X)
                    , (int)(TopLeftCorner.Y + onWindowPosition.Value.Y + tileWidth)
                    , (int)(Size.X * tileWidth)
                    , (int)(Size.Y * tileWidth));
            return destinationRectangle;
        }

        public virtual void Hit(Bullet bullet, int tickCount)
        {
            if (State == PlayerState.Alive)
            {
                var temp = bullet.Damage;
                bullet.Damage -= this.Lives;
                this.lives -= temp;
                if (this.lives <= 0)
                {
                    ProcessDeath(tickCount);
                }
            }
        }

        public void ProcessDeath(int tickCount, bool respawn = false)
        {
            State = PlayerState.Dead;
            if (fireSoundInstance.State == SoundState.Stopped)
                fireSoundInstance.Play();
        }

        public virtual void RestorePosition()
        {
            Lives = 1;
            Position = StartPosition;
            onWindowPosition = null;
            direction = new Vector2(0, 1);
            blinkTickStart = 0;
        }

        public virtual EnemyBullet GetBullet(Player player, Map gameMap)
        {
            var bullet = new EnemyBullet(content, spriteBatch, deviceScreenSize, screenPad, this.Position, 0);
            bullet.Direction = new Vector2(0, 2f);
            return bullet;
        }
    }

    public class Enemy2 : Enemy
    {
        Vector2? onWindowStartPosition;
        float angle = 0f;
        protected float rangeWidth = .25f;
        protected float ticksToYRate = 20;

        public Enemy2(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'b';
            Size = new Vector2(2, 2);
            Speed = 2f;
            Position = position;
            IsFlying = true;
            IsPassingBy = true;
        }

        Texture2D shadowSpriteSheet;
        public override void LoadContent()
        {
            base.LoadContent();
            shadowSpriteSheet = GetTexture("ShadowSpriteSheet");
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime, int tickCount, float scrollRows, List<Enemy> onScreenEnemies, Map gameMap)
        {
            UpdateBlink(tickCount);

            if ((State == PlayerState.Alive || State == PlayerState.Combo) && onWindowPosition != null)
            {
                if (onWindowStartPosition == null)
                    onWindowStartPosition = onWindowPosition;

                if (accumElapsedGameTime >= TimeSpan.FromMilliseconds(tickInMS / 20f))
                {
                    if (gameMap.State == MapState.Scrolling)
                    {
                        if (State == PlayerState.Combo)
                        {
                            onWindowPosition = onWindowPosition + Speed * new Vector2(0, -1);
                        }
                        else
                        {
                            var rad = ((onWindowTicks % 360) / 360f) * 2 * Math.PI;

                            onWindowPosition +=
                                new Vector2((float)Math.Cos(rad), .5f);

                        }
                        accumElapsedGameTime = TimeSpan.FromSeconds(0);
                        onWindowTicks++;
                    }
                }
                accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);

                Position = (onWindowPosition.Value / scrollRowHeight) + new Vector2(0, (int)(scrollRows));
            }
        }

        protected override void DrawAliveEnemy(GameTime gameTime, int tickCount, float scrollRows)
        {
            base.DrawAliveEnemy(gameTime, tickCount, scrollRows);
            if (IsSpriteVisible())
            {
                spriteBatch.Draw(shadowSpriteSheet
                    , ShadowDestinationRectangle()
                    , new Rectangle(0, 0, (int)Size.X * tileWidth, (int)Size.Y * tileWidth)
                    , Color.White);
            }
        }

        public override EnemyBullet GetBullet(Player player, Map gameMap)
        {
            var bullet = new EnemyBullet2(content, spriteBatch, deviceScreenSize, screenPad, this.Position, 0);
            var newDirection = new Vector2(player.Position.X - this.Position.X, (player.Position.Y + gameMap.ScrollRows) - this.Position.Y) + player.Size / 2;
            newDirection.Normalize();
            bullet.Direction = newDirection;
            return bullet;
        }

        public override void RestorePosition()
        {
            base.RestorePosition();
            onWindowTicks = 0;
            angle = 0f;
        }
    }

    public class Enemy3 : Enemy
    {
        public Enemy3(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'c';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
            IsPassingBy = true;
        }

        public override EnemyBullet GetBullet(Player player, Map gameMap)
        {
            var bullet = new EnemyBullet3(content, spriteBatch, deviceScreenSize, screenPad, this.Position, 0);
            var newDirection = new Vector2(player.Position.X - this.Position.X, (player.Position.Y + gameMap.ScrollRows) - this.Position.Y) + player.Size / 2;
            newDirection.Normalize();
            bullet.Direction = newDirection;
            return bullet;
        }
    }

    public class Enemy4 : Enemy2
    {
        public Enemy4(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'd';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
            rangeWidth = .125f;
            ticksToYRate = 10;
        }
    }

    public class Enemy5 : Enemy
    {
        bool isDisassembling = false;
        bool isAssembling = false;
        float assemblyTimeInMS = 500;
        float disassemblyTimeInMS = 500;
        float maxSplitDistance = 32;
        float splitDistance = 0;
        Texture2D splitSpriteSheet;

        public Enemy5(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'e';
            Size = new Vector2(2, 2);
            Speed = 2f;
            Position = position;
            IsPassingBy = false;
            Lives = 3;
        }

        public override void LoadContent()
        {
            base.LoadContent();
            splitSpriteSheet = GetTexture("splitSpriteSheet");
        }

        public override void UpdateDirection(Player player, Map gameMap)
        {
            if ((this.Position.X / 2) == (int)(this.Position.X / 2)
                || (this.Position.Y / 2) == (int)(this.Position.Y / 2))
            {
                var newDirection = new Vector2(player.Position.X - this.Position.X, (player.Position.Y + gameMap.ScrollRows) - this.Position.Y) + player.Size / 2;
                if (onWindowTicks / (changeDirectionInMS / tickInMS) % 2 == 0)
                    newDirection = new Vector2(newDirection.X, 0);
                else
                    newDirection = new Vector2(0, newDirection.Y);

                newDirection.Normalize();
                this.Direction = newDirection;
            }
        }

        public override void Hit(Bullet bullet, int tickCount)
        {
            if (!(isDisassembling || isAssembling))
            {
                base.Hit(bullet, tickCount);

                if (this.Lives > 0)
                    isDisassembling = true;
            }
        }

        public override void Update(GameTime gameTime, int tickCount, float scrollRows, List<Enemy> onScreenEnemies, Map gameMap)
        {
            if (isDisassembling)
            {
                if (gameMap.State == MapState.Scrolling)
                {
                    if (accumElapsedGameTime >= TimeSpan.FromMilliseconds(tickInMS))
                    {
                        splitDistance = splitDistance * 1.05f + 1;
                    }

                    if (splitDistance >= maxSplitDistance)
                    {
                        accumElapsedGameTime = TimeSpan.FromSeconds(0);
                        isDisassembling = false;
                        isAssembling = true;
                    }
                    else
                        accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);
                }
            }
            else if (isAssembling)
            {
                if (gameMap.State == MapState.Scrolling)
                {
                    if (accumElapsedGameTime >= TimeSpan.FromMilliseconds(tickInMS))
                    {
                        splitDistance = splitDistance / 1.05f - 1;
                    }

                    if (splitDistance <= 0)
                    {
                        accumElapsedGameTime = TimeSpan.FromSeconds(0);
                        isDisassembling = false;
                        isAssembling = false;
                    }
                    else
                        accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);
                }
            }
            else
            {
                base.Update(gameTime, tickCount, scrollRows, onScreenEnemies, gameMap);
            }
        }

        protected override void DrawAliveEnemy(GameTime gameTime, int tickCount, float scrollRows)
        {
            if (isDisassembling || isAssembling)
            {
                if (IsSpriteVisible())
                {
                    var timeInMS = isDisassembling ? disassemblyTimeInMS : assemblyTimeInMS;

                    spriteBatch.Draw(splitSpriteSheet
                        , new Rectangle((int)(TopLeftCorner.X + onWindowPosition.Value.X
                                            - splitDistance)
                                        , (int)(TopLeftCorner.Y + onWindowPosition.Value.Y)
                                        , (int)Size.X * tileWidth
                                        , (int)Size.Y * tileWidth)
                        , new Rectangle(
                            0
                            , 0
                            , (int)Size.X * tileWidth
                            , (int)Size.Y * tileWidth)
                        , Color.White
                        , - (float)((splitDistance / maxSplitDistance) * Math.PI / 6)
                        , new Vector2(tileWidth * Size.X / 2, 0)
                        , SpriteEffects.None
                        , 0);

                    spriteBatch.Draw(splitSpriteSheet
                        , new Rectangle((int)(TopLeftCorner.X + onWindowPosition.Value.X + (Size.X * tileWidth) / 2
                                            + splitDistance)
                                        , (int)(TopLeftCorner.Y + onWindowPosition.Value.Y)
                                        , (int)Size.X * tileWidth
                                        , (int)Size.Y * tileWidth)
                        , new Rectangle(
                            (int)Size.X * tileWidth
                            , 0
                            , (int)Size.X * tileWidth
                            , (int)Size.Y * tileWidth)
                        , Color.White
                        , (float)((splitDistance / maxSplitDistance) * Math.PI / 6)
                        , new Vector2(tileWidth * Size.X / 2, 0)
                        , SpriteEffects.None
                        , 0);
                }
            }
            else
            {
                base.DrawAliveEnemy(gameTime, tickCount, scrollRows);
            }
        }

        public override void RestorePosition()
        {
            base.RestorePosition();
            Lives = 3;
            isDisassembling = false;
            isAssembling = false;
            splitDistance = 0;
            Speed = 2f;
            IsPassingBy = false;
            Lives = 3;
        }
    }

    public class Enemy6 : Enemy
    {
        public Enemy6(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'f';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
        }
    }

    public class Enemy7 : Enemy
    {
        public Enemy7(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'g';
            Size = new Vector2(2, 2);
            Speed = 2f;
            Position = position;
            IsPassingBy = false;
        }

        public override void UpdateDirection(Player player, Map gameMap)
        {
            if ((this.Position.X / 2) == (int)(this.Position.X / 2)
                || (this.Position.Y / 2) == (int)(this.Position.Y / 2))
            {
                var newDirection = new Vector2(player.Position.X - this.Position.X, (player.Position.Y + gameMap.ScrollRows) - this.Position.Y) + player.Size / 2;
                if (onWindowTicks / (changeDirectionInMS / tickInMS) % 2 == 0)
                    newDirection = new Vector2(newDirection.X, 0);
                else
                    newDirection = new Vector2(0, newDirection.Y);

                newDirection.Normalize();
                this.Direction = newDirection;
            }
        }
    }

    public class Enemy8 : Enemy
    {
        public Enemy8(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'h';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
        }
    }

    public class Enemy9 : Enemy
    {
        public Enemy9(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'i';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
        }
    }

    public class Enemy10 : Enemy
    {
        public Enemy10(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'j';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
        }
    }

    public class Enemy11 : Enemy3
    {
        public Enemy11(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'k';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
            IsPassingBy = false;
        }

        public override void UpdateDirection(Player player, Map gameMap)
        {
            if (State == PlayerState.Alive
                && gameMap.State == MapState.Scrolling
                && ((this.Position.X / 2) == (int)(this.Position.X / 2)
                    || (this.Position.Y / 2) == (int)(this.Position.Y / 2)))
            {
                var target = new Vector2(player.Position.X >= windowTilesSize.X / 2 ? 0 : windowTilesSize.X, (player.Position.Y + gameMap.ScrollRows));

                var newDirection = new Vector2(target.X - this.Position.X, target.Y - this.Position.Y) + this.Size / 2;
                if (onWindowTicks / (changeDirectionInMS / tickInMS) % 2 == 0)
                    newDirection = new Vector2(newDirection.X, 0);
                else
                    newDirection = new Vector2(0, newDirection.Y);

                newDirection.Normalize();
                this.Direction = newDirection;
            }
        }
    }



    public class Enemy12 : Enemy
    {
        bool wasHit = false;
        bool isDescending = true;

        public Enemy12(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'l';
            Size = new Vector2(2, 2);
            Speed = 1f;
            Position = position;
            Lives = 1000;
            IsPassingBy = true;
            IsFlying = true;
            Direction = new Vector2(position.X < windowTilesSize.X / 2 ? -1 : 1, 0);
        }

        public override void Update(GameTime gameTime, int tickCount, float scrollRows, List<Enemy> onScreenEnemies, Map gameMap)
        {
            base.Update(gameTime, tickCount, scrollRows, onScreenEnemies, gameMap);

            if (onWindowPosition.HasValue)
            {
                if (!isDescending != onWindowPosition.Value.Y >= this.Size.Y * 2)
                {
                    isDescending = false;
                    Direction = new Vector2(1, 0);
                }
            }
        }

        public override void UpdateDirection(Player player, Map gameMap)
        {
            if (wasHit)
            {
                Direction = new Vector2(0, 1);
                Speed = 10;
            }
            else
            {
                if (isDescending)
                {
                    Direction = new Vector2(0, 1);
                    Speed = 1;
                }
                else
                {
                    if (Position.X <= windowTilesSize.X / 4)
                        Direction = new Vector2(1, 0);
                    else if (Position.X >= (windowTilesSize.X - windowTilesSize.X / 4))
                        Direction = new Vector2(-1, 0);
                }
            }
        }

        public override void Hit(Bullet bullet, int tickCount)
        {
            if (!isDescending)
            {
                base.Hit(bullet, tickCount);

                wasHit = true;
            }
        }

        public override void RestorePosition()
        {
            base.RestorePosition();
            Lives = 1000;
            wasHit = false;
            isDescending = false;
        }
    }

    public class Enemy13 : Enemy
    {
        public Enemy13(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            charCode = 'm';
            Size = new Vector2(2, 2);
            Speed = 3f;
            Position = position;
        }
    }
    public class EnemyDeathMessage { public Enemy Enemy { get; set; } }
    public class EnemyShotMessage { public Enemy Enemy { get; set; } }

    public class EnemyBullet : Enemy
    {
        public EnemyBullet(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            Size = new Vector2(2, 2);
            Speed = 2f;
            Position = position;
            IsBullet = true;
        }

        public override void Hit(Bullet bullet, int tickCount)
        {
            //a bullet was hit? Nothing happens.
        }
    }

    public class EnemyBullet2 : EnemyBullet
    {
        public EnemyBullet2(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            Size = new Vector2(2, 2);
            Speed = 4f;
            Position = position;
            IsBullet = true;
        }
    }

    public class EnemyBullet3 : EnemyBullet
    {
        public EnemyBullet3(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, Vector2 position, int groupId)
            : base(content, spriteBatch, deviceScreenSize, screenPad, position, groupId)
        {
            Size = new Vector2(2, 2);
            Speed = 5f;
            Position = position;
            IsBullet = true;
        }
    }
}
