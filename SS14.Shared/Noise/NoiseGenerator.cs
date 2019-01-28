using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using SS14.Shared.Maths;

namespace SS14.Shared.Noise
{
    [PublicAPI]
    public sealed class NoiseGenerator : IDisposable
    {
        [PublicAPI]
        public enum NoiseType
        {
            Fbm = 0,
            Ridged = 1
        }

        private IntPtr _nativeGenerator;

        public NoiseGenerator(NoiseType type)
        {
            _nativeGenerator = _GeneratorMake((byte) type);
        }

        public bool Disposed => _nativeGenerator == IntPtr.Zero;

        public void Dispose()
        {
            if (Disposed) return;

            _GeneratorDispose(_nativeGenerator);
            _nativeGenerator = IntPtr.Zero;
        }

        public void SetFrequency(double frequency)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));
            _GeneratorSetFrequency(_nativeGenerator, frequency);
        }

        public void SetLacunarity(double lacunarity)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));
            _GeneratorSetLacunarity(_nativeGenerator, lacunarity);
        }

        public void SetPersistence(double persistence)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));
            _GeneratorSetPersistence(_nativeGenerator, persistence);
        }

        public void SetPeriodX(double periodX)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));
            _GeneratorSetPeriodX(_nativeGenerator, periodX);
        }

        public void SetPeriodY(double periodY)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));
            _GeneratorSetPeriodY(_nativeGenerator, periodY);
        }

        public void SetOctaves(uint octaves)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));
            if (octaves > 32)
                throw new ArgumentOutOfRangeException(nameof(octaves), octaves,
                    "Octave count cannot be greater than 32.");
            _GeneratorSetOctaves(_nativeGenerator, octaves);
        }

        public void SetSeed(uint seed)
        {
            _GeneratorSetSeed(_nativeGenerator, seed);
        }

        public double GetNoiseTiled(float x, float y)
        {
            return GetNoiseTiled(new Vector2(x, y));
        }

        public double GetNoiseTiled(Vector2d vec)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));

            return _GetNoiseTiled2D(_nativeGenerator, vec);
        }

        public double GetNoise(float x, float y)
        {
            return GetNoise(new Vector2(x, y));
        }

        public double GetNoise(Vector2d vector)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));

            return _GetNoise2D(_nativeGenerator, vector);
        }

        public double GetNoise(float x, float y, float z)
        {
            return GetNoise(new Vector3(x, y, z));
        }

        public double GetNoise(Vector3d vector)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));

            return _GetNoise3D(_nativeGenerator, vector);
        }

        public double GetNoise(float x, float y, float z, float w)
        {
            return GetNoise(new Vector4(x, y, z, w));
        }

        public double GetNoise(Vector4d vector)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(NoiseGenerator));

            return _GetNoise4D(_nativeGenerator, vector);
        }

        ~NoiseGenerator()
        {
            Dispose();
        }

        #region FFI

        [DllImport("ss14_noise.dll", EntryPoint = "generator_new", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _GeneratorMake(byte noiseType);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_octaves", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorSetOctaves(IntPtr gen, uint octaves);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_persistence",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorSetPersistence(IntPtr gen, double persistence);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_lacunarity",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorSetLacunarity(IntPtr gen, double lacunarity);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_period_x",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorSetPeriodX(IntPtr gen, double periodX);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_period_y",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorSetPeriodY(IntPtr gen, double periodY);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_frequency",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorSetFrequency(IntPtr gen, double frequency);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_seed", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorSetSeed(IntPtr gen, uint seed);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_dispose", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _GeneratorDispose(IntPtr gen);

        [DllImport("ss14_noise.dll", EntryPoint = "get_noise_2d", CallingConvention = CallingConvention.Cdecl)]
        private static extern double _GetNoise2D(IntPtr gen, Vector2d pos);

        [DllImport("ss14_noise.dll", EntryPoint = "get_noise_3d", CallingConvention = CallingConvention.Cdecl)]
        private static extern double _GetNoise3D(IntPtr gen, Vector3d pos);

        [DllImport("ss14_noise.dll", EntryPoint = "get_noise_4d", CallingConvention = CallingConvention.Cdecl)]
        private static extern double _GetNoise4D(IntPtr gen, Vector4d pos);

        [DllImport("ss14_noise.dll", EntryPoint = "get_noise_tiled_2d", CallingConvention = CallingConvention.Cdecl)]
        private static extern double _GetNoiseTiled2D(IntPtr gen, Vector2d pos);

        #endregion
    }
}
