﻿using Godot;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Log;

namespace SS14.Client.Log
{
    /// <summary>
    ///     Handles logs using Godot's <see cref="GD.Print(object[])"/>.
    /// </summary>
    class GodotLogHandler : ILogHandler
    {
        public void Log(LogMessage message)
        {
            var name = message.LogLevelToName();
            var msg = $"[{name}] {message.SawmillName}: {message.Message}";
            GD.Print(msg);
        }
    }
}
