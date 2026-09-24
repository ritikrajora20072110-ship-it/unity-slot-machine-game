namespace SlotGame.Core
{
    /// <summary>
    /// Contract for RNG implementations ensuring decoupling, provable fairness, and deterministic testing.
    /// </summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>
        /// Returns an integer within [minInclusive, maxExclusive).
        /// </summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>
        /// Returns a floating point value in [0.0, 1.0).
        /// </summary>
        float NextFloat();

        /// <summary>
        /// Selects an index according to weighted probabilities.
        /// </summary>
        /// <param name="weights">Array of relative weights for each outcome.</param>
        /// <returns>Selected index from 0 to weights.Length - 1.</returns>
        int NextWeightedIndex(int[] weights);
    }
}
