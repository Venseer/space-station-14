using SS14.Shared.Maths;

namespace SS14.Client.Utility
{
    internal static class OpenTKConversions
    {
        public static OpenTK.Matrix3 ConvertOpenTK(this Matrix3 matrix)
        {
            return new OpenTK.Matrix3
            {
                M11 = matrix.R0C0,
                M12 = matrix.R0C1,
                M13 = matrix.R0C2,
                M21 = matrix.R1C0,
                M22 = matrix.R1C1,
                M23 = matrix.R1C2,
                M31 = matrix.R2C0,
                M32 = matrix.R2C1,
                M33 = matrix.R2C2
            };
        }

        public static OpenTK.Graphics.Color4 ConvertOpenTK(this Color color)
        {
            return new OpenTK.Graphics.Color4(color.R, color.G, color.B, color.A);
        }
    }
}
