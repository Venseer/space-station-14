﻿using Godot;
using SS14.Client.Input;

namespace SS14.Client
{
    public sealed partial class GameController
    {
        /// <summary>
        ///     Invoked when a key on the keyboard is pressed down.
        /// </summary>
        private void KeyDown(KeyEventArgs keyEvent)
        {
            inputManager.KeyDown(keyEvent);
        }

        /// <summary>
        ///     Invoked when a key on the keyboard is released.
        /// </summary>
        private void KeyUp(KeyEventArgs keyEvent)
        {
            inputManager.KeyUp(keyEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is pressed down.
        /// </summary>
        private void MouseDown(MouseButtonEventArgs mouseEvent)
        {
            _userInterfaceManager.UnhandledMouseDown(mouseEvent);
            _stateManager.MouseDown(mouseEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is released.
        /// </summary>
        private void MouseUp(MouseButtonEventArgs mouseButtonEventArgs)
        {
            _userInterfaceManager.UnhandledMouseUp(mouseButtonEventArgs);
            _stateManager.MouseUp(mouseButtonEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse is moved inside the game window.
        /// </summary>
        private void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            _stateManager.MouseMove(mouseMoveEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse wheel is moved.
        /// </summary>
        private void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
            _stateManager.MouseWheelMove(mouseWheelEventArgs);
        }

        // Override that converts and distributes the input events
        //   to the more sane methods above.
        public override void Input(InputEvent inputEvent)
        {
            switch (inputEvent)
            {
                case InputEventKey keyEvent:
                    var keyEventArgs = (KeyEventArgs)keyEvent;
                    if (keyEvent.Echo)
                    {
                        return;
                    }
                    else if (keyEvent.Pressed)
                    {
                        KeyDown(keyEventArgs);
                    }
                    else
                    {
                        KeyUp(keyEventArgs);
                    }
                    break;

                case InputEventMouseButton mouseButtonEvent:
                    if (mouseButtonEvent.ButtonIndex >= (int)ButtonList.WheelUp && mouseButtonEvent.ButtonIndex <= (int)ButtonList.WheelRight)
                    {
                        // Mouse wheel event.
                        var mouseWheelEventArgs = (MouseWheelEventArgs)mouseButtonEvent;
                        MouseWheel(mouseWheelEventArgs);
                    }
                    else
                    {
                        // Mouse button event.
                        var mouseButtonEventArgs = (MouseButtonEventArgs)mouseButtonEvent;
                        if (mouseButtonEvent.Pressed)
                        {
                            MouseDown(mouseButtonEventArgs);
                        }
                        else
                        {
                            MouseUp(mouseButtonEventArgs);
                        }
                    }
                    break;

                case InputEventMouseMotion mouseMotionEvent:
                    var mouseMoveEventArgs = (MouseMoveEventArgs)mouseMotionEvent;
                    MouseMove(mouseMoveEventArgs);
                    break;
            }
        }

        public override void PreInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventKey keyEvent)
            {
                var keyEventArgs = (KeyEventArgs)keyEvent;
                if (keyEvent.Echo)
                {
                    return;
                }
                else if (keyEvent.Pressed)
                {
                    // TODO: these hacks are in right now for toggling the debug console.
                    // Somehow find a way to make the console use the key binds system?
                    _userInterfaceManager.PreKeyDown(keyEventArgs);
                }
                else
                {
                    _userInterfaceManager.PreKeyUp(keyEventArgs);
                }
            }
        }
    }
}
