// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class SpeedAimEvaluator
    {
        public const double SINGLE_SPACING_THRESHOLD = OsuDifficultyHitObject.NORMALISED_DIAMETER * 1.25; // 1.25 circles distance between centers

        /// <summary>
        /// Evaluates the difficulty of aiming the current object, based on:
        /// <list type="bullet">
        /// <item><description>distance between the previous and current object</description></item>
        /// </list>
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuPrevObj = current.Index > 0 ? (OsuDifficultyHitObject)current.Previous(0) : null;

            double travelDistance = osuPrevObj?.LazyTravelDistance ?? 0;
            double distance = travelDistance + osuCurrObj.LazyJumpDistance;

            // Cap distance at single_spacing_threshold
            distance = Math.Min(distance, SINGLE_SPACING_THRESHOLD);

            // Max distance bonus is 1 * `distance_multiplier` at single_spacing_threshold
            double distanceBonus = Math.Pow(distance / SINGLE_SPACING_THRESHOLD, 3.95);

            // Apply reduced small circle bonus because flow aim difficulty on small circles doesn't scale as hard as jumps
            distanceBonus *= Math.Sqrt(osuCurrObj.SmallCircleBonus);

            double velocity = distanceBonus * 1000 / osuCurrObj.AdjustedDeltaTime;

            if (osuCurrObj.Angle != null && osuPrevObj?.Angle != null)
            {
                double angleDifference = Math.Abs(osuCurrObj.Angle.Value - osuPrevObj.Angle.Value);
                double angleDifferenceAdjusted = Math.Sin(angleDifference / 2) * 180.0;
                double angularVelocity = angleDifferenceAdjusted / (0.1 * osuCurrObj.AdjustedDeltaTime);
                double angularVelocityBonus = Math.Max(0.0, 0.65 * Math.Log10(angularVelocity));

                velocity *= 0.65 + angularVelocityBonus * 0.45;
            }

            velocity *= highBpmBonus(osuCurrObj.AdjustedDeltaTime);

            return velocity;
        }

        private static double highBpmBonus(double ms) => 1 / (1 - Math.Pow(0.3, ms / 1000));
    }
}
