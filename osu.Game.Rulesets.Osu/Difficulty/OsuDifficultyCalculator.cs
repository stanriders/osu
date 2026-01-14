// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        public override int Version => 20250306;

        public OsuDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        public static double CalculateRateAdjustedApproachRate(double approachRate, double clockRate)
        {
            double preempt = IBeatmapDifficultyInfo.DifficultyRange(approachRate, OsuHitObject.PREEMPT_MAX, OsuHitObject.PREEMPT_MID, OsuHitObject.PREEMPT_MIN) / clockRate;
            return IBeatmapDifficultyInfo.InverseDifficultyRange(preempt, OsuHitObject.PREEMPT_MAX, OsuHitObject.PREEMPT_MID, OsuHitObject.PREEMPT_MIN);
        }

        public static double CalculateRateAdjustedOverallDifficulty(double overallDifficulty, double clockRate)
        {
            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(overallDifficulty);

            double hitWindowGreat = hitWindows.WindowFor(HitResult.Great) / clockRate;

            return (79.5 - hitWindowGreat) / 6;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Mods = mods };

            var aim = skills.OfType<Aim>().Single(a => a.IncludeSliders);
            var aimWithoutSliders = skills.OfType<Aim>().Single(a => !a.IncludeSliders);
            var speed = skills.OfType<Speed>().Single();
            var flashlight = skills.OfType<Flashlight>().SingleOrDefault();

            double aimDifficultyValue = aim.DifficultyValue();
            double aimNoSlidersDifficultyValue = aimWithoutSliders.DifficultyValue();
            double speedDifficultyValue = speed.DifficultyValue();

            double speedNotes = speed.RelevantNoteCount();

            double aimDifficultStrainCount = aim.CountTopWeightedStrains(aimDifficultyValue);
            double speedDifficultStrainCount = speed.CountTopWeightedStrains(speedDifficultyValue);

            double aimNoSlidersTopWeightedSliderCount = aimWithoutSliders.CountTopWeightedSliders(aimNoSlidersDifficultyValue);
            double aimNoSlidersDifficultStrainCount = aimWithoutSliders.CountTopWeightedStrains(aimNoSlidersDifficultyValue);

            double aimTopWeightedSliderFactor = aimNoSlidersTopWeightedSliderCount / Math.Max(1, aimNoSlidersDifficultStrainCount - aimNoSlidersTopWeightedSliderCount);

            double speedTopWeightedSliderCount = speed.CountTopWeightedSliders(speedDifficultyValue);
            double speedTopWeightedSliderFactor = speedTopWeightedSliderCount / Math.Max(1, speedDifficultStrainCount - speedTopWeightedSliderCount);

            double difficultSliders = aim.GetDifficultSliders();

            double approachRate = CalculateRateAdjustedApproachRate(beatmap.Difficulty.ApproachRate, clockRate);
            double overallDifficulty = CalculateRateAdjustedOverallDifficulty(beatmap.Difficulty.OverallDifficulty, clockRate);

            int hitCircleCount = beatmap.HitObjects.Count(h => h is HitCircle);
            int sliderCount = beatmap.HitObjects.Count(h => h is Slider);
            int spinnerCount = beatmap.HitObjects.Count(h => h is Spinner);

            int totalHits = beatmap.HitObjects.Count;

            double mechanicalDifficultyRating = calculateMechanicalDifficultyRating(aimDifficultyValue, speedDifficultyValue);
            double sliderFactor = aimDifficultyValue > 0 ? OsuRatingCalculator.CalculateDifficultyRating(aimNoSlidersDifficultyValue) / OsuRatingCalculator.CalculateDifficultyRating(aimDifficultyValue) : 1;

            var osuRatingCalculator = new OsuRatingCalculator(mods, totalHits, approachRate, overallDifficulty, mechanicalDifficultyRating, sliderFactor);

            double aimRating = osuRatingCalculator.ComputeAimRating(aimDifficultyValue);
            double speedRating = osuRatingCalculator.ComputeSpeedRating(speedDifficultyValue);

            double flashlightRating = 0.0;

            if (flashlight is not null)
                flashlightRating = osuRatingCalculator.ComputeFlashlightRating(flashlight.DifficultyValue());

            double sliderNestedScorePerObject = LegacyScoreUtils.CalculateNestedScorePerObject(beatmap, totalHits);
            double legacyScoreBaseMultiplier = LegacyScoreUtils.CalculateDifficultyPeppyStars(beatmap);

            var simulator = new OsuLegacyScoreSimulator();
            var scoreAttributes = simulator.Simulate(WorkingBeatmap, beatmap);

            double baseAimPerformance = OsuStrainSkill.DifficultyToPerformance(aimRating);
            double baseSpeedPerformance = OsuStrainSkill.DifficultyToPerformance(speedRating);
            double baseFlashlightPerformance = Flashlight.DifficultyToPerformance(flashlightRating);

            double basePerformance = DifficultyCalculationUtils.Norm(OsuPerformanceCalculator.PERFORMANCE_NORM_EXPONENT, baseAimPerformance, baseSpeedPerformance, baseFlashlightPerformance);

            double starRating = calculateStarRating(basePerformance);

            OsuDifficultyAttributes attributes = new OsuDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                AimDifficulty = aimRating,
                AimDifficultSliderCount = difficultSliders,
                SpeedDifficulty = speedRating,
                SpeedNoteCount = speedNotes,
                FlashlightDifficulty = flashlightRating,
                SliderFactor = sliderFactor,
                AimDifficultStrainCount = aimDifficultStrainCount,
                SpeedDifficultStrainCount = speedDifficultStrainCount,
                AimTopWeightedSliderFactor = aimTopWeightedSliderFactor,
                SpeedTopWeightedSliderFactor = speedTopWeightedSliderFactor,
                MaxCombo = beatmap.GetMaxCombo(),
                HitCircleCount = hitCircleCount,
                SliderCount = sliderCount,
                SpinnerCount = spinnerCount,
                NestedScorePerObject = sliderNestedScorePerObject,
                LegacyScoreBaseMultiplier = legacyScoreBaseMultiplier,
                MaximumLegacyComboScore = scoreAttributes.ComboScore
            };

            return attributes;
        }

        private double calculateMechanicalDifficultyRating(double aimDifficultyValue, double speedDifficultyValue)
        {
            double aimValue = OsuStrainSkill.DifficultyToPerformance(OsuRatingCalculator.CalculateDifficultyRating(aimDifficultyValue));
            double speedValue = OsuStrainSkill.DifficultyToPerformance(OsuRatingCalculator.CalculateDifficultyRating(speedDifficultyValue));

            double totalValue = DifficultyCalculationUtils.Norm(OsuPerformanceCalculator.PERFORMANCE_NORM_EXPONENT, aimValue, speedValue);

            return calculateStarRating(totalValue);
        }

        private double calculateStarRating(double basePerformance)
        {
            return Math.Cbrt(basePerformance * OsuPerformanceCalculator.PERFORMANCE_BASE_MULTIPLIER);
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            List<DifficultyHitObject> objects = new List<DifficultyHitObject>();

            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (int i = 1; i < beatmap.HitObjects.Count; i++)
            {
                objects.Add(new OsuDifficultyHitObject(beatmap.HitObjects[i], beatmap.HitObjects[i - 1], clockRate, objects, objects.Count));
            }

            populateFlowParams(objects);

            return objects;
        }

        private void populateFlowParams(List<DifficultyHitObject> objects)
        {
            foreach (var obj in objects)
            {
                var difficultyHitObject = (OsuDifficultyHitObject)obj;
                if (difficultyHitObject.Index <= 0)
                    continue;

                //int degrees = 12;
                int previousObjectsCount = 2;
                var previousObjects = new List<OsuDifficultyHitObject>();

                for (int i = previousObjectsCount; i >= 0; i--)
                {
                    var prev = (OsuDifficultyHitObject)difficultyHitObject.Previous(i);
                    if (prev != null)
                        previousObjects.Add(prev);
                }

                int nextObjectsCount = 2;
                var nextObjects = new List<OsuDifficultyHitObject>();

                for (int i = 0; i < nextObjectsCount; i++)
                {
                    var next = (OsuDifficultyHitObject)difficultyHitObject.Next(i);
                    if (next != null)
                        nextObjects.Add(next);
                }

                var baseObject = (OsuHitObject)difficultyHitObject.BaseObject;
                float scalingFactor = OsuDifficultyHitObject.NORMALISED_RADIUS / (float)baseObject.Radius;

                var splineObjectsToConsider = previousObjects.Append(difficultyHitObject)
                                                             .Concat(nextObjects)
                                                             .ToArray();
                var splinePoints = new List<Vector2>();

                for (int i = 0; i < splineObjectsToConsider.Length; i++)
                {
                    var baseObjectSpline = (OsuHitObject)splineObjectsToConsider[i].BaseObject;
                    var objectScaledPosition = baseObjectSpline.StackedPosition * scalingFactor;
                    splinePoints.Add(objectScaledPosition);

                    if (splineObjectsToConsider[i].BaseObject is Slider)
                    {
                        objectScaledPosition = (splineObjectsToConsider[i].LazyEndPosition ?? baseObjectSpline.StackedPosition) * scalingFactor;
                        splinePoints.Add(objectScaledPosition);
                    }
                    /*
                    if (i < splineObjectsToConsider.Length - 1)
                    {
                        var nextBaseObject = (OsuHitObject)splineObjectsToConsider[i + 1].BaseObject;
                        var nextObjectScaledPosition = nextBaseObject.StackedPosition * scalingFactor;

                        splinePoints.Add(new Vector2((objectScaledPosition.X + nextObjectScaledPosition.X) / 2, (objectScaledPosition.Y + nextObjectScaledPosition.Y) / 2));
                    }*/
                }

                difficultyHitObject.FlowPoints = PathApproximator.BezierToPiecewiseLinear(new ReadOnlySpan<Vector2>(splinePoints.ToArray()));

                double snapDistance = previousObjects.Skip(1).Concat(nextObjects).Sum(x => x.LazyJumpDistance);
                float flowDistance = 0f;

                for (int i = 1; i < difficultyHitObject.FlowPoints.Count; i++)
                {
                    var splinePoint = difficultyHitObject.FlowPoints[i];
                    var prevSplinePoint = difficultyHitObject.FlowPoints[i - 1];
                    flowDistance += Vector2.Distance(splinePoint, prevSplinePoint);
                }

                difficultyHitObject.FlowDistance = flowDistance;
                difficultyHitObject.SnapDistance = snapDistance;

                // should super small distance jumps be flow or snap by default?
                if (flowDistance > 1 && snapDistance > 1)
                {
                    difficultyHitObject.IsFlow = Math.Abs(snapDistance - flowDistance) < OsuDifficultyHitObject.NORMALISED_DIAMETER * 1.25;

                    difficultyHitObject.FlowProbability = Math.Pow(
                        Math.Min(snapDistance + OsuDifficultyHitObject.NORMALISED_DIAMETER, flowDistance) / Math.Max(snapDistance, flowDistance),
                        10);
                }
            }
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            var skills = new List<Skill>
            {
                new Aim(mods, true),
                new Aim(mods, false),
                new Speed(mods),
                new flow(mods)
            };

            if (mods.Any(h => h is OsuModFlashlight))
                skills.Add(new Flashlight(mods));

            return skills.ToArray();
        }

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new OsuModTouchDevice(),
            new OsuModDoubleTime(),
            new OsuModHalfTime(),
            new OsuModEasy(),
            new OsuModHardRock(),
            new OsuModFlashlight(),
            new OsuModHidden(),
        };
    }
}
