// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class FlowAimEvaluator
    {
        private const double single_spacing_threshold = OsuDifficultyHitObject.NORMALISED_DIAMETER * 1.25; // 1.25 circles distance between centers

        /// <summary>
        /// Evaluates the difficulty of tapping the current object, based on:
        /// <list type="bullet">
        /// <item><description>time between pressing the previous and current object,</description></item>
        /// <item><description>distance between those objects,</description></item>
        /// <item><description>and how easily they can be cheesed.</description></item>
        /// </list>
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current, IReadOnlyList<Mod> mods)
        {
            if (current.BaseObject is Spinner)
                return 0;

            // derive strainTime for calculation
            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuPrevObj = current.Index > 0 ? (OsuDifficultyHitObject)current.Previous(0) : null;

            double strainTime = osuCurrObj.AdjustedDeltaTime;

            double travelDistance = osuPrevObj?.TravelDistance ?? 0;
            double distance = travelDistance + osuCurrObj.LazyJumpDistance;

            // Cap distance at single_spacing_threshold
            //distance = Math.Min(distance, single_spacing_threshold);

            // Max distance bonus is 1 * `distance_multiplier` at single_spacing_threshold
            /*double distanceBonus = distance > single_spacing_threshold
                ? distance / single_spacing_threshold
                : Math.Pow(distance / single_spacing_threshold, 3.95);*/
            double distanceBonus = distance / single_spacing_threshold;
            distanceBonus *= 1;



            //distanceBonus *= osuCurrObj.FlowProbability;

            // Apply reduced small circle bonus because flow aim difficulty on small circles doesn't scale as hard as jumps
            distanceBonus *= Math.Sqrt(osuCurrObj.SmallCircleBonus);

            if (mods.OfType<OsuModAutopilot>().Any())
                distanceBonus = 0;

            double adjustedDistanceScale = 1.0;

            if (osuCurrObj.Angle != null && osuPrevObj?.Angle != null &&
                Math.Abs(osuCurrObj.DeltaTime - osuPrevObj.DeltaTime) < 25)
            {
                double angleDifference = Math.Abs(osuCurrObj.Angle.Value - osuPrevObj.Angle.Value);
                double angleDifferenceAdjusted = Math.Sin(angleDifference / 2) * 180.0;
                double angularVelocity = angleDifferenceAdjusted / (0.1 * strainTime);
                double angularVelocityBonus = Math.Max(0.0, 0.65 * Math.Log10(angularVelocity));

                // ensure that distance is consistent
                var distances = new List<double>();

                for (int i = 0; i < 16; i++)
                {
                    var obj = current.Index > i ? (OsuDifficultyHitObject)current.Previous(i) : null;
                    var objPrev = current.Index > i + 1 ? (OsuDifficultyHitObject)current.Previous(i + 1) : null;

                    if (obj != null && objPrev != null)
                    {
                        if (Math.Abs(obj.DeltaTime - objPrev.DeltaTime) > 25)
                            break;

                        distances.Add(Math.Abs(obj.MinimumJumpDistance - objPrev.MinimumJumpDistance));
                    }
                }

                double averageDistanceDifference = distances.Count > 0 ? distances.Average() : 0;
                double distanceDifferenceScaling = Math.Max(0, 1.0 - averageDistanceDifference / 30.0);
                adjustedDistanceScale = Math.Min(1.0, 0.6 + averageDistanceDifference / 30.0) + angularVelocityBonus * distanceDifferenceScaling;
            }

            distanceBonus *= adjustedDistanceScale;

            return distanceBonus * 1000 / strainTime;
        }
    }
}
