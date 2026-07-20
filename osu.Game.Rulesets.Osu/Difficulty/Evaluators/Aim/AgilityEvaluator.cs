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
        /// <summary>
        /// Evaluates the difficulty of fast aiming
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuLastObj = (OsuDifficultyHitObject)current.Previous(0);

            double agilityDifficulty = 1000 / osuCurrObj.AdjustedDeltaTime;

            if (osuLastObj != null && Math.Max(osuCurrObj.AdjustedDeltaTime, osuLastObj.AdjustedDeltaTime) < 1.25 * Math.Min(osuCurrObj.AdjustedDeltaTime, osuLastObj.AdjustedDeltaTime)) // If rhythms are the same.
            {
                if (osuCurrObj.Angle != null && osuLastObj.Angle != null)
                {
                    double currAngle = osuCurrObj.Angle.Value;
                    double lastAngle = osuLastObj.Angle.Value;

                    var acuteAngleBonus = 1.0;

                    // Penalize angle repetition. It is important to do it _before_ multiplying by anything because we compare raw acuteness here
                    var repetitionNerf = 1 + (0.08 + 0.92 * (1 - Math.Min(SnapAimEvaluator.CalcAngleAcuteness(currAngle), DiffUtils.Pow(SnapAimEvaluator.CalcAngleAcuteness(lastAngle), 3))));

                    // Apply acute angle bonus for BPM above 300 1/2 and distance more than one diameter
                    acuteAngleBonus *= 1 + DiffUtils.Smootherstep(DiffUtils.MillisecondsToBPM(osuCurrObj.AdjustedDeltaTime, 2), 300, 400);

                    agilityDifficulty *= Math.Max(1, acuteAngleBonus * 0.6 * repetitionNerf);
                }
            }

            agilityDifficulty *= DiffUtils.Pow(osuCurrObj.SmallCircleBonus, 1.5);

            agilityDifficulty *= highBpmBonus(osuCurrObj.AdjustedDeltaTime);

            return agilityDifficulty;
        }

        private static double highBpmBonus(double ms) => 1 / (1 - DiffUtils.Pow(0.2, ms / 1000));
    }
}
