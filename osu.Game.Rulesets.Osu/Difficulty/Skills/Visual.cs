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
        private const double rhythm_multiplier = 45.0;
        private const double aim_multiplier = 10.0;

        private double skillMultiplier => 1.2;
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

            if(osuCurrent.NoteDensity > 1.1)
            {
                var visibleObjects = osuCurrent.visibleObjects;

                rhythmReadingComplexity = calculateRhythmReading(visibleObjects, (OsuHitObject)Previous[0].BaseObject, osuHitObject, visibleObjects[0], clockRate) * rhythm_multiplier;
                aimReadingComplexity = calculateAimReading(visibleObjects, osuHitObject, visibleObjects[0]) * aim_multiplier;
            }

            var strain = (rhythmReadingComplexity + aimReadingComplexity) * 8.0 * (Mods.Any(h => h is OsuModHidden) ? 1 + (osuCurrent.NoteDensity / 10) : 1.0);

            //if (strain > 20)
            //    Console.WriteLine( Math.Round((current.StartTime / 1000.0), 3).ToString() + "  " + Math.Round(strain, 3).ToString() + "   " + Math.Round(rhythmReadingComplexity, 3).ToString() + "  " + Math.Round(aimReadingComplexity, 3).ToString());

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
            var prevCurrDistance = (currentPosition - prevPosition).Length / (currentObject.Radius * 2);

            var nextPosition = nextObject.StackedPosition;
            var currNextDistance = (nextPosition - currentPosition).Length / (currentObject.Radius * 2);

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
                var visibleDistance = (currentPosition - visibleObjectPosition).Length / (currentObject.Radius * 2);

                overlapness += (logistic((0.5 - visibleDistance) / 0.1) - 0.2);

                // this is temp until sliders get proper reading impl
                if (visibleObject is Slider)
                    overlapness /= 2.0;

                overlapness *= visibleObject.GetVisibiltyAtTime(currentObject.StartTime);

                overlapness = Math.Max(0, overlapness);
            }
            overlapness /= visibleObjects.Count / 2.0;

            var distanceRatio = currNextDistance / (prevCurrDistance + 1e-10);

            var changeRatio = distanceRatio * tRatio;
            var spacingChange = Math.Min(0.0, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(0.0, Math.Pow(distanceRatio - 1, 2) * 1000);

            return overlapness * spacingChange;
        }

        private double calculateAimReading(List<OsuHitObject> visibleObjects, OsuHitObject currentObject, OsuHitObject nextObject)
        {
            var intersections = 0.0;

            var currentPosition = currentObject.StackedPosition;
            var nextPosition = nextObject.StackedPosition;
            var movementDistance = (nextPosition - currentPosition).Length / (currentObject.Radius * 2);

            var prevVisualObjectPosition = nextPosition;

            // calculate amount of circles intersecting the movement excluding current and next circles
            for (int i = 1; i < visibleObjects.Count; i++)
            {
                var visibleObject = visibleObjects[i];
                var visibleObjectPosition = visibleObject.StackedPosition;
                var visibleToCurrentDistance = (currentPosition - visibleObjectPosition).Length / (currentObject.Radius * 2);
                var visibleToNextDistance = (nextPosition - visibleObjectPosition).Length / (currentObject.Radius * 2);
                var prevVisibleToVisible = (prevVisualObjectPosition - visibleObjectPosition).Length / (currentObject.Radius * 2);

                //Console.WriteLine(visibleObject.GetVisibiltyAtTime(currentObject.StartTime));

                //Console.WriteLine((currentObject.StartTime / 1000.0).ToString() + " " + (visibleObject.StartTime / 1000.0).ToString() + " " + checkMovementIntersect(currentObject.StackedPosition, nextObject.StackedPosition, visibleObject.StackedPosition, currentObject.Radius).ToString());
                //Console.WriteLine(movementDistance);

                // scale the bonus by distance of movement and distance between intersected object and movement end object
                var intersectionBonus = checkMovementIntersect(currentObject.StackedPosition, nextObject.StackedPosition, visibleObject.StackedPosition, currentObject.Radius) *
                                        logistic((movementDistance - 1.5) / 0.5) *
                                        logistic((visibleToCurrentDistance - 1.5) / 0.5) *
                                        logistic((visibleToNextDistance - 1.5) / 0.5) *
                                        logistic((prevVisibleToVisible - 1.5) / 0.5) *
                                        visibleObject.GetVisibiltyAtTime(currentObject.StartTime) *
                                        (visibleObject.StartTime - currentObject.StartTime) / 500;

                //Console.WriteLine(visibleToNextDistance);

                // TODO: approach circle intersections

                prevVisualObjectPosition = visibleObjectPosition;
                intersections += intersectionBonus;
            }

            return intersections;
        }

        private double checkMovementIntersect(Vector2 startCircle, Vector2 endCircle, Vector2 visibleObject, double radius)
        {
            var numerator = Math.Abs( ((endCircle.X - startCircle.X) * (startCircle.Y - visibleObject.Y)) - ((startCircle.X - visibleObject.X) * (endCircle.Y - startCircle.Y)));
            var denominator = Math.Sqrt(Math.Pow(endCircle.X - startCircle.X, 2) + Math.Pow(endCircle.Y - startCircle.Y, 2));

            if (double.IsNaN(numerator / denominator))
                return 0;

            return 1 - Math.Min(1, (numerator / denominator) / radius);
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
