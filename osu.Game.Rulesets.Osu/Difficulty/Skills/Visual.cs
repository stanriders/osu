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

        private const double rhythm_multiplier = 0.5;
        private const double aim_multiplier = 1.2;

        private const double reading_window_backwards = 500.0;
        private const double reading_window_forwards = 2500.0;

        private double skillMultiplier => 3;
        private double strainDecayBase => 0.0;
        private double currentStrain = 1;

        protected override int HistoryLength => 32;

        private double strainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count < 2)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            var rhythmReadingComplexity = 0.0;
            var aimReadingComplexity = 0.0;

            // the reading window represents the player's focus when processing the screen
            // we include the previous 500ms of objects, and the next 3000ms worth of visual objects
            // previous 500ms: previous objects influence the readibility of current
            // next 3000ms: next objects influence the readibility of current
            List<OsuDifficultyHitObject> readingWindow = new List<OsuDifficultyHitObject>();

            foreach (OsuDifficultyHitObject hitObject in Previous.Reverse())
            {
                if (osuCurrent.StartTime - hitObject.StartTime <= reading_window_backwards)
                    readingWindow.Add(hitObject);
            }

            foreach (OsuDifficultyHitObject hitObject in osuCurrent.visibleObjects)
            {
                if (hitObject.StartTime - osuCurrent.StartTime <= reading_window_forwards)
                    readingWindow.Add(hitObject);
            }

            if (osuCurrent.NoteDensity > 1)
            {
                rhythmReadingComplexity = calculateRhythmReading(readingWindow, (OsuDifficultyHitObject)Previous[0], osuCurrent) * rhythm_multiplier;
                aimReadingComplexity = calculateAimReading(readingWindow, osuCurrent, osuCurrent.visibleObjects[0]) * aim_multiplier;
            }

            // Reading density strain represents the amount of *stuff* on screen.
            // Higher weighting given to objects within the reading window.
            var readingDensityStrain = (readingWindow.Count * 2 + osuCurrent.NoteDensity) / 16;
            readingDensityStrain *= logistic((osuCurrent.JumpDistance - 78) / 26);

            var strain = readingDensityStrain + Math.Pow(rhythmReadingComplexity + aimReadingComplexity, 1.2) * 8.0;

            //if (strain > 0.5)
            //    Console.WriteLine( Math.Round((current.StartTime / 1000.0), 3).ToString() + "  " + Math.Round(strain, 3).ToString() + "  " + Math.Round(readingDensityStrain, 3).ToString() + "   " + Math.Round(rhythmReadingComplexity, 3).ToString() + "  " + Math.Round(aimReadingComplexity, 3).ToString());

            return strain;
        }

        private double calculateRhythmReading(List<OsuDifficultyHitObject> visibleObjects, OsuDifficultyHitObject prevObject, OsuDifficultyHitObject currentObject)
        {

            var overlapnessTotal = 0.0;
            var rhythmChanges = 0.0;

            // calculate how much visible objects overlap the previous
            for (int i = 1; i < visibleObjects.Count; i++)
            {
                var tCurrNext = visibleObjects[i].StrainTime;
                var tPrevCurr = visibleObjects[i - 1].StrainTime;
                var tRatio = Math.Max(tCurrNext / tPrevCurr, tPrevCurr / tCurrNext);
                var constantRhythmNerf = 1 - Math.Max(0, -100 * Math.Pow(tRatio - 1, 2) + 1);

                if (Math.Abs(1 - tRatio) > 0.01)
                    rhythmChanges += 1 * visibleObjects[i].GetVisibilityAtTime(currentObject.StartTime);

                var distanceRatio = visibleObjects[i].JumpDistance / (visibleObjects[i - 1].JumpDistance + 1e-10);
                var changeRatio = distanceRatio * tRatio;
                var spacingChange = Math.Min(1.2, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(1.0, Math.Pow(distanceRatio - 1, 2) * 1000);

                var overlapness = logistic((18 - visibleObjects[i].JumpDistance) / 5) *
                                  constantRhythmNerf;

                overlapness *= spacingChange;
                overlapness *= windowFalloff(currentObject.StartTime, visibleObjects[i].StartTime);

                //if (overlapness > 0.5)
                //    Console.WriteLine(Math.Round(currentObject.StartTime / 1000.0, 3).ToString() + "->" + Math.Round(visibleObjects[i].StartTime / 1000.0, 3).ToString() + " = " + Math.Round(overlapness, 3).ToString());

                overlapnessTotal += Math.Max(0, overlapness);
            }

            //return overlapness * (1 + (rhythmChanges / 4));
            return overlapnessTotal * (1 + (rhythmChanges / 16));
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
                                        nextObject.GetVisibilityAtTime(currentObject.StartTime) *
                                        windowFalloff(currentObject.StartTime, visibleObjects[i].StartTime) *
                                        Math.Abs(visibleObjects[i].StartTime - currentObject.StartTime) / 1000;

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

        private double windowFalloff(double currentTime, double visualTime)
        {
            if (currentTime > visualTime)
                return windowBackwardsFalloff(currentTime, visualTime);
            else if (currentTime < visualTime)
                return windowForwardsFalloff(currentTime, visualTime);

            return 1.0;
        }

        private double windowBackwardsFalloff(double currentTime, double visualTime) => (reading_window_backwards - (currentTime - visualTime)) / reading_window_backwards;

        private double windowForwardsFalloff(double currentTime, double visualTime) => (- Math.Pow(visualTime - currentTime, 3) / Math.Pow(reading_window_forwards, 3)) + 1;

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
