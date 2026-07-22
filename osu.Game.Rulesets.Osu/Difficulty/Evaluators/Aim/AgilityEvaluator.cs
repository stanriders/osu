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

            if (osuLastObj != null)
            {
                var acuteAngleBonus = Math.Max(1, (DiffUtils.MillisecondsToBPM(osuCurrObj.AdjustedDeltaTime, 2) - 150) / 125); //1 + DiffUtils.Smootherstep(DiffUtils.MillisecondsToBPM(osuCurrObj.AdjustedDeltaTime, 2), 300, 400);

                if (osuCurrObj.Angle != null && osuLastObj.Angle != null)
                {
                    acuteAngleBonus *= 1 + 1 - Math.Min(SnapAimEvaluator.CalcAngleAcuteness(osuCurrObj.Angle.Value), DiffUtils.Pow(SnapAimEvaluator.CalcAngleAcuteness(osuLastObj.Angle.Value), 3));
                }

                agilityDifficulty *= Math.Max(1, acuteAngleBonus * 0.5);
            }

            agilityDifficulty *= DiffUtils.Pow(osuCurrObj.SmallCircleBonus, 1.5);

            agilityDifficulty *= highBpmBonus(osuCurrObj.AdjustedDeltaTime);

            return agilityDifficulty;
        }

        private static double highBpmBonus(double ms) => 1 / (1 - DiffUtils.Pow(0.2, ms / 1000));
    }
}
