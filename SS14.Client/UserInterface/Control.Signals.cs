﻿using SS14.Client.GodotGlue;
using SS14.Client.Input;
using SS14.Shared.Log;

namespace SS14.Client.UserInterface
{
    // Signal registration is a lot of annoying, hacky and ugly boiler plate
    // so we put it in here!
    public partial class Control
    {
        private GodotSignalSubscriber0 __mouseEnteredSubscriber;
        private GodotSignalSubscriber0 __mouseExitedSubscriber;
        private GodotSignalSubscriber1 __guiInputSubscriber;
        private GodotSignalSubscriber0 __focusEnteredSubscriber;
        private GodotSignalSubscriber0 __focusExitedSubscriber;
        private GodotSignalSubscriber0 __treeExitedSubscriber;
        private GodotSignalSubscriber0 __resizedSubscriber;

        protected virtual void SetupSignalHooks()
        {
            __mouseEnteredSubscriber = new GodotSignalSubscriber0();
            __mouseEnteredSubscriber.Connect(SceneControl, "mouse_entered");
            __mouseEnteredSubscriber.Signal += __mouseEnteredHook;

            __mouseExitedSubscriber = new GodotSignalSubscriber0();
            __mouseExitedSubscriber.Connect(SceneControl, "mouse_exited");
            __mouseExitedSubscriber.Signal += __mouseExitedHook;

            __guiInputSubscriber = new GodotSignalSubscriber1();
            __guiInputSubscriber.Connect(SceneControl, "gui_input");
            __guiInputSubscriber.Signal += __guiInputHook;

            __focusEnteredSubscriber = new GodotSignalSubscriber0();
            __focusEnteredSubscriber.Connect(SceneControl, "focus_entered");
            __focusEnteredSubscriber.Signal += __focusEnteredHook;

            __focusExitedSubscriber = new GodotSignalSubscriber0();
            __focusExitedSubscriber.Connect(SceneControl, "focus_exited");
            __focusExitedSubscriber.Signal += __focusExitedHook;

            __treeExitedSubscriber = new GodotSignalSubscriber0();
            __treeExitedSubscriber.Connect(SceneControl, "tree_exited");
            __treeExitedSubscriber.Signal += __treeExitedHook;

            __resizedSubscriber = new GodotSignalSubscriber0();
            __resizedSubscriber.Connect(SceneControl, "resized");
            __resizedSubscriber.Signal += __resizedHook;
        }

        protected virtual void DisposeSignalHooks()
        {
            if (__mouseEnteredSubscriber != null)
            {
                __mouseEnteredSubscriber.Disconnect(SceneControl, "mouse_entered");
                __mouseEnteredSubscriber.Dispose();
                __mouseEnteredSubscriber = null;
            }

            if (__mouseExitedSubscriber != null)
            {
                __mouseExitedSubscriber.Disconnect(SceneControl, "mouse_exited");
                __mouseExitedSubscriber.Dispose();
                __mouseExitedSubscriber = null;
            }

            if (__guiInputSubscriber != null)
            {
                __guiInputSubscriber.Disconnect(SceneControl, "gui_input");
                __guiInputSubscriber.Dispose();
                __guiInputSubscriber = null;
            }

            if (__focusEnteredSubscriber != null)
            {
                __focusEnteredSubscriber.Disconnect(SceneControl, "focus_entered");
                __focusEnteredSubscriber.Dispose();
                __focusEnteredSubscriber = null;
            }

            if (__focusExitedSubscriber != null)
            {
                __focusExitedSubscriber.Disconnect(SceneControl, "focus_exited");
                __focusExitedSubscriber.Dispose();
                __focusExitedSubscriber = null;
            }

            if (__treeExitedSubscriber != null)
            {
                __treeExitedSubscriber.Disconnect(SceneControl, "tree_exited");
                __treeExitedSubscriber.Dispose();
                __treeExitedSubscriber = null;
            }

            if (__resizedSubscriber != null)
            {
                __resizedSubscriber.Disconnect(SceneControl, "resized");
                __resizedSubscriber.Dispose();
                __resizedSubscriber = null;
            }
        }

        private void __mouseEnteredHook()
        {
            MouseEntered();
        }

        private void __mouseExitedHook()
        {
            MouseExited();
        }

        private void __guiInputHook(object ev)
        {
            HandleGuiInput((Godot.InputEvent) ev);
        }

        private void __focusEnteredHook()
        {
            FocusEntered();
        }

        private void __focusExitedHook()
        {
            FocusExited();
        }

        private void __treeExitedHook()
        {
            // Eh maybe make a separate event later.
            FocusExited();
        }

        private void __resizedHook()
        {
            Resized();
        }
    }
}
