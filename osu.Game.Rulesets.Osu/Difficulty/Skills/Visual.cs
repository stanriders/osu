// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using osu.Framework.Utils;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to read every object in the map.
    /// </summary>
    public class Visual : OsuStrainSkill
    {
        public Visual(Mod[] mods, double clockRate)
            : base(mods)
        {
            this.clockRate = clockRate;
        }
        private const double rhythm_multiplier = 15.0;
        private const double aim_multiplier = 32.0;

        private double skillMultiplier => 8;
        private double strainDecayBase => 0.15;
        private double currentStrain = 1;

        private double clockRate { get; }

        private double strainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            if (Previous.Count == 0)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;
            var osuHitObject = (OsuHitObject)(osuCurrent.BaseObject);

            var rhythmReadingComplexity = 0.0;
            var aimReadingComplexity = 0.0;

            if(osuCurrent.NoteDensity > 2)
            {
                var visibleObjects = osuCurrent.visibleObjects;

                rhythmReadingComplexity = calculateRhythmReading(visibleObjects, (OsuHitObject)Previous[0].BaseObject, osuHitObject, visibleObjects[0], clockRate) * rhythm_multiplier;
                aimReadingComplexity = calculateAimReading(visibleObjects, osuHitObject, visibleObjects[0]) * aim_multiplier;
            }

            var strain = (rhythmReadingComplexity + aimReadingComplexity) * 8.0;

            if (strain > 0)
                Console.WriteLine( Math.Round((current.StartTime / 1000.0), 3).ToString() + "  " + Math.Round(strain, 3).ToString() + "   " + Math.Round(rhythmReadingComplexity, 3).ToString() + "  " + Math.Round(aimReadingComplexity, 3).ToString());

            return strain;
        }

        private double calculateRhythmReading(List<OsuHitObject> visibleObjects,
                                                     OsuHitObject prevObject,
                                                     OsuHitObject currentObject,
                                                     OsuHitObject nextObject,
                                                     double clockRate)
        {
            var overlapness = 0.0;
            var prevPosition = prevObject.StackedPosition;

            var currentPosition = currentObject.StackedPosition;
            var prevCurrDistance = (currentPosition - prevPosition).Length;

            var nextPosition = nextObject.StackedPosition;
            var currNextDistance = (nextPosition - currentPosition).Length;

            // buff overlapness if previous object was also overlapping
            overlapness += logistic((0.5 - prevCurrDistance) / 0.1) - 0.2;

            // calculate if rhythm change correlates to spacing change
            var tPrevCurr = (currentObject.StartTime - prevObject.StartTime) / clockRate;
            var tCurrNext = (nextObject.StartTime - currentObject.StartTime) / clockRate;
            var tRatio = tCurrNext / (tPrevCurr + 1e-10);

            // calculate how much visible objects overlap current object
            for (int i = 0; i < visibleObjects.Count; i++)
            {
                var visibleObject = visibleObjects[i];
                var visibleObjectPosition = visibleObject.StackedPosition;
                var visibleDistance = (currentPosition - visibleObjectPosition).Length;

                overlapness += (logistic((0.5 - visibleDistance) / 0.1) - 0.2);

                // this is temp until sliders get proper reading impl
                if (visibleObject is Slider)
                    overlapness /= 2.0;

                overlapness = Math.Max(0, overlapness);
            }
            overlapness /= visibleObjects.Count / 2.0;

            var distanceRatio = currNextDistance / (prevCurrDistance + 1e-10);

            var changeRatio = distanceRatio * tRatio;
            var spacingChange = Math.Min(0.00, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(0.00, Math.Pow(distanceRatio - 1, 2) * 1000);

            //Console.WriteLine(overlapness.ToString() + " " +spacingChange.ToString());

            return Math.Pow(0.3, 2) * overlapness * spacingChange * (Mods.Any(h => h is OsuModHidden) ? 1.2 : 1.0);
        }

        private double calculateAimReading(List<OsuHitObject> visibleObjects, OsuHitObject currentObject, OsuHitObject nextObject)
        {
            var intersections = 0.0;

            var currentPosition = currentObject.StackedPosition;
            var nextPosition = nextObject.StackedPosition;
            var nextVector = currentPosition - nextPosition;
            var movementDistance = (nextPosition - currentPosition).Length;

            // calculate amount of circles intersecting the movement excluding current and next circles
            for (int i = 1; i < visibleObjects.Count; i++)
            {
                var visibleObject = visibleObjects[i];
                var visibleObjectPosition = visibleObject.StackedPosition;
                var visibleToCurrentVector = currentPosition - visibleObjectPosition;
                var visibleToNextDistance = (nextPosition - visibleObjectPosition).Length;

                //Console.WriteLine(checkMovementIntersect(nextVector, nextObject.Radius, visibleToCurrentVector));

                // scale the bonus by distance of movement and distance between intersected object and movement end object
                var intersectionBonus = checkMovementIntersect(nextVector, nextObject.Radius, visibleToCurrentVector) *
                                        logistic((movementDistance - 3.0) / 0.7) *
                                        logistic((3.0 - visibleToNextDistance) / 0.7);

                // this is temp until sliders get proper reading impl
                if (visibleObject is Slider)
                    intersectionBonus *= 2.0;

                // TODO: approach circle intersections

                intersections += Math.Sqrt(intersectionBonus);
            }

            Console.WriteLine(intersections);

            return intersections / visibleObjects.Count;
        }

        private double checkMovementIntersect(Vector2 direction, double radius, Vector2 endPoint)
        {
            float a = Vector2.Dot(direction, direction);
            float b = 2 * Vector2.Dot(direction, endPoint);
            float c = Vector2.Dot(endPoint, endPoint) - (float)(radius * radius);

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

        private double logistic(double x) => 1 / (1 + Math.Pow(Math.E, -x));

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time) => currentStrain * strainDecay(time - Previous[0].StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += strainValueOf(current) * skillMultiplier;

            return currentStrain;
        }
    }
}
