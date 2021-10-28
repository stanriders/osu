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
        public Visual(Mod[] mods)
            : base(mods)
        {   }
        private const double rhythm_multiplier = 11.0;
        private const double aim_multiplier = 9.0;

        private double skillMultiplier => 0.2;
        private double strainDecayBase => 0.0;
        private double currentStrain = 1;

        protected override int HistoryLength => 2;

        private double strainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            if (Previous.Count < 2)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            var rhythmReadingComplexity = 0.0;
            var aimReadingComplexity = 0.0;

            if(osuCurrent.NoteDensity > 1)
            {
                var visibleObjects = osuCurrent.visibleObjects;

                rhythmReadingComplexity = calculateRhythmReading(visibleObjects, (OsuDifficultyHitObject)Previous[0], osuCurrent) * rhythm_multiplier;
                aimReadingComplexity = calculateAimReading(visibleObjects, osuCurrent, visibleObjects[0]) * aim_multiplier;
            }

            var strain = Math.Pow(rhythmReadingComplexity + aimReadingComplexity, 1.2) * 8.0;

            //if (strain > 20)
            //    Console.WriteLine( Math.Round((current.StartTime / 1000.0), 3).ToString() + "  " + Math.Round(strain, 3).ToString() + "   " + Math.Round(rhythmReadingComplexity, 3).ToString() + "  " + Math.Round(aimReadingComplexity, 3).ToString());

            return strain;
        }

        private double calculateRhythmReading(List<OsuDifficultyHitObject> visibleObjects, OsuDifficultyHitObject prevObject, OsuDifficultyHitObject currentObject)
        {
            if (prevObject.StrainTime - currentObject.StrainTime < 20)
                return 0;

            var overlapness = 0.0;
            var rhythmChanges = 0.0;

            // calculate how much visible objects overlap the previous
            for (int i = 1; i < visibleObjects.Count; i++)
            {
                var tCurrNext = visibleObjects[i].StrainTime;
                var tPrevCurr = visibleObjects[i - 1].StrainTime;

                var tRatio = Math.Max(tCurrNext / tPrevCurr, tPrevCurr / tCurrNext);
                if (Math.Abs(1 - tRatio) > 0.01)
                    rhythmChanges += 1 * visibleObjects[i].GetVisibilityAtTime(currentObject.StartTime);

                var distanceRatio = visibleObjects[i].JumpDistance / (visibleObjects[i - 1].JumpDistance + 1e-10);
                var changeRatio = distanceRatio * tRatio;

                var spacingChange = Math.Min(1.2, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(1.0, Math.Pow(distanceRatio - 1, 2) * 1000);

                overlapness += logistic((18 - visibleObjects[i].JumpDistance) / 5);

                overlapness *= spacingChange * visibleObjects[i].GetVisibilityAtTime(currentObject.StartTime);

                overlapness = Math.Max(0, overlapness);
            }

            return overlapness + (rhythmChanges / 4);
        }

        private double calculateAimReading(List<OsuDifficultyHitObject> visibleObjects, OsuDifficultyHitObject currentObject, OsuDifficultyHitObject nextObject)
        {
            var intersections = 0.0;

            var movementDistance = nextObject.JumpDistance;

            // calculate amount of circles intersecting the movement excluding current and next circles
            for (int i = 1; i < visibleObjects.Count; i++)
            {
                var visibleToCurrentDistance = currentObject.NormalisedDistanceTo(visibleObjects[i]);
                var visibleToNextDistance = nextObject.NormalisedDistanceTo(visibleObjects[i]);
                var prevVisibleToVisible = visibleObjects[i - 1].NormalisedDistanceTo(visibleObjects[i]);

                // scale the bonus by distance of movement and distance between intersected object and movement end object
                var intersectionBonus = checkMovementIntersect(currentObject, nextObject, visibleObjects[i]) *
                                        logistic((movementDistance - 78) / 26) *
                                        logistic((visibleToCurrentDistance - 78) / 26) *
                                        logistic((visibleToNextDistance - 78) / 26) *
                                        logistic((prevVisibleToVisible - 78) / 26) *
                                        visibleObjects[i].GetVisibilityAtTime(currentObject.StartTime) *
                                        (visibleObjects[i].StartTime - currentObject.StartTime) / 500;

                // TODO: approach circle intersections

                intersections += intersectionBonus;
            }

            return intersections;
        }

        private double checkMovementIntersect(OsuDifficultyHitObject currentObject, OsuDifficultyHitObject nextObject, OsuDifficultyHitObject visibleObject)
        {

            Vector2 startCircle = ((OsuHitObject)currentObject.BaseObject).StackedPosition;
            Vector2 endCircle = ((OsuHitObject)nextObject.BaseObject).StackedPosition;
            Vector2 visibleCircle = ((OsuHitObject)visibleObject.BaseObject).StackedPosition;
            double radius = ((OsuHitObject)currentObject.BaseObject).Radius;

            var numerator = Math.Abs( ((endCircle.X - startCircle.X) * (startCircle.Y - visibleCircle.Y)) - ((startCircle.X - visibleCircle.X) * (endCircle.Y - startCircle.Y)));
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
