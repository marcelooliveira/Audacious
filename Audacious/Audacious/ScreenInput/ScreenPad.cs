using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Audacious;
using xInput = Microsoft.Xna.Framework.Input;

namespace ScreenControlsSample
{
    public class ScreenPad
    {
        Vector2 leftStick = new Vector2(0, 0);
        Vector2 rightStick = new Vector2(0, 0);

        bool buttonAPressed = false;
        bool buttonBPressed = false;
        bool buttonXPressed = false;
        bool buttonDownPressed = false;
        bool buttonUpPressed = false;
        bool buttonLeftPressed = false;
        bool buttonRightPressed = false;

        #region Fields and Properties
        TouchCollection touches;

        Vector2 LeftThumbPosition;
        Vector2 RightThumbPosition;

        Vector2 maxLeft;
        Vector2 minLeft;

        Vector2 maxRight;
        Vector2 minRight;

        Vector3 touch3DPoint = Vector3.Zero;
        ContainmentType t = ContainmentType.Disjoint;

        /// <summary>
        /// The Value From the Left Stick. Vector2
        /// </summary>
        public Vector2 LeftStick
        {
            get
            {
                return leftStick;
                //Vector2 scaledVector = (LeftThumbPosition - LeftThumbOriginalPosition) / (padTexture.Width / 2);
                //scaledVector.Y *= -1;

                //if (scaledVector.Length() > 1f)
                //    scaledVector.Normalize();

                //return scaledVector;
            }
        }

        /// <summary>
        /// The Value From the Right Stick. Vector2
        /// </summary>
        public Vector2 RightStick
        {
            get
            {
                return rightStick;
                //Vector2 scaledVector = (RightThumbPosition - RightThumbOriginalPosition) / (padTexture.Width / 2);
                //scaledVector.Y *= -1;
                //if (scaledVector.Length() > 1f)
                //    scaledVector.Normalize();
                //return scaledVector;
            }
        }

        private Vector2 LeftThumbOriginalPosition;
        private Vector2 RightThumbOriginalPosition;

        private Vector2 LeftPadArea;
        private Vector2 LeftPadCenter;
        private Vector2 RightPadArea;
        private Vector2 RightPadCenter;

        private Vector2 fireButtonPosition;

        private Vector2 ButtonX;
        private Vector2 ButtonY;
        private Vector2 ButtonA;
        private Vector2 ButtonB;

        private float xOffset;
        private float yOffest;

        private Texture2D padTexture;
        private Texture2D thumbTexture;
        private Texture2D fireButton;

        private Texture2D abxyTexture;

        private Color LeftThumbColor;
        private Color RightThumbColor;
        private Color FireButtonColor;

        int leftId = -1;
        int rightId = -1;
        int fireId = -1;

        int xId = -1;
        int yId = -1;
        int aId = -1;
        int bId = -1;

        Color xColor;
        Color yColor;
        Color aColor;
        Color bColor;

        private BoundingSphere fireButtonCollision;

        private BoundingSphere leftStickCollision;
        private BoundingSphere rightStickCollision;

        private BoundingSphere xButtonCollision;
        private BoundingSphere yButtonCollision;
        private BoundingSphere aButtonCollision;
        private BoundingSphere bButtonCollision;

        TouchLocation? leftTouch = null;
        TouchLocation? rightTouch = null;
        TouchLocation? fireTouch = null;

        TouchLocation? xTouch = null;
        TouchLocation? yTouch = null;
        TouchLocation? aTouch = null;
        TouchLocation? bTouch = null;

        float maxDistance;
        //TouchPanel screen;

        // weapon type for proper HUD selection
        /// <summary>
        /// Weapon Type for Proper HUD Selection
        /// </summary>
        public WeaponType CurrentWeapon
        {
            get { return currentWeapon; }
            set { currentWeapon = value; }
        }

        private WeaponType currentWeapon = WeaponType.Range;

        public enum WeaponType
        {
            Meelee,
            Range,
            RangeAutoAim,
        }

        public bool FireButtonPressed { get; set; }

        ButtonState xPressed, yPressed, aPressed, bPressed = ButtonState.Released; //, mPressed
        BaseGame game;

        public bool XTYPE;

