// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators.Aim
{
    public static class SnapAimEvaluator
    {
        private const double wide_angle_multiplier = 9.67;
        private const double acute_angle_multiplier = 2.41;
        private const double velocity_change_multiplier = 0.9;
        private const double wiggle_multiplier = 1.02; // WARNING: Increasing this multiplier beyond 1.02 reduces difficulty as distance increases. Refer to the desmos link above the wiggle bonus calculation
        private const double maximum_repetition_nerf = 0.15;
        private const double maximum_vector_influence = 0.5;

        private const int radius = OsuDifficultyHitObject.NORMALISED_RADIUS;
        private const int diameter = OsuDifficultyHitObject.NORMALISED_DIAMETER;

        /// <summary>
        /// Evaluates the difficulty of aiming the current object, based on:
        /// <list type="bullet">
        /// <item><description>cursor velocity to the current object,</description></item>
        /// <item><description>angle difficulty,</description></item>
        /// <item><description>sharp velocity increases,</description></item>
        /// <item><description>and slider difficulty.</description></item>
        /// </list>
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current, Movement currentMovement)
        {
            if (current.BaseObject is Spinner || current.Index < 1 || current.Previous(0).BaseObject is Spinner)
                return 0;

            var osuCurrObj = (OsuDifficultyHitObject)current;

            var previousMovement = currentMovement.PreviousMovement!;
            var prevPrevMovement = previousMovement.PreviousMovement;

            double currVelocity = currentMovement.Distance / currentMovement.Time;
            double prevVelocity = previousMovement.Distance / previousMovement.Time;

            double aimStrain = currVelocity; // Start strain with regular velocity.

            // Penalize angle repetition.
            if (prevPrevMovement != null)
                aimStrain *= vectorAngleRepetition(currentMovement, previousMovement, prevPrevMovement);

            if (prevPrevMovement != null)
            {
                double currAngle = currentMovement.Angle(previousMovement);
                double lastAngle = previousMovement.Angle(prevPrevMovement);

                // Rewarding angles, take the smaller velocity as base.
                double velocityInfluence = Math.Min(currVelocity, prevVelocity);

                double acuteAngleBonus = 0;

                if (Math.Max(currentMovement.Time, previousMovement.Time) < 1.25 * Math.Min(currentMovement.Time, previousMovement.Time)) // If rhythms are the same.
                {
                    acuteAngleBonus = CalcAngleAcuteness(currAngle);

                    // Penalize angle repetition. It is important to do it _before_ multiplying by anything because we compare raw acuteness here
                    acuteAngleBonus *= 0.08 + 0.92 * (1 - Math.Min(acuteAngleBonus, Math.Pow(CalcAngleAcuteness(lastAngle), 3)));

                    // Apply acute angle bonus for BPM above 300 1/2 and distance more than one diameter
                    acuteAngleBonus *= velocityInfluence *
                                       DifficultyCalculationUtils.Smootherstep(DifficultyCalculationUtils.MillisecondsToBPM(currentMovement.Time, 2), 300, 400) *
                                       DifficultyCalculationUtils.Smootherstep(currentMovement.Distance, 0, diameter * 2);
                }

                double wideAngleBonus = calcAngleWideness(currAngle);

                // Penalize angle repetition. It is important to do it _before_ multiplying by velocity because we compare raw wideness here
                wideAngleBonus *= 0.25 + 0.75 * (1 - Math.Min(wideAngleBonus, Math.Pow(calcAngleWideness(lastAngle), 3)));

                // Rescaling velocity for the wide angle bonus
                const double wide_angle_time_scale = 1.45;
                double wideAngleCurrVelocity = currentMovement.Distance / Math.Pow(currentMovement.Time, wide_angle_time_scale);
                double wideAnglePrevVelocity = previousMovement.Distance / Math.Pow(previousMovement.Time, wide_angle_time_scale);

                wideAngleBonus *= Math.Min(wideAngleCurrVelocity, wideAnglePrevVelocity);

                var osuLastObj = (OsuDifficultyHitObject)current.Previous(0);
                var osuLast2Obj = (OsuDifficultyHitObject)current.Previous(2);

                if (osuLast2Obj != null)
                {
                    // If objects just go back and forth through a middle point - don't give as much wide bonus
                    // Use Previous(2) and Previous(0) because angles calculation is done prevprev-prev-curr, so any object's angle's center point is always the previous object
                    var lastBaseObject = (OsuHitObject)osuLastObj.BaseObject;
                    var last2BaseObject = (OsuHitObject)osuLast2Obj.BaseObject;

                    float distance = (last2BaseObject.StackedPosition - lastBaseObject.StackedPosition).Length;

                    if (distance < 1)
                    {
                        wideAngleBonus *= 1 - 0.55 * (1 - distance);
                    }
                }

                // Add in acute angle bonus or wide angle bonus, whichever is larger.
                aimStrain += Math.Max(acuteAngleBonus * acute_angle_multiplier, wideAngleBonus * wide_angle_multiplier);

                // Apply wiggle bonus for jumps that are [radius, 3*diameter] in distance, with < 110 angle
                // https://www.desmos.com/calculator/dp0v0nvowc
                double wiggleBonus = velocityInfluence
                                     * DifficultyCalculationUtils.Smootherstep(currentMovement.Distance, radius, diameter)
                                     * Math.Pow(DifficultyCalculationUtils.ReverseLerp(currentMovement.Distance, diameter * 3, diameter), 1.8)
                                     * DifficultyCalculationUtils.Smootherstep(currAngle, double.DegreesToRadians(110), double.DegreesToRadians(60))
                                     * DifficultyCalculationUtils.Smootherstep(previousMovement.Distance, radius, diameter)
                                     * Math.Pow(DifficultyCalculationUtils.ReverseLerp(previousMovement.Distance, diameter * 3, diameter), 1.8)
                                     * DifficultyCalculationUtils.Smootherstep(lastAngle, double.DegreesToRadians(110), double.DegreesToRadians(60));

                aimStrain += wiggleBonus * wiggle_multiplier;
            }

            if (Math.Max(prevVelocity, currVelocity) != 0)
            {
                // Scale with ratio of difference compared to 0.5 * max dist.
                double distRatio = DifficultyCalculationUtils.Smoothstep(Math.Abs(prevVelocity - currVelocity) / Math.Max(prevVelocity, currVelocity), 0, 1);

                // Reward for % distance up to 125 / strainTime for overlaps where velocity is still changing.
                double overlapVelocityBuff = Math.Min(diameter * 1.25 / Math.Min(currentMovement.Time, previousMovement.Time), Math.Abs(prevVelocity - currVelocity));

                double velocityChangeBonus = overlapVelocityBuff * distRatio;

                // Penalize for rhythm changes.
                velocityChangeBonus *= Math.Pow(Math.Min(currentMovement.Time, previousMovement.Time) / Math.Max(currentMovement.Time, previousMovement.Time), 2);

                aimStrain += velocityChangeBonus * velocity_change_multiplier;
            }

            // Apply high circle size bonus
            if (currentMovement.PrimaryMovement)
                aimStrain *= osuCurrObj.SmallCircleBonus;

            aimStrain *= highBpmBonus(currentMovement.Time);

            return aimStrain;
        }

        private static double highBpmBonus(double ms) => 1 / (1 - Math.Pow(0.03, Math.Pow(ms / 1000, 0.65)));

        private static double vectorAngleRepetition(Movement current, Movement previous, Movement prevPrevious)
        {
            const double note_limit = 6;

            double constantAngleCount = 0;

            var loopMovement = current;

            for (int index = 0; index < note_limit; index++)
            {
                loopMovement = loopMovement.PreviousMovement;

                if (loopMovement.IsNull())
                    break;

                // Only consider vectors in the same jump section, stopping to change rhythm ruins momentum
                if (Math.Max(current.Time, loopMovement.Time) > 1.1 * Math.Min(current.Time, loopMovement.Time))
                    break;

                if (current.PreviousMovement != null && loopMovement.PreviousMovement != null)
                {
                    double angleDifference = Math.Abs(current.NormalizedAngleVector(current.PreviousMovement) - loopMovement.NormalizedAngleVector(loopMovement.PreviousMovement));
                    // Refer to this desmos for tuning, constants need to be precise so that values stay within the range of 0 and 1.
                    // https://www.desmos.com/calculator/a8jesv5sv2
                    constantAngleCount += Math.Cos(8 * Math.Min(double.DegreesToRadians(11.25), angleDifference));
                }
            }

            double vectorRepetition = Math.Pow(Math.Min(0.5 / constantAngleCount, 1), 2);

            double stackFactor = DifficultyCalculationUtils.Smootherstep(current.Distance, 0, OsuDifficultyHitObject.NORMALISED_DIAMETER);

            double currAngle = current.Angle(previous);
            double lastAngle = previous.Angle(prevPrevious);

            double angleDifferenceAdjusted = Math.Cos(2 * Math.Min(double.DegreesToRadians(45), Math.Abs(currAngle - lastAngle) * stackFactor));

            double baseNerf = 1 - maximum_repetition_nerf * CalcAngleAcuteness(lastAngle) * angleDifferenceAdjusted;

            return Math.Pow(baseNerf + (1 - baseNerf) * vectorRepetition * maximum_vector_influence * stackFactor, 2);
        }

        private static double calcAngleWideness(double angle) => DifficultyCalculationUtils.Smoothstep(angle, double.DegreesToRadians(40), double.DegreesToRadians(140));

        public static double CalcAngleAcuteness(double angle) => DifficultyCalculationUtils.Smoothstep(angle, double.DegreesToRadians(140), double.DegreesToRadians(40));
    }
}
