// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Utils;
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
            var osuPrevObj = (OsuDifficultyHitObject?)current.Previous();

            double numerator = 1;

            if (osuCurrObj.Angle != null && osuPrevObj?.Angle != null)
            {
                // angle switching bonus
                numerator += 0.5 * (1 - Math.Min(AngleUtils.CalculateAcuteness(osuCurrObj.Angle.Value), DiffUtils.Pow(AngleUtils.CalculateAcuteness(osuPrevObj.Angle.Value), 3)));
                numerator += AngleUtils.CalculateWideness(osuCurrObj.Angle.Value);
            }

            double agilityDifficulty = numerator / DiffUtils.Pow(osuCurrObj.AdjustedDeltaTime, 3);

            agilityDifficulty *= osuCurrObj.SmallCircleBonus;

            return agilityDifficulty * 1_000_000;
        }
    }
}