        #endregion

        public ScreenPad(BaseGame game, Texture2D pad, Texture2D thumb, Texture2D fire,
            Color leftColor, Color rightColor, Color buttonColor)
        {
            this.game = game;

            padTexture = pad;
            thumbTexture = thumb;

            maxDistance = (padTexture.Width - thumbTexture.Width);

            fireButton = fire;

            // colors for sides 
            LeftThumbColor = leftColor;
            RightThumbColor = rightColor;
            FireButtonColor = buttonColor;
            // auto placement according to screen rotation
            PlaceControls();
        }

        /// <summary>
        /// Creates X360 type virtual Controler
        /// </summary>
        /// <param name="game">instance of game</param>
        /// <param name="pad">Texture for thumbstick base</param>
        /// <param name="thumb">Texture for thumbstick</param>
        /// <param name="abxyButtons">Texture for A, B, X, Y buttons. With size of 256x64 px with ABXY order.</param>
		public ScreenPad(BaseGame game, Texture2D pad, Texture2D thumb, Texture2D abxyButtons)
        {
            this.game = game;

            padTexture = pad;
            thumbTexture = thumb;
            abxyTexture = abxyButtons;

            aColor = xColor = yColor = bColor = Color.LightGray;

            maxDistance = (padTexture.Width - thumbTexture.Width);
            XTYPE = true;
            LeftThumbColor = Color.White;
            PlaceControls(true);
        }

