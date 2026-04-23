// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators.Aim
{
    public static class FlowAimEvaluator
    {
        private const double velocity_change_multiplier = 0.52;

        /// <summary>
        /// Evaluates difficulty of "flow aim" - aiming pattern where player doesn't stop their cursor on every object and instead "flows" through them.
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current, Movement currentMovement)
        {
            if (current.BaseObject is Spinner || current.Index <= 1 || current.Previous(0).BaseObject is Spinner)
                return 0;

            var osuCurrObj = (OsuDifficultyHitObject)current;

            var previousMovement = currentMovement.PreviousMovement!;
            var prevPrevMovement = previousMovement.PreviousMovement!;

            double currVelocity = currentMovement.Distance / currentMovement.Time;
            double prevVelocity = previousMovement.Distance / previousMovement.Time;

            double currAngle = currentMovement.Angle(previousMovement);
            double prevAngle = previousMovement.Angle(prevPrevMovement);

            double flowDifficulty = currVelocity;

            if (currentMovement.PrimaryMovement && previousMovement.PrimaryMovement)
            {
                // Apply high circle size bonus to the base velocity.
                // We use reduced CS bonus here because the bonus was made for an evaluator with a different d/t scaling
                flowDifficulty *= Math.Sqrt(osuCurrObj.SmallCircleBonus);

                // Rhythm changes are harder to flow
                flowDifficulty *= 1 + Math.Min(0.25,
                    Math.Pow((Math.Max(currentMovement.Time, previousMovement.Time) - Math.Min(currentMovement.Time, previousMovement.Time)) / 50, 4));

                // Low angular velocity flow (angles are consistent) is easier to follow than erratic flow

                double angleDifference = Math.Abs(currAngle - prevAngle);
                double angleDifferenceAdjusted = Math.Sin(angleDifference / 2) * 180.0;
                double angularVelocity = angleDifferenceAdjusted / (currentMovement.Time * 0.1);
                flowDifficulty *= 0.8 + Math.Sqrt(angularVelocity / 270.0);

                // If all three notes are overlapping - don't reward bonuses as you don't have to do additional movement
                double overlappedNotesWeight = 1;

                if (current.Index > 2)
                {
                    double o1 = calculateOverlapFactor(currentMovement, previousMovement);
                    double o2 = calculateOverlapFactor(currentMovement, prevPrevMovement);
                    double o3 = calculateOverlapFactor(previousMovement, prevPrevMovement);

                    overlappedNotesWeight = 1 - o1 * o2 * o3;
                }

                // Acute angles are also hard to flow
                flowDifficulty += currVelocity *
                                  SnapAimEvaluator.CalcAngleAcuteness(currAngle) *
                                  overlappedNotesWeight;

                if (Math.Max(prevVelocity, currVelocity) != 0)
                {
                    // Scale with ratio of difference compared to 0.5 * max dist.
                    double distRatio = DifficultyCalculationUtils.Smoothstep(Math.Abs(prevVelocity - currVelocity) / Math.Max(prevVelocity, currVelocity), 0, 1);

                    // Reward for % distance up to 125 / strainTime for overlaps where velocity is still changing.
                    double overlapVelocityBuff = Math.Min(OsuDifficultyHitObject.NORMALISED_DIAMETER * 1.25 / Math.Min(currentMovement.Time, previousMovement.Time),
                        Math.Abs(prevVelocity - currVelocity));

                    flowDifficulty += overlapVelocityBuff *
                                      distRatio *
                                      overlappedNotesWeight *
                                      velocity_change_multiplier;
                }

                // Final velocity is being raised to a power because flow difficulty scales harder with both high distance and time, and we want to account for that
                flowDifficulty = Math.Pow(flowDifficulty, 1.45);
            }

            // Reduce difficulty for low spacing since spacing below radius is always to be flowed
            return flowDifficulty * DifficultyCalculationUtils.Smootherstep(currentMovement.Distance, 0, OsuDifficultyHitObject.NORMALISED_RADIUS);
        }

        private static double calculateOverlapFactor(Movement first, Movement second)
        {
            double objectRadius = first.StartRadius;

            double distance = Vector2.Distance(first.Start, second.Start);
            return Math.Clamp(1 - Math.Pow(Math.Max(distance - objectRadius, 0) / objectRadius, 2), 0, 1);
        }
    }
}
