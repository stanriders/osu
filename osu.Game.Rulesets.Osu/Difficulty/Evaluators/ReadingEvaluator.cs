// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
        private const double rhythm_multiplier = 15.0;
        private const double aim_multiplier = 32.0;

        /// <summary>
        /// Calculates reading difficulty of the map
        /// </summary>
        public static double CalculateReadingDiff(DifficultyHitObject current, double fingerStrain, double clockRate, double preempt, bool hidden = false)
        {
            /*int noteDensity = (int)Math.Floor(noteDensities[i]) - 1; // exclude current circle
            if (noteDensity > hitObjects.Count - i)
                noteDensity = hitObjects.Count - i;*/
            if (current.BaseObject is Spinner)
            {
                return 0;
            }

            var noteDensity = CalculateNoteDensities(current, preempt);

            noteDensity = Math.Min(noteDensity, current.Index);

            double rhythmReadingComplexity = 0.0;
            double aimReadingComplexity = 0.0;

            if (noteDensity > 1)
            {
                var visibleObjects = new List<OsuHitObject>();

                for (int i = 0; i < (int)Math.Floor(noteDensity); i++)
                {
                    visibleObjects.Add((OsuHitObject)current.Previous(i).BaseObject);
                }

                var nextObject = current.Next(0);

                rhythmReadingComplexity = calculateRhythmReading(visibleObjects, (OsuHitObject)current.Previous(0).BaseObject, (OsuHitObject)current.BaseObject, (OsuHitObject)nextObject.BaseObject, fingerStrain, clockRate, hidden) * rhythm_multiplier;
                aimReadingComplexity = calculateAimReading(visibleObjects, (OsuHitObject)current.BaseObject, (OsuHitObject)nextObject.BaseObject, hidden) * aim_multiplier;
            }

            return Math.Pow(rhythmReadingComplexity + aimReadingComplexity, 2.5);
        }

        /// <summary>
        /// Calculates note density for every note
        /// </summary>
        public static double CalculateNoteDensities(DifficultyHitObject current, double preempt)
        {
            List<DifficultyHitObject> window = new List<DifficultyHitObject>();

            int next = 0;

            while (current.Next(next) != null && current.Next(next).StartTime < current.StartTime + preempt)
            {
                window.Add(current.Next(next));
                next++;
            }

            return calculateNoteDensity(current.StartTime, preempt, window);
        }

        private static double calculateNoteDensity(double time, double preempt, List<DifficultyHitObject> window)
        {
            double noteDensity = 0;

            foreach (var hitObject in window)
            {
                noteDensity += 1 - Math.Abs(hitObject.StartTime - time) / preempt;
            }

            return noteDensity;
        }

        private static double calculateRhythmReading(List<OsuHitObject> visibleObjects,
                                                     OsuHitObject prevObject,
                                                     OsuHitObject currentObject,
                                                     OsuHitObject nextObject,
                                                     double currentFingerStrain,
                                                     double clockRate,
                                                     bool hidden)
        {
            double overlapness = 0.0;
            var prevPosition = prevObject.StackedPosition;

            var currentPosition = currentObject.StackedPosition;
            float prevCurrDistance = ((currentPosition - prevPosition) / OsuDifficultyHitObject.NORMALISED_DIAMETER).Length;

            var nextPosition = nextObject.StackedPosition;
            float currNextDistance = ((nextPosition - currentPosition) / OsuDifficultyHitObject.NORMALISED_DIAMETER).Length;

            // buff overlapness if previous object was also overlapping
            overlapness += DifficultyCalculationUtils.Logistic((0.5 - prevCurrDistance) / 0.1) - 0.2;

            // calculate how much visible objects overlap current object
            for (int i = 1; i < visibleObjects.Count; i++)
            {
                var visibleObject = visibleObjects[i];
                var visibleObjectPosition = visibleObject.StackedPosition;
                float visibleDistance = ((currentPosition - visibleObjectPosition) / OsuDifficultyHitObject.NORMALISED_DIAMETER).Length;

                overlapness += DifficultyCalculationUtils.Logistic((0.5 - visibleDistance) / 0.1) - 0.2;

                // this is temp until sliders get proper reading impl
                if (visibleObject is Slider)
                    overlapness /= 2.0;

                overlapness = Math.Max(0, overlapness);
            }

            overlapness /= visibleObjects.Count / 2.0;

            // calculate if rhythm change correlates to spacing change
            double tPrevCurr = (currentObject.StartTime - prevObject.StartTime) / clockRate;
            double tCurrNext = (nextObject.StartTime - currentObject.StartTime) / clockRate;
            double tRatio = tCurrNext / (tPrevCurr + 1e-10);

            double distanceRatio = currNextDistance / (prevCurrDistance + 1e-10);

            double changeRatio = distanceRatio * tRatio;
            double spacingChange = Math.Min(1.05, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(1.00, Math.Pow(distanceRatio - 1, 2) * 1000);

            return Math.Pow(0.3, 2 / (currentFingerStrain + 1e-10)) * overlapness * spacingChange * (hidden ? 1.2 : 1.0);
        }

        private static double calculateAimReading(List<OsuHitObject> visibleObjects, OsuHitObject currentObject, OsuHitObject nextObject, bool hidden)
        {
            double intersections = 0.0;

            var currentPosition = currentObject.StackedPosition;
            var nextPosition = nextObject.StackedPosition;
            var nextVector = currentPosition - nextPosition;
            float movementDistance = ((nextPosition - currentPosition) / OsuDifficultyHitObject.NORMALISED_DIAMETER).Length;

            // calculate amount of circles intersecting the movement excluding current and next circles
            for (int i = 2; i < visibleObjects.Count; i++)
            {
                var visibleObject = visibleObjects[i];
                var visibleObjectPosition = visibleObject.StackedPosition;
                var visibleToCurrentVector = currentPosition - visibleObjectPosition;
                float visibleToNextDistance = ((nextPosition - visibleObjectPosition) / OsuDifficultyHitObject.NORMALISED_DIAMETER).Length;

                // scale the bonus by distance of movement and distance between intersected object and movement end object
                double intersectionBonus = checkMovementIntersect(nextVector, nextObject.Radius * 2, visibleToCurrentVector) *
                                           DifficultyCalculationUtils.Logistic((movementDistance - 3) / 0.7) *
                                           DifficultyCalculationUtils.Logistic((3 - visibleToNextDistance) / 0.7);

                // this is temp until sliders get proper reading impl
                if (visibleObject is Slider)
                    intersectionBonus *= 2.0;

                // TODO: approach circle intersections

                intersections += intersectionBonus;
            }

            return intersections / visibleObjects.Count;
        }

        private static double checkMovementIntersect(Vector2 direction, double radius, Vector2 endPoint)
        {
            double a = Vector2.Dot(direction, direction);
            double b = 2 * Vector2.Dot(endPoint, direction);
            double c = Vector2.Dot(endPoint, endPoint) - radius * radius;

            double discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                // no intersection
                return 0.0;
            }
            else
            {
                discriminant = Math.Sqrt(discriminant);

                double t1 = (-b - discriminant) / (2 * a);
                double t2 = (-b + discriminant) / (2 * a);

                if (t1 >= 0 && t1 <= 1)
                {
                    // t1 is the intersection, and it's closer than t2
                    return t1;
                }

                // here t1 didn't intersect so we are either started
                // inside the sphere or completely past it
                if (t2 >= 0 && t2 <= 1)
                {
                    return t2 / 2.0;
                }

                return 0.0;
            }
        }
    }
}
