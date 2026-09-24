using System;
using System.Security.Cryptography;

namespace SlotGame.Core
{
    /// <summary>
    /// Cryptographically secure Random Number Generator implementation.
    /// Uses system-level entropy for casino-grade unpredictability and compliance.
    /// </summary>
    public class CryptographicRNG : IRandomNumberGenerator, IDisposable
    {
        private readonly RandomNumberGenerator _rng;
        private readonly byte[] _uintBuffer = new byte[4];

        public CryptographicRNG()
        {
            _rng = RandomNumberGenerator.Create();
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentException("minInclusive must be strictly less than maxExclusive");
            }

            long range = (long)maxExclusive - minInclusive;
            long maxValid = (1L << 32) - ((1L << 32) % range);

            uint randValue;
            do
            {
                _rng.GetBytes(_uintBuffer);
                randValue = BitConverter.ToUInt32(_uintBuffer, 0);
            } while (randValue >= maxValid); // Rejection sampling to prevent modulo bias

            return (int)(minInclusive + (randValue % range));
        }

        public float NextFloat()
        {
            _rng.GetBytes(_uintBuffer);
            uint randValue = BitConverter.ToUInt32(_uintBuffer, 0);
            return (float)(randValue / (double)uint.MaxValue);
        }

        public int NextWeightedIndex(int[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                throw new ArgumentException("Weights array cannot be null or empty.");
            }

            int totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] < 0)
                {
                    throw new ArgumentException("Weights must be non-negative.");
                }
                totalWeight += weights[i];
            }

            if (totalWeight <= 0)
            {
                throw new InvalidOperationException("Total weight must be positive.");
            }

            int roll = NextInt(0, totalWeight);
            int runningSum = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                runningSum += weights[i];
                if (roll < runningSum)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        public void Dispose()
        {
            _rng?.Dispose();
        }
    }
}
