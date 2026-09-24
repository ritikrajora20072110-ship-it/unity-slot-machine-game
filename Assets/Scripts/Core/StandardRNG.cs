using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Fast pseudo-random number generator for unit testing, simulation, and deterministic runs.
    /// </summary>
    public class StandardRNG : IRandomNumberGenerator
    {
        private readonly Random _random;

        public StandardRNG()
        {
            _random = new Random();
        }

        public StandardRNG(int seed)
        {
            _random = new Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        public int NextWeightedIndex(int[] weights)
        {
            int totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }

            int roll = _random.Next(0, totalWeight);
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
    }
}
