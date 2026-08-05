// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Difficulty.Skills
{
    public class HarmonicSkillAttributes : ISkillAttributes
    {
        public double Difficulty { get; init; }
        public List<double> ObjectDifficulties { get; init; } = new List<double>();
        public double TopWeightedObjectDifficultiesCount { get; init; }
    }

    public abstract class HarmonicSkill : ISkill
    {
        /// <summary>
        /// The sum of object weights, calculated during summation.
        /// Required for any calculations which need to normalise difficulty value.
        /// </summary>
        protected double ObjectWeightSum;

        /// <summary>
        /// Scaling factor applied as HarmonicScale / (1 + index) during weight calculations.
        /// A higher value will increase the influence of the hardest object difficulties during summation.
        /// </summary>
        protected virtual double HarmonicScale => 1.0;

        /// <summary>
        /// Exponent that controls the rate of which decay increases as the index increases.
        /// Values closer to 1 decay faster whilst lower values give more weight to lower object difficulties.
        /// </summary>
        protected virtual double DecayExponent => 0.9;

        public IReadOnlyList<Mod> Mods { get; init; }
        public IReadOnlyList<DifficultyHitObject> DifficultyHitObjects { get; init; }

        protected HarmonicSkill(Mod[] mods, DifficultyHitObject[] difficultyHitObjects)
        {
            Mods = mods;
            DifficultyHitObjects = difficultyHitObjects;
        }

        /// <summary>
        /// Returns the difficulty value of the current <see cref="DifficultyHitObject"/>. This value is calculated with or without respect to previous objects.
        /// </summary>
        protected abstract double ObjectDifficultyOf(DifficultyHitObject current);

        /// <summary>
        /// Transforms the object difficulties specifically for final difficulty summation.
        /// This can be used to decrease weight of certain objects based on a skill-specific criteria.
        /// </summary>
        protected virtual List<double> GetTransformedDifficulties(List<double> difficulties) => difficulties;

        public virtual ISkillAttributes Process()
        {
            var objectDifficulties = new List<double>();

            foreach (var difficultyHitObject in DifficultyHitObjects)
            {
                objectDifficulties.Add(ObjectDifficultyOf(difficultyHitObject));
            }

            double difficulty = aggregate(objectDifficulties);

            return new HarmonicSkillAttributes
            {
                Difficulty = difficulty,
                ObjectDifficulties = objectDifficulties,
                TopWeightedObjectDifficultiesCount = CountTopWeightedObjectDifficulties(objectDifficulties, difficulty)
            };
        }

        public virtual IEnumerable<TimedSkillAttributes> ProcessTimed()
        {
            var objectDifficulties = new List<double>();

            foreach (var difficultyHitObject in DifficultyHitObjects)
            {
                objectDifficulties.Add(ObjectDifficultyOf(difficultyHitObject));

                double difficulty = aggregate(objectDifficulties);

                yield return new TimedSkillAttributes(new HarmonicSkillAttributes
                {
                    Difficulty = difficulty,
                    ObjectDifficulties = objectDifficulties,
                    TopWeightedObjectDifficultiesCount = CountTopWeightedObjectDifficulties(objectDifficulties, difficulty)
                }, difficultyHitObject.EndTime);
            }
        }

        private double aggregate(List<double> objectDifficulties)
        {
            if (objectDifficulties.Count == 0)
                return 0;

            // Objects with 0 difficulty are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
            // These objects will not contribute to the difficulty.
            var difficulties = objectDifficulties;

            if (difficulties.Count == 0)
                return 0;

            difficulties = GetTransformedDifficulties(difficulties);

            double difficulty = 0;
            int index = 0;

            foreach (double obj in difficulties.OrderDescending().Where(v => v > 0))
            {
                // Use a harmonic sum that considers each object of the map according to a predefined weight.
                double weight = (1 + (HarmonicScale / (1 + index))) / (DiffUtils.Pow(index, DecayExponent) + 1 + (HarmonicScale / (1 + index)));

                ObjectWeightSum += weight;

                difficulty += obj * weight;
                index += 1;
            }

            return difficulty;
        }

        /// <summary>
        /// Calculates the number of object difficulties weighted against the top object difficulty.
        /// </summary>
        protected virtual double CountTopWeightedObjectDifficulties(List<double> objectDifficulties, double difficultyValue)
        {
            if (objectDifficulties.Count == 0)
                return 0.0;

            if (ObjectWeightSum == 0)
                return 0.0;

            double consistentTopObject = difficultyValue / ObjectWeightSum; // What would the top difficulty be if all object difficulties were identical

            if (consistentTopObject == 0)
                return 0;

            return objectDifficulties.Sum(d => DiffUtils.Logistic(d / consistentTopObject, 0.88, 10, 1.1));
        }

        public static double DifficultyToPerformance(double difficulty) => 4.0 * DiffUtils.Pow(difficulty, 3);
    }
}
