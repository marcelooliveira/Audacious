using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using ScreenControlsSample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xInput = Microsoft.Xna.Framework.Input;

namespace Audacious.GameModel
{
    public abstract class PhysicalObject
    {
        public Guid Guid = Guid.NewGuid();
        protected Vector2 stageSize = new Vector2(512, 3776);
        protected int scrollRowHeight = 16;
        protected Vector2 gameScreenTilesSize = new Vector2(32, 30);
        protected Vector2 windowTilesSize = new Vector2(32, 28);
        protected int tileWidth = 16;
        protected ContentManager content;
        protected SpriteBatch spriteBatch;
        protected Vector2 deviceScreenSize;
        protected ScreenPad screenPad;
        protected SpriteFont font;
        public string Key = Guid.NewGuid().ToString();
        public Vector2 TopLeftCorner;
        public Vector2 StartPosition;
        public Vector2 Position;
        public Vector2 Size;
        static Dictionary<string, Texture2D> dicTexture = new Dictionary<string,Texture2D>();

        public PhysicalObject(ContentManager content, SpriteBatch spriteBatch, Vector2 deviceScreenSize, ScreenPad screenPad)
        {
            SetNewContent(content);
            this.spriteBatch = spriteBatch;
            this.deviceScreenSize = deviceScreenSize;
            this.screenPad = screenPad;
            this.TopLeftCorner = new Vector2((deviceScreenSize.X - gameScreenTilesSize.X * tileWidth) / 2, (deviceScreenSize.Y - gameScreenTilesSize.Y * tileWidth) / 2);
            font = font ?? ContentHelper.Instance.GetContent<SpriteFont>("Super-Contra-NES");
            LoadContent();
        }

        protected virtual void SetNewContent(ContentManager content)
        {
            this.content = content;
        }

        public override bool Equals(object obj)
        {
            return ((PhysicalObject)this).Guid.Equals(((PhysicalObject)obj).Guid);
        }

        public abstract void LoadContent();

        protected Texture2D GetTexture(string name)
        {
            if (dicTexture.ContainsKey(name))
                return dicTexture[name];
            else
            {
                var texture = ContentHelper.Instance.GetContent<Texture2D>(name);
                dicTexture.Add(name, texture);
                return texture;
            }                
        }

        public abstract void Update(GameTime gameTime, int tickCount, float scrollRows);

        public abstract void Draw(GameTime gameTime, int tickCount, float scrollRows);

        public virtual CollisionResult TestCollision(PhysicalObject that, Vector2 thatNewPosition, float scrollRows)
        {
            var collisionResult = new CollisionResult();

            var thisScreenRectangle =
                new Rectangle((int)Position.X
                    , (int)Position.Y
                    , (int)this.Size.X
                    , (int)this.Size.Y);

            var thatScreenRectangle =
                new Rectangle((int)thatNewPosition.X
                    , (int)(thatNewPosition.Y)
                    , (int)that.Size.X
                    , (int)that.Size.Y);

            Rectangle intersectArea = Rectangle.Intersect(thisScreenRectangle, thatScreenRectangle);
            if (intersectArea.X * intersectArea.Y != 0)
            {
                collisionResult.CollisionType = CollisionType.Blocked;
            }

            return collisionResult;
        }
    }
}
