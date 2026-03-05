// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
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
            double distanceBonus = Math.Pow(distance / SINGLE_SPACING_THRESHOLD, 2.9);

            // Apply reduced small circle bonus because flow aim difficulty on small circles doesn't scale as hard as jumps
            distanceBonus *= Math.Pow(osuCurrObj.SmallCircleBonus, 0.7);

            double strain = distanceBonus * 1000 / osuCurrObj.AdjustedDeltaTime;

            double velocityChangeBonus = 0;

            if (osuPrevObj != null)
            {
                double currDistance = osuCurrObj.JumpDistance;
                double currVelocity = currDistance / osuCurrObj.AdjustedDeltaTime;

                // As above, do the same for the previous hitobject.
                double prevDistance = osuPrevObj.JumpDistance;
                double prevVelocity = prevDistance / osuPrevObj.AdjustedDeltaTime;

                const int diameter = OsuDifficultyHitObject.NORMALISED_DIAMETER;

                if (Math.Max(prevVelocity, currVelocity) != 0)
                {
                    // Scale with ratio of difference compared to 0.5 * max dist.
                    double distRatio = DifficultyCalculationUtils.Smoothstep(Math.Abs(prevVelocity - currVelocity) / Math.Max(prevVelocity, currVelocity), 0, 1);

                    // Reward for % distance up to 125 / strainTime for overlaps where velocity is still changing.
                    double overlapVelocityBuff = Math.Min(diameter * 1.25 / Math.Min(osuCurrObj.AdjustedDeltaTime, osuPrevObj.AdjustedDeltaTime), Math.Abs(prevVelocity - currVelocity));

                    velocityChangeBonus = overlapVelocityBuff * distRatio;

                    // Penalize for rhythm changes.
                    velocityChangeBonus *= Math.Pow(Math.Min(osuCurrObj.AdjustedDeltaTime, osuPrevObj.AdjustedDeltaTime) / Math.Max(osuCurrObj.AdjustedDeltaTime, osuPrevObj.AdjustedDeltaTime), 2);

                    velocityChangeBonus *= DifficultyCalculationUtils.Smootherstep(currDistance, 0, OsuDifficultyHitObject.NORMALISED_RADIUS);
                }
            }

            strain += velocityChangeBonus * 10;

            strain *= highBpmBonus(osuCurrObj.AdjustedDeltaTime);

            return strain;
        }

        private static double highBpmBonus(double ms) => 1 / (1 - Math.Pow(0.3, Math.Pow(ms / 1000, 0.9)));
    }
}
