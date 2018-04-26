using System.Linq;

namespace Logic.Maths
{
    public static class DiceExtensions
    {
        public static int[] D6(this WellRng rng, int n)
        {
            var results = new int[n];
            for (var i = 0; i < n; i++)
            {
                results[i] = rng.NextInt(1, 6);
            }
            return results;
        }

        public static int D6(this WellRng rng)
        {
            return rng.NextInt(1, 6);
        }

        public static int Successes(this WellRng rng, int dice, int target)
        {
            return rng.D6(dice).Count(result => result >= target);
        }
    }
}