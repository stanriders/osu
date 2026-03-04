// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class FlowEvaluator
    {
        /// <summary>
        /// it do be flowing a lil
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || current.Index <= 1 || current.Previous(0).BaseObject is Spinner)
                return 0;

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuLastObj = (OsuDifficultyHitObject)current.Previous(0);

            const int radius = OsuDifficultyHitObject.NORMALISED_RADIUS;
            const int diameter = OsuDifficultyHitObject.NORMALISED_DIAMETER;

            // Calculate the velocity to the current hitobject, which starts with a base distance / time assuming the last object is a hitcircle.
            double currDistance = osuCurrObj.JumpDistance;
            double currVelocity = currDistance / osuCurrObj.AdjustedDeltaTime;

            // As above, do the same for the previous hitobject.
            double prevDistance = osuLastObj.JumpDistance;
            double prevVelocity = prevDistance / osuLastObj.AdjustedDeltaTime;

            double acuteAngleBonus = 0;

            double aimStrain = currVelocity; // Start strain with regular velocity.

            if (osuCurrObj.Angle != null && osuLastObj.Angle != null)
            {
                double currAngle = osuCurrObj.Angle.Value;
                double lastAngle = osuLastObj.Angle.Value;

                // Rewarding angles, take the smaller velocity as base.
                double angleBonus = Math.Min(currVelocity, prevVelocity);

                acuteAngleBonus = calcAcuteAngleBonus(currAngle);

                acuteAngleBonus *= angleBonus;
            }

            aimStrain *= Math.Min(2, Math.Max(osuCurrObj.AdjustedDeltaTime, osuLastObj.AdjustedDeltaTime) / Math.Min(osuCurrObj.AdjustedDeltaTime, osuLastObj.AdjustedDeltaTime));

            aimStrain += acuteAngleBonus * 2;

            // Apply high circle size bonus
            aimStrain *= osuCurrObj.SmallCircleBonus;

            //aimStrain *= highBpmBonus(osuCurrObj.AdjustedDeltaTime, osuCurrObj.LazyJumpDistance);

            aimStrain *= DifficultyCalculationUtils.Smootherstep(osuCurrObj.LazyJumpDistance, 0, OsuDifficultyHitObject.NORMALISED_RADIUS);

            return aimStrain;
        }

        // We decrease strain for distances <radius to fix cases where doubles with no aim requirement
        // have their strain buffed incredibly high due to the delta time.
        // These objects do not require any movement, so it does not make sense to award them.
        private static double highBpmBonus(double ms, double distance) => 1 / (1 - Math.Pow(0.03, Math.Pow(ms / 1000, 0.65)))
                                                                          * DifficultyCalculationUtils.Smootherstep(distance, 0, OsuDifficultyHitObject.NORMALISED_RADIUS);

        private static double calcAcuteAngleBonus(double angle) => DifficultyCalculationUtils.Smoothstep(angle, double.DegreesToRadians(140), double.DegreesToRadians(40));
    }
}
