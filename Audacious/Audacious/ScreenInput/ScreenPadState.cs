using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace ScreenControlsSample
{
    public struct ScreenPadState
    {
        public ThumbSticks ThumbSticks;
        public Buttons Buttons;

        public ScreenPadState(ThumbSticks sticks, Buttons btns)
        {
            ThumbSticks = sticks;
            Buttons = btns;
        }
    }

    public struct Buttons
    {
        public ButtonState X;
        public ButtonState Y;
        public ButtonState A;
        public ButtonState B;

        public Buttons(ButtonState a, ButtonState b, ButtonState x, ButtonState y)
        {
            X = x;
            Y = y;
            A = a;
            B = b;
        }
    }

    public struct ThumbSticks
    {
        public Vector2 Left;
        public Vector2 Right;

        public ThumbSticks(Vector2 left, Vector2 right)
        {
            Left = left;
            Right = right;
        }
    }

    public enum ButtonState
    {
        Pressed = 1,
        Released = 0
    }
}
