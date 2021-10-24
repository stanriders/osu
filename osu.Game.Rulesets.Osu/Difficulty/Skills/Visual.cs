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
        private const double rhythm_multiplier = 0.3;
        private const double aim_multiplier = 10.0;

        private double skillMultiplier => 1.2;
        private double strainDecayBase => 0.15;
        private double currentStrain = 1;

        protected override int HistoryLength => 2;

        private double clockRate { get; }

        private double strainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            if (Previous.Count < 2)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;
            var osuHitObject = (OsuHitObject)(osuCurrent.BaseObject);

            var rhythmReadingComplexity = 0.0;
            var aimReadingComplexity = 0.0;

            if(osuCurrent.NoteDensity > 1.1)
            {
                var visibleObjects = osuCurrent.visibleObjects;

                rhythmReadingComplexity = calculateRhythmReading(visibleObjects, (OsuHitObject)Previous[1].BaseObject, (OsuHitObject)Previous[0].BaseObject, osuHitObject, visibleObjects[0], clockRate) * rhythm_multiplier;
                aimReadingComplexity = calculateAimReading(visibleObjects, osuHitObject, visibleObjects[0]) * aim_multiplier;
            }

            var strain = (rhythmReadingComplexity + aimReadingComplexity) * 8.0 * (Mods.Any(h => h is OsuModHidden) ? 1 + (osuCurrent.NoteDensity / 8) : 1.0);

            //if (strain > 20)
            //    Console.WriteLine( Math.Round((current.StartTime / 1000.0), 3).ToString() + "  " + Math.Round(strain, 3).ToString() + "   " + Math.Round(rhythmReadingComplexity, 3).ToString() + "  " + Math.Round(aimReadingComplexity, 3).ToString());

            return strain;
        }

        private double calculateRhythmReading(List<OsuHitObject> visibleObjects,
                                                     OsuHitObject secondPrevObject,
                                                     OsuHitObject prevObject,
                                                     OsuHitObject currentObject,
                                                     OsuHitObject nextObject,
                                                     double clockRate)
        {
            if ((prevObject.StartTime - secondPrevObject.StartTime) - (currentObject.StartTime - prevObject.StartTime) < 20)
                return 0;

            var overlapness = 0.0;

            var currentPosition = currentObject.StackedPosition;

            // calculate if rhythm change correlates to spacing change
            var tPrevCurr = -1.0;
            var prevCurrDistance = -1.0;

            var lastVisibleObject = currentObject;

            // calculate how much visible objects overlap the previous
            for (int i = 0; i < visibleObjects.Count; i++)
            {
                var visibleObject = visibleObjects[i];
                var visibleObjectPosition = visibleObject.StackedPosition;
                var lastVisiblePosition = lastVisibleObject.StackedPosition;
                var currNextDistance = (visibleObjectPosition - currentPosition).Length / (currentObject.Radius * 2);

                var tCurrNext = (visibleObject.StartTime - currentObject.StartTime) / clockRate;
                var tRatio = Math.Max(tCurrNext / (tPrevCurr + 1e-10), tPrevCurr / (tCurrNext + 1e-10));

                if (tPrevCurr == -1)
                {
                    lastVisibleObject = visibleObject;
                    tPrevCurr = tCurrNext;
                    continue;
                }

                var distanceRatio = currNextDistance / (prevCurrDistance + 1e-10);
                var changeRatio = distanceRatio * tRatio;

                var spacingChange = Math.Min(1.2, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(1.0, Math.Pow(distanceRatio - 1, 2) * 1000);

                var visibleDistance = (lastVisiblePosition - visibleObjectPosition).Length / (currentObject.Radius * 2);

                overlapness += (logistic((0.3 - visibleDistance) / 0.1));

                overlapness *= spacingChange * visibleObject.GetVisibiltyAtTime(currentObject.StartTime);
                
                overlapness = Math.Max(0, overlapness);

                lastVisibleObject = visibleObject;
                tPrevCurr = tCurrNext;
            }

            return overlapness;
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

                // scale the bonus by distance of movement and distance between intersected object and movement end object
                var intersectionBonus = checkMovementIntersect(currentObject.StackedPosition, nextObject.StackedPosition, visibleObject.StackedPosition, currentObject.Radius) *
                                        logistic((movementDistance - 1.5) / 0.5) *
                                        logistic((visibleToCurrentDistance - 1.5) / 0.5) *
                                        logistic((visibleToNextDistance - 1.5) / 0.5) *
                                        logistic((prevVisibleToVisible - 1.5) / 0.5) *
                                        visibleObject.GetVisibiltyAtTime(currentObject.StartTime) *
                                        (visibleObject.StartTime - currentObject.StartTime) / 500;

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
