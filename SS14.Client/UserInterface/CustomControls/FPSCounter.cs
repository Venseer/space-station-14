﻿using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;
using SS14.Shared.Reflection;

namespace SS14.Client.UserInterface.CustomControls
{
    [Reflect(false)]
    public class FPSCounter : Label
    {
        protected override void Initialize()
        {
            base.Initialize();

            AddColorOverride("font_color_shadow", Color.Black);
            AddConstantOverride("shadow_offset_x", 1);
            AddConstantOverride("shadow_offset_y", 1);

            MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Update(ProcessFrameEventArgs args)
        {
            if (!Visible)
            {
                return;
            }
            var fps = Godot.Performance.GetMonitor(Godot.Performance.Monitor.TimeFps);
            Text = $"FPS: {fps}";
        }
    }
}