        private void PlaceControls(bool xType)
        {
            #region LeftStick
            xOffset = /*game.GraphicsDevice.Viewport.X +*/ 30;
            yOffest = BaseGame.ScreenSize.Y - padTexture.Height - 30;

            LeftPadArea = new Vector2(xOffset, yOffest);

            LeftPadCenter = new Vector2(
                LeftPadArea.X + padTexture.Width / 2,
                LeftPadArea.Y + padTexture.Height / 2);

            //leftStickCollision = new BoundingSphere(new Vector3(LeftPadCenter, 0), padTexture.Width / 2);
            leftStickCollision = new BoundingSphere(new Vector3(new Vector2(110, 470), 0), padTexture.Width / 2);

            LeftThumbOriginalPosition = new Vector2(
                LeftPadArea.X + (padTexture.Width - thumbTexture.Width) / 2,
                LeftPadArea.Y + (padTexture.Height - thumbTexture.Height) / 2);

            minLeft = new Vector2(
                LeftPadCenter.X - padTexture.Width / 2 - thumbTexture.Width / 2,
                LeftPadCenter.Y - padTexture.Height / 2 - thumbTexture.Height / 2);

            maxLeft = new Vector2(
                LeftPadCenter.X + padTexture.Width / 2 - thumbTexture.Width / 2,
                LeftPadCenter.Y + padTexture.Height / 2 - thumbTexture.Height / 2);

            LeftThumbPosition = LeftThumbOriginalPosition;
            #endregion
            #region Right Stick
            //xOffset = game.GraphicsDevice.Viewport.Width - (padTexture.Width - 30) * 2f;
            //yOffest = game.GraphicsDevice.Viewport.Height - (padTexture.Height - 30) * 2f;

            xOffset = game.GraphicsDevice.Viewport.Width - 190;
            yOffest = game.GraphicsDevice.Viewport.Height - 190;

            // we already have the yOffset properly setup so use it from a few lines above

            RightPadArea = new Vector2(xOffset, yOffest);

            RightPadCenter = new Vector2(
                RightPadArea.X + 32,
                RightPadArea.Y + 32);

            rightStickCollision = new BoundingSphere(new Vector3(RightPadCenter, 0), padTexture.Width / 2);

            RightThumbOriginalPosition = new Vector2(
                RightPadArea.X + (padTexture.Width - thumbTexture.Width) / 2,
                RightPadArea.Y + (padTexture.Height - thumbTexture.Height) / 2);

            RightThumbPosition = RightThumbOriginalPosition;

            minRight = new Vector2(
                RightPadCenter.X - padTexture.Width / 2 - thumbTexture.Width / 2,
                RightPadCenter.Y - padTexture.Height / 2 - thumbTexture.Height / 2);

            maxRight = new Vector2(
                RightPadCenter.X + padTexture.Width / 2 - thumbTexture.Width / 2,
                RightPadCenter.Y + padTexture.Height / 2 - thumbTexture.Height / 2);
            #endregion
            #region Buttons
            //ButtonX = new Vector2(RightPadCenter.X - 52f, RightPadCenter.Y + 42f);
            //xButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonX.X + 42f, ButtonX.Y + 42f), 0), 42);

            //ButtonY = new Vector2(RightPadCenter.X + 48f, RightPadCenter.Y + 42f);
            //yButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonY.X + 42f, ButtonY.Y + 42f), 0), 42f);

            //ButtonA = new Vector2(RightPadCenter.X + 48f, RightPadCenter.Y - 58f);
            //aButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonA.X + 42f, ButtonA.Y + 42), 0), 42);

            //ButtonB = new Vector2(RightPadCenter.X + 64f, /*RightPadCenter.Y +*/ 32f);
            //bButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonB.X + 30f, ButtonB.Y + 30f), 0), 30f);

            ButtonX = new Vector2(650f, 392f);
            //xButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonX.X + 42f, ButtonX.Y + 42f), 0), 42f);
            xButtonCollision = new BoundingSphere(new Vector3(new Vector2(1150, 650), 0), 42f);

            ButtonY = new Vector2(650f, 272f);
            yButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonY.X + 42f, ButtonY.Y + 42f), 0), 42f);

            ButtonA = new Vector2(650f, 152f);
            aButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonA.X + 42f, ButtonA.Y + 42), 0), 42);

            ButtonB = new Vector2(/*RightPadCenter.X + 64f*/ 650f, /*RightPadCenter.Y +*/ 32f);
            bButtonCollision = new BoundingSphere(new Vector3(new Vector2(ButtonB.X + 30f, ButtonB.Y + 30f), 0), 30f);
            #endregion
        }

        private void PlaceControls()
        {
            // left pad, it's always present and gives the direction for movement
            xOffset = game.GraphicsDevice.Viewport.X + 30;
            yOffest = game.GraphicsDevice.Viewport.Height - padTexture.Height - 30;

            LeftPadArea = new Vector2(xOffset, yOffest);

            LeftPadCenter = new Vector2(
                LeftPadArea.X + padTexture.Width / 2,
                LeftPadArea.Y + padTexture.Height / 2);

            leftStickCollision = new BoundingSphere(new Vector3(LeftPadCenter, 0), padTexture.Width / 2);

            LeftThumbOriginalPosition = new Vector2(
                LeftPadArea.X + (padTexture.Width - thumbTexture.Width) / 2,
                LeftPadArea.Y + (padTexture.Height - thumbTexture.Height) / 2);

            minLeft = new Vector2(
                LeftPadCenter.X - padTexture.Width / 2 - thumbTexture.Width / 2,
                LeftPadCenter.Y - padTexture.Height / 2 - thumbTexture.Height / 2);

            maxLeft = new Vector2(
                LeftPadCenter.X + padTexture.Width / 2 - thumbTexture.Width / 2,
                LeftPadCenter.Y + padTexture.Height / 2 - thumbTexture.Height / 2);

            LeftThumbPosition = LeftThumbOriginalPosition;

            // right thumb (when we have range weapons in arms)
            xOffset = game.GraphicsDevice.Viewport.Width - padTexture.Width - 30;

            // we already have the yOffset properly setup so use it from a few lines above

            RightPadArea = new Vector2(xOffset, yOffest);

            RightPadCenter = new Vector2(
                RightPadArea.X + padTexture.Width / 2,
                RightPadArea.Y + padTexture.Height / 2);

            rightStickCollision = new BoundingSphere(new Vector3(RightPadCenter, 0), padTexture.Width / 2);

            RightThumbOriginalPosition = new Vector2(
                RightPadArea.X + (padTexture.Width - thumbTexture.Width) / 2,
                RightPadArea.Y + (padTexture.Height - thumbTexture.Height) / 2);

            RightThumbPosition = RightThumbOriginalPosition;

            minRight = new Vector2(
                RightPadCenter.X - padTexture.Width / 2 - thumbTexture.Width / 2,
                RightPadCenter.Y - padTexture.Height / 2 - thumbTexture.Height / 2);

            maxRight = new Vector2(
                RightPadCenter.X + padTexture.Width / 2 - thumbTexture.Width / 2,
                RightPadCenter.Y + padTexture.Height / 2 - thumbTexture.Height / 2);

            // and finally the fire button when we have meelee or auto aim arms
            xOffset = game.GraphicsDevice.Viewport.Width - fireButton.Width - 30;
            yOffest = game.GraphicsDevice.Viewport.Height - fireButton.Height - 30;

            fireButtonPosition = new Vector2(
                RightPadCenter.X - fireButton.Width / 2,
                RightPadCenter.Y - fireButton.Height / 2); //new Vector2(xOffset, yOffest);

            fireButtonCollision = new BoundingSphere(new Vector3(RightPadCenter, 0), fireButton.Width / 2);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Vector2 currentLeftPosition = new Vector2(
                LeftThumbOriginalPosition.X + LeftStick.X * maxDistance,
                LeftThumbOriginalPosition.Y + LeftStick.Y * maxDistance * -1);

            // left thumb we always draw 
            spriteBatch.Draw(padTexture, LeftPadArea, leftTouch.HasValue ? LeftThumbColor : Color.White);
            spriteBatch.Draw(thumbTexture, currentLeftPosition, leftTouch.HasValue ? LeftThumbColor : Color.White);

            #region X360 Typed Controls
            if (XTYPE)
            {
                //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                spriteBatch.Draw(abxyTexture, ButtonA, new Rectangle(0, 0, 84, 84), (aPressed == ButtonState.Pressed ? Color.Crimson : Color.White)); //aColor

                // this is a special button to open menu
                spriteBatch.Draw(abxyTexture, ButtonB, new Rectangle(84, 0, 84, 84), (bPressed == ButtonState.Pressed ? Color.Crimson : Color.White), 0f, Vector2.Zero, 0.839f, SpriteEffects.None, 0f);

                spriteBatch.Draw(abxyTexture, ButtonX, new Rectangle(168, 0, 84, 84), (xPressed == ButtonState.Pressed ? Color.Crimson : Color.White));

                spriteBatch.Draw(abxyTexture, ButtonY, new Rectangle(252, 0, 84, 84), (yPressed == ButtonState.Pressed ? Color.Crimson : Color.White));
            }
            #endregion

            #region This is not X360 type controls
            else
            {
                switch (currentWeapon)
                {
                    case WeaponType.Meelee:
                        spriteBatch.Draw(fireButton, fireButtonPosition, fireTouch.HasValue ? FireButtonColor : Color.White);
                        break;

                    case WeaponType.Range:
                        Vector2 currentRightPosition = new Vector2(
                            RightThumbOriginalPosition.X + RightStick.X * maxDistance,
                            RightThumbOriginalPosition.Y + RightStick.Y * maxDistance * -1);

                        spriteBatch.Draw(padTexture, RightPadArea, rightTouch.HasValue ? RightThumbColor : Color.White);
                        spriteBatch.Draw(thumbTexture, currentRightPosition, rightTouch.HasValue ? RightThumbColor : Color.White);

                        break;

                    case WeaponType.RangeAutoAim:
                        spriteBatch.Draw(fireButton, fireButtonPosition, fireTouch.HasValue ? FireButtonColor : Color.White);

                        break;
                }
            }
            #endregion
        }

        public void Update()
        {
            var keyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            if (keyboardState.IsKeyDown(xInput.Keys.A))
            {
                buttonAPressed = true;
                aPressed = ButtonState.Pressed;
            }
            else if (keyboardState.IsKeyUp(xInput.Keys.A) && buttonAPressed)
            {
                buttonAPressed = false;
                aPressed = ButtonState.Released;
            }

            if (keyboardState.IsKeyDown(xInput.Keys.B))
            {
                buttonBPressed = true;
                bPressed = ButtonState.Pressed;
            }
            else if (keyboardState.IsKeyUp(xInput.Keys.B) && buttonBPressed)
            {
                buttonBPressed = false;
                bPressed = ButtonState.Released;
            }

            if (keyboardState.IsKeyDown(xInput.Keys.Space))
            {
                buttonXPressed = true;
                xPressed = ButtonState.Pressed;

            }
            else if (keyboardState.IsKeyUp(xInput.Keys.Space) && buttonXPressed)
            {
                buttonXPressed = false;
                xPressed = ButtonState.Released;
            }

            if (keyboardState.IsKeyDown(xInput.Keys.Up))
            {
                buttonUpPressed = true;
                leftStick.Y = 1;
            }
            else if (keyboardState.IsKeyUp(xInput.Keys.Up) && buttonUpPressed)
            {
                buttonUpPressed = false;
                leftStick.Y = 0;
            }

            if (keyboardState.IsKeyDown(xInput.Keys.Down))
            {
                buttonDownPressed = true;
                leftStick.Y = -1;
            }
            else if (keyboardState.IsKeyUp(xInput.Keys.Down) && buttonDownPressed)
            {
                buttonDownPressed = false;
                leftStick.Y = 0;
            }

            if (keyboardState.IsKeyDown(xInput.Keys.Left))
            {
                buttonLeftPressed = true;
                leftStick.X = -1;
            }
            else if (keyboardState.IsKeyUp(xInput.Keys.Left) && buttonLeftPressed)
            {
                buttonLeftPressed = false;
                leftStick.X = 0;
            }

            if (keyboardState.IsKeyDown(xInput.Keys.Right))
            {
                buttonRightPressed = true;
                leftStick.X = 1;
            }
            else if (keyboardState.IsKeyUp(xInput.Keys.Right) && buttonRightPressed)
            {
                buttonRightPressed = false;
                leftStick.X = 0;
            }


#if WINDOWS_PHONE
            leftTouch = null;
            rightTouch = null;
            fireTouch = null;

            xTouch = null;
            yTouch = null;
            aTouch = null;
            bTouch = null;

            touches = TouchPanel.GetState();

            //foreach (var t in touches)
            for (int i = 0; i < touches.Count; i++)
            {
                TouchLocation t = touches[i];

                float x = t.Position.X;
                float y = t.Position.Y;

            #region Left stick is always updated
                if (t.Id == leftId)
                {
                    leftTouch = t;
                    continue;
                }

                if (leftId == -1)
                {
                    if (IsTouchingLeftStick(ref x, ref y))
                    {
                        leftTouch = t;
                        continue;
                    }
                }
            #endregion

            #region XTYPE OR NOT

                // XTYPE means that we have only ono Left Thumb Stick and four buttons A, B, X and Y
                if (XTYPE)
                {
            #region X button
                    if (t.Id == xId)
                    {
                        xTouch = t;
                        continue;
                    }
                    if (xId == -1)
                    {
                        if (IsTouchingButton(ref x, ref y, ref xButtonCollision))
                        {
                            xTouch = t;
                            continue;
                        }
                    }
            #endregion

            #region Y button
                    if (t.Id == yId)
                    {
                        yTouch = t;
                        continue;
                    }
                    if (yId == -1)
                    {
                        if (IsTouchingButton(ref x, ref y, ref yButtonCollision))
                        {
                            yTouch = t;
                            continue;
                        }
                    }
            #endregion

            #region A button
                    if (t.Id == aId)
                    {
                        aTouch = t;
                        continue;
                    }
                    if (aId == -1)
                    {
                        if (IsTouchingButton(ref x, ref y, ref aButtonCollision))
                        {
                            aTouch = t;
                            continue;
                        }
                    }
            #endregion

            #region B button
                    if (t.Id == bId)
                    {
                        bTouch = t;continue;
                    }
                    if (bId == -1)
                    {
                        if (IsTouchingButton(ref x, ref y, ref bButtonCollision))
                        {
                            bTouch = t;
                            continue;
                        }
                    }
            #endregion
                }
                else
                {
                    if (CurrentWeapon == WeaponType.Range)
                    {
                        if (t.Id == rightId)
                        {
                            rightTouch = t;
                            continue;
                        }
                        if (rightId == -1)
                        {
                            if (IsTouchingRightStick(t.Position))
                            {
                                rightTouch = t;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (t.Id == fireId)
                        {
                            fireTouch = t;
                            continue;
                        }
                        if (fireId == -1)
                        {
                            if (IsTouchingButton(ref x, ref y, ref fireButtonCollision))
                            {
                                fireTouch = t;
                                continue;
                            }
                        }
                    }
                }
            #endregion
            }

            #region Left Stick
            if (leftTouch.HasValue)
            {
                LeftThumbPosition = new Vector2(
                    leftTouch.Value.Position.X - thumbTexture.Width / 2,
                    leftTouch.Value.Position.Y - thumbTexture.Height / 2);
                
                LeftThumbPosition = Vector2.Clamp(LeftThumbPosition, minLeft, maxLeft);
                leftId = leftTouch.Value.Id;
            }
            else
            {
                leftId = -1;
                LeftThumbPosition += (LeftThumbOriginalPosition - LeftThumbPosition) * 0.9f;
            }
            #endregion

            #region Right Stick
            if (rightTouch.HasValue)
            {
                RightThumbPosition = new Vector2(
                    rightTouch.Value.Position.X - thumbTexture.Width / 2,
                    rightTouch.Value.Position.Y - thumbTexture.Height / 2);

                RightThumbPosition = Vector2.Clamp(RightThumbPosition, minRight, maxRight);
                rightId = rightTouch.Value.Id;
            }
            else
            {
                rightId = -1;
                RightThumbPosition += (RightThumbOriginalPosition - RightThumbPosition) * 0.9f;
            }
            #endregion

            #region fireTouch
            if (fireTouch.HasValue)
            {
                FireButtonPressed = true;
            }
            else
            {
                fireId = -1;
                FireButtonPressed = false;
            }
            #endregion

            #region x Touch
            if (xTouch.HasValue)
            {
                xPressed = ButtonState.Pressed;
                xColor = Color.White;
            }
            else
            {
                xId = -1;
                xPressed = ButtonState.Released;
                xColor = Color.LightGray;
            }
            #endregion

            #region y Touch
            if (yTouch.HasValue)
            {
                yPressed = ButtonState.Pressed;
                yColor = Color.White;
            }
            else
            {
                yId = -1;
                yPressed = ButtonState.Released;
                yColor = Color.LightGray;
            }
            #endregion

            #region a Touch
            if (aTouch.HasValue)
            {
                aPressed = ButtonState.Pressed;
                aColor = Color.White;
            }
            else
            {
                aId = -1;
                aPressed = ButtonState.Released;
                aColor = Color.LightGray;
            }
            #endregion

            #region b Touch
            if (bTouch.HasValue)
            {
                bPressed = ButtonState.Pressed;
                bColor = Color.White;
            }
            else
            {
                bId = -1;
                bPressed = ButtonState.Released;
                bColor = Color.LightGray;
            }
            #endregion
#endif

        }

        private bool IsTouchingLeftStick(ref float x, ref float y)
        {
            Vector3 point = new Vector3(x, y, 0);
            leftStickCollision.Contains(ref point, out t);
            return (t == ContainmentType.Contains);
        }

        private bool IsTouchingRightStick(Vector2 point)
        {
            ContainmentType t = rightStickCollision.Contains(new Vector3(point.X, point.Y, 0));
            return (t == ContainmentType.Contains);
        }

        private bool IsTouchingFireButtom(ref Vector3 point)
        {
            fireButtonCollision.Contains(ref point, out t);
            return (t == ContainmentType.Contains);
        }

        private bool IsTouchingButton(ref float x, ref float y, ref BoundingSphere buttonBounds)
        {
            Vector3 point = new Vector3(x, y, 0);
            buttonBounds.Contains(ref point, out t);
            return (t == ContainmentType.Contains);
        }

        /// <summary>
        /// Returns the ScreenPad state
        /// State consists of Thumbsticks and ABXY buttons
        /// More buttons will be added later
        /// </summary>
        /// <returns>ScreenPadState</returns>
        public ScreenPadState GetState()
        {
            return new ScreenPadState
            (
                new ThumbSticks
                (
                    LeftStick,
                    RightStick
                ),
                new Buttons
                (
                    aPressed,
                    bPressed,
                    xPressed,
                    yPressed
                )
            );
        }
    }
}
