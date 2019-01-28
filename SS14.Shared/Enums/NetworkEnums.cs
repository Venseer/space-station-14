﻿namespace SS14.Shared.Enums
{
    public enum PlacementManagerMessage
    {
        StartPlacement,
        CancelPlacement,
        PlacementFailed,
        RequestPlacement,
        RequestEntRemove,
    }

    public enum SessionStatus : byte
    {
        Zombie = 0,
        Connecting,
        Connected,
        InGame,
        Disconnected
    }

    public enum NetworkDataType
    {
        d_enum,
        d_bool,
        d_byte,
        d_sbyte,
        d_ushort,
        d_short,
        d_int,
        d_uint,
        d_ulong,
        d_long,
        d_float,
        d_double,
        d_string,
        d_byteArray
    }
}
