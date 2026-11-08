// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Aggregation;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators.Speed;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to press keys with regards to keeping up with the speed at which objects need to be hit.
    /// </summary>
    public class Speed : Skill
    {
        private readonly int hitCircleCount;
        private readonly int sliderCount;
        private readonly List<double> sliderStrains = new List<double>();

        private double currentStrain;
        private double harmonicWeightSum;

        public Speed(Mod[] mods, int hitCircleCount, int sliderCount)
            : base(mods)
        {
            this.hitCircleCount = hitCircleCount;
            this.sliderCount = sliderCount;
        }

        private double strainDecay(double ms) => DiffUtils.Pow(0.3, ms / 1000);

        protected override double ProcessInternal(DifficultyHitObject current)
        {
            const double skill_multiplier = 66.2;

            if (Mods.Any(m => m is OsuModRelax))
                return 0;

            double decay = strainDecay(((OsuDifficultyHitObject)current).AdjustedDeltaTime);

            currentStrain *= decay;
            currentStrain += calculateAdjustedDifficulty(current) * (1 - decay) * skill_multiplier;

            double currentRhythm = RhythmEvaluator.EvaluateDifficultyOf(current);

            double totalStrain = currentStrain * currentRhythm;

            totalStrain += computeAccuracyValue((OsuDifficultyHitObject)current);

            if (current.BaseObject is Slider)
                sliderStrains.Add(totalStrain);

            return totalStrain;
        }

        private double calculateAdjustedDifficulty(DifficultyHitObject current)
        {
            double difficulty = SpeedEvaluator.EvaluateDifficultyOf(current);

            if (Mods.Any(m => m is OsuModAutopilot))
                difficulty *= 0.5;

            return difficulty;
        }

        private double computeAccuracyValue(OsuDifficultyHitObject current)
        {
            // This percentage only considers HitCircles of any value - in this part of the calculation we focus on hitting the timing hit window.
            var usingClassicSliderAccuracy = Mods.OfType<OsuModClassic>().Any(m => m.NoSliderHeadAccuracy.Value);
            var usingScoreV2 = Mods.Any(m => m is ModScoreV2);

            int amountHitObjectsWithAccuracy = hitCircleCount;
            if (!usingClassicSliderAccuracy || usingScoreV2)
                amountHitObjectsWithAccuracy += sliderCount;

            // Lots of arbitrary values from testing.
            // Considering to use derivation from perfect accuracy in a probabilistic manner - assume normal distribution.
            double accuracyValue = DiffUtils.Pow(1.52163, current.OverallDifficulty) * DiffUtils.Pow(1, 24) * 2.83;

            // Bonus for many hitcircles - it's harder to keep good accuracy up for longer.
            accuracyValue *= amountHitObjectsWithAccuracy < 1000
                ? DiffUtils.Pow(amountHitObjectsWithAccuracy / 1000.0, 0.3)
                : DiffUtils.Pow(amountHitObjectsWithAccuracy / 1000.0, 0.1);

            // Increasing the accuracy value by object count for Blinds isn't ideal, so the minimum buff is given.
            if (Mods.Any(m => m is OsuModBlinds))
                accuracyValue *= 1.14;
            /*else if (score.Mods.Any(m => m is OsuModTraceable))
            {
                // Decrease bonus for AR > 10
                accuracyValue *= 1 + 0.08 * DiffUtils.ReverseLerp(current, 11.5, 10);
            }*/

            return accuracyValue * 0.4;
        }

        public override double DifficultyValue()
        {
            if (ObjectDifficulties.Count == 0)
                return 0;

            (double difficulty, harmonicWeightSum) = HarmonicSeries.Aggregate(ObjectDifficulties, harmonicScale: 20);

            return difficulty;
        }

        public double RelevantObjectCount()
        {
            if (ObjectDifficulties.Count == 0)
                return 0;

            double maxStrain = ObjectDifficulties.Max();

            if (maxStrain == 0)
                return 0;

            return ObjectDifficulties.Sum(strain => DiffUtils.Logistic(strain / maxStrain, 0.5, 12.0));
        }

        public virtual double CountTopWeightedObjectDifficulties(double difficultyValue)
        {
            if (ObjectDifficulties.Count == 0)
                return 0.0;

            if (harmonicWeightSum == 0)
                return 0.0;

            double consistentTopObject = difficultyValue / harmonicWeightSum; // What would the top difficulty be if all object difficulties were identical

            if (consistentTopObject == 0)
                return 0;

            return ObjectDifficulties.Sum(d => DiffUtils.Logistic(d / consistentTopObject, 0.88, 10, 1.1));
        }

        public double CountTopWeightedSliders(double difficultyValue)
        {
            if (sliderStrains.Count == 0)
                return 0;

            if (harmonicWeightSum == 0)
                return 0.0;

            double consistentTopObject = difficultyValue / harmonicWeightSum; // What would the top note be if all note values were identical

            if (consistentTopObject == 0)
                return 0;

            // Use a weighted sum of all notes. Constants are arbitrary and give nice values
            return sliderStrains.Sum(s => DiffUtils.Logistic(s / consistentTopObject, 0.88, 10, 1.1));
        }
    }
}
