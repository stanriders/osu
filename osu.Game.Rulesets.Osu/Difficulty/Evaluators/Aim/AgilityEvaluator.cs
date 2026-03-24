// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators.Aim
{
    public static class AgilityEvaluator
    {
        private const double distance_cap = OsuDifficultyHitObject.NORMALISED_DIAMETER * 1.25; // 1.25 circles distance between centers

        /// <summary>
        /// Evaluates the difficulty of fast aiming
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuPrevObj = current.Index > 0 ? (OsuDifficultyHitObject)current.Previous(0) : null;

            double travelDistance = osuPrevObj?.LazyTravelDistance ?? 0;
            double distance = travelDistance + osuCurrObj.LazyJumpDistance;

            double distanceScaled = Math.Min(distance, distance_cap) / distance_cap;

            double strain = distanceScaled * 1000 / osuCurrObj.AdjustedDeltaTime;

            if (osuCurrObj.Angle != null && osuPrevObj?.Angle != null)
            {
                double currAngle = osuCurrObj.Angle.Value;
                double lastAngle = osuPrevObj.Angle.Value;

                double currDistance = osuCurrObj.LazyJumpDistance;
                double currVelocity = currDistance / osuCurrObj.AdjustedDeltaTime;

                double prevDistance = osuPrevObj.LazyJumpDistance;
                double prevVelocity = prevDistance / osuPrevObj.AdjustedDeltaTime;

                // Rewarding angles, take the smaller velocity as base.
                double angleBonus = Math.Min(currVelocity, prevVelocity);

                if (Math.Max(osuCurrObj.AdjustedDeltaTime, osuPrevObj.AdjustedDeltaTime) < 1.25 * Math.Min(osuCurrObj.AdjustedDeltaTime, osuPrevObj.AdjustedDeltaTime)) // If rhythms are the same.
                {
                    double acuteAngleBonus = SnapAimEvaluator.CalcAcuteAngleBonus(currAngle);

                    // Penalize angle repetition.
                    acuteAngleBonus *= 0.08 + 0.92 * (1 - Math.Min(acuteAngleBonus, Math.Pow(SnapAimEvaluator.CalcAcuteAngleBonus(lastAngle), 3)));

                    // Apply acute angle bonus for BPM above 300 1/2 and distance more than one diameter
                    acuteAngleBonus *= angleBonus *
                                       DifficultyCalculationUtils.Smootherstep(DifficultyCalculationUtils.MillisecondsToBPM(osuCurrObj.AdjustedDeltaTime, 2), 300, 400) *
                                       DifficultyCalculationUtils.Smootherstep(currDistance, 0, OsuDifficultyHitObject.NORMALISED_DIAMETER * 2);

                    strain += acuteAngleBonus * 20.41;
                }
            }

            strain *= osuCurrObj.SmallCircleBonus;

            strain *= highBpmBonus(osuCurrObj.AdjustedDeltaTime);

            return strain * DifficultyCalculationUtils.Smootherstep(distance, 0, OsuDifficultyHitObject.NORMALISED_RADIUS);
        }

        private static double highBpmBonus(double ms) => 1 / (1 - Math.Pow(0.3, Math.Pow(ms / 1000, 0.9)));
    }
}
