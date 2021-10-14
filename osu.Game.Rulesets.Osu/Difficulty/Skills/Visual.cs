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
        {
        }

        private double skillMultiplier => 0.01;
        private double strainDecayBase => 0.15;
        private double currentStrain = 1;

        private double strainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            if (Previous.Count == 0)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;
            var osuHitObject = (OsuHitObject)(osuCurrent.BaseObject);
            var visibleObjects = osuCurrent.visibleObjects;

            var result = 0.1;

            if (Mods.Any(m => m is OsuModHidden))
                result *= 2.0;

            OsuHitObject lastObject = osuHitObject;
            double overlaps = 0;
            double totalDistance = 0;

            foreach(var Object in visibleObjects)
            {
                totalDistance += (Object.Position - lastObject.Position).Length;

                lastObject = Object;
            }

            for (int i = 0; i < visibleObjects.Count - 1; i++)
            {
                for (int j = i + 1; j < visibleObjects.Count; j++)
                {
                    var overlapness = (visibleObjects[i].Position - visibleObjects[j].Position).Length / osuHitObject.Radius;
                    overlaps += 1 - Math.Min(overlapness, 1);
                }
            }

            //Console.WriteLine((osuCurrent.StartTime / 1000.0).ToString() + " " + osuCurrent.NoteDensity.ToString());
            //Console.WriteLine((osuCurrent.StartTime / 1000.0).ToString() + " " + overlaps.ToString());

            result *= Math.Pow((osuCurrent.NoteDensity - 1), 4);

            return result;
        }

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
