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
    public class Bullet : PhysicalObject
    {
        protected Texture2D bulletSpriteSheet;
        protected SoundEffectInstance soundEffectInstance;
        protected bool shouldPlaySound = true;

        protected float damage = 1;
        public float Damage
        {
            get { return damage; }
            set { damage = value; }
        }

        protected Vector2 direction = new Vector2(0, 1);
        public Vector2 Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        protected float rotation = 0;
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        protected float speed = .15f;
        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        public event EventHandler OffScreen;

        public Bullet(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage)
            : base(content, spriteBatch, deviceScreenSize, screenPad)
        {
            Size = new Vector2(2, 2);
            this.damage = damage;
            soundEffectInstance.Volume = .6f;
        }

        public override void LoadContent()
        {
            bulletSpriteSheet = bulletSpriteSheet ?? ContentHelper.Instance.GetContent<Texture2D>(this.GetType().Name + "SpriteSheet");
            soundEffectInstance = soundEffectInstance ?? ContentHelper.Instance.GetSoundEffectInstance(this.GetType().Name + "Shooting");
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime, int tickCount, float scrollRows)
        {
            Position = Position + speed * direction;
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

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime, int tickCount, float scrollRows)
        {
            var spriteCount = bulletSpriteSheet.Width / bulletSpriteSheet.Height;
            var bulletStep = tickCount % spriteCount;
            var pos = TopLeftCorner + (Position) * tileWidth;
            var destinationRectangle = new Rectangle((int)pos.X + (int)(Size.X * tileWidth / 2f), (int)pos.Y, (int)Size.X * tileWidth, (int)Size.Y * tileWidth);

            spriteBatch.Draw(bulletSpriteSheet
                , destinationRectangle
                , new Rectangle((int)(bulletStep * Size.X * tileWidth), 0, (int)Size.X * tileWidth, (int)Size.Y * tileWidth)
                , Color.White
                , rotation
                , new Vector2(Size.X * tileWidth / 2f, Size.Y * tileWidth / 2f)
                , SpriteEffects.None
                , 0);
        }

        protected virtual void OnOffScreen(EventArgs e)
        {
            if (OffScreen != null)
                OffScreen(this, e);
        }

        public void Shoot()
        {
            if (shouldPlaySound)
            {
                soundEffectInstance.Stop();
                soundEffectInstance.Play();
            }
        }
    }

    public class PlayerBullet : Bullet
    {
        protected Player player;
        protected ScreenPad screenPad;
        public PlayerBullet(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage, Player player)
            : base(content, spriteBatch, deviceScreenSize, screenPad, damage)
        {
            Size = new Vector2(2, 2);
            speed = .4f;
            this.damage = damage;
            this.player = player;
            this.screenPad = screenPad;
            direction = new Vector2(0, -1);
        }
    }

    public class PlayerBullet1 : PlayerBullet
    {
        public PlayerBullet1(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage, Player player)
        : base(content, spriteBatch, deviceScreenSize, screenPad, damage, player)
        {
        }
    }

    public class PlayerBullet2 : PlayerBullet
    {
        public PlayerBullet2(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage, Player player)
            : base(content, spriteBatch, deviceScreenSize, screenPad, damage, player)
        {
        }
    }

    public class PlayerBullet3 : PlayerBullet
    {
        public PlayerBullet3(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage, Player player)
            : base(content, spriteBatch, deviceScreenSize, screenPad, damage, player)
        {
        }
    }

    public class PlayerBullet4 : PlayerBullet
    {
        public PlayerBullet4(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage, Player player)
            : base(content, spriteBatch, deviceScreenSize, screenPad, damage, player)
        {
        }
    }

    public class PlayerBullet5 : PlayerBullet
    {
        public PlayerBullet5(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage, Player player, Vector2 direction, float rotation, bool shouldPlaySound)
            : base(content, spriteBatch, deviceScreenSize, screenPad, damage, player)
        {
            this.direction = direction;
            this.rotation = rotation;
            this.shouldPlaySound = shouldPlaySound;
        }
    }

    public class PlayerBullet6 : PlayerBullet
    {
        BoomerangDirection boomerangDirection = BoomerangDirection.Up;
        float acceleration = 0f;

        public PlayerBullet6(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad, float damage, Player player)
            : base(content, spriteBatch, deviceScreenSize, screenPad, damage, player)
        {
            speed = 20f;
        }

        public override void Update(GameTime gameTime, int tickCount, float scrollRows)
        {
            var t = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Speed += acceleration;

            Position = Position + (float)(Speed * t) * Direction;
            Position = new Vector2(player.Position.X, Position.Y);
            if (IsOffScreen() || Position.Y >= player.Position.Y)
            {
                OnOffScreen(new EventArgs());
            }

            if (boomerangDirection == BoomerangDirection.Up && screenPad.GetState().Buttons.X == ButtonState.Released)
            {
                acceleration = -2;
                boomerangDirection = BoomerangDirection.Down;
            }
        }
    }

    public enum BoomerangDirection
    {
        Up,
        Down
    }
}
