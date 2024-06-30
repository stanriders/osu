// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class RhythmEvaluator
    {
        private readonly struct Island : IEquatable<Island>
        {
            public Island()
            {
            }

            public List<long> Deltas { get; } = new List<long>();

            public void AddDelta(int delta, double epsilone)
            {
                var existingDelta = Deltas.FirstOrDefault(x => Math.Abs(x - delta) >= epsilone);

                if (existingDelta == default)
                {
                    Deltas.Add(delta);
                }
                else
                {
                    Deltas.Add(existingDelta);
                }
            }

            public double AverageDelta() => Deltas.Average();

            public override int GetHashCode()
            {
                string joinedDeltas = string.Join(string.Empty, Deltas.Select(x => x).ToArray());
                return joinedDeltas.GetHashCode();
            }

            public bool Equals(Island other)
            {
                return other.GetHashCode() == GetHashCode();
            }

            public override bool Equals(object? obj)
            {
                return obj?.GetHashCode() == GetHashCode();
            }
        }

        private const int history_time_max = 5000; // 5 seconds of calculatingRhythmBonus max.
        private static double rhythm_multiplier = 1.12;

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            return EvaluateDifficultyOfLOL(current).Item1;
        }

        /// <summary>
        /// Calculates a rhythm multiplier for the difficulty of the tap associated with historic data of the current <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public static (double, int) EvaluateDifficultyOfLOL(DifficultyHitObject current)
        {
            Dictionary<Island, int> islandCounts = new Dictionary<Island, int>();

            if (current.BaseObject is Spinner)
                return (0, 0);

            rhythm_multiplier = 1.15;

            int previousIslandSize = 0;

            double rhythmComplexitySum = 0;
            int islandSize = 1;
            var island = new Island();
            var previousIsland = new Island();

            double startRatio = 0; // store the ratio of the current start of an island to buff for tighter rhythms

            bool firstDeltaSwitch = false;

            int historicalNoteCount = Math.Min(current.Index, 32);

            int rhythmStart = 0;

            while (rhythmStart < historicalNoteCount - 2 && current.StartTime - current.Previous(rhythmStart).StartTime < history_time_max)
                rhythmStart++;

            OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)current.Previous(rhythmStart);
            OsuDifficultyHitObject lastObj = (OsuDifficultyHitObject)current.Previous(rhythmStart + 1);

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)current.Previous(i - 1);

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                currHistoricalDecay = Math.Min((double)(historicalNoteCount - i) / historicalNoteCount, currHistoricalDecay); // either we're limited by time or limited by object count.

                double currDelta = currObj.StrainTime;
                double prevDelta = prevObj.StrainTime;
                double lastDelta = lastObj.StrainTime;
                double currRatio = 1.0 + 6.0 * Math.Min(0.5, Math.Pow(Math.Sin(Math.PI / (Math.Min(prevDelta, currDelta) / Math.Max(prevDelta, currDelta))), 2)); // fancy function to calculate rhythmbonuses.

                double windowPenalty = Math.Min(1, Math.Max(0, Math.Abs(prevDelta - currDelta) - currObj.HitWindowGreat * 0.3) / (currObj.HitWindowGreat * 0.3));

                windowPenalty = Math.Min(1, windowPenalty);

                double effectiveRatio = windowPenalty * currRatio;

                double deltaDifferenceEpsilone = currObj.HitWindowGreat * 0.2;

                if (firstDeltaSwitch)
                {
                    if (!(Math.Abs(prevDelta - currDelta) > deltaDifferenceEpsilone))
                    {
                        if (islandSize < 7)
                        {
                            island.AddDelta((int)currDelta, deltaDifferenceEpsilone);
                            islandSize++; // island is still progressing, count size.
                        }
                    }
                    else //if (islandSize > 1)
                    {
                        //island.AddDelta((int)currDelta);
                        if (currObj.BaseObject is Slider) // bpm change is into slider, this is easy acc window
                            effectiveRatio *= 0.125;

                        if (prevObj.BaseObject is Slider) // bpm change was from a slider, this is easier typically than circle -> circle
                            effectiveRatio *= 0.25;

                        //if (previousIslandSize == islandSize) // repeated island size (ex: triplet -> triplet)
                        //    effectiveRatio *= 0.25;

                        //if (previousIslandSize % 2 == islandSize % 2) // repeated island polartiy (2 -> 4, 3 -> 5)
                        //    effectiveRatio *= 0.50;

                        if (previousIsland.Deltas.Count % 2 == island.Deltas.Count % 2)
                            effectiveRatio *= 0.50;

                        if (lastDelta > prevDelta + deltaDifferenceEpsilone && prevDelta > currDelta + deltaDifferenceEpsilone) // previous increase happened a note ago, 1/1->1/2-1/4, dont want to buff this.
                            effectiveRatio *= 0.125;

                        if (islandCounts.ContainsKey(island))
                        {
                            islandCounts[island]++;
                            var power = logistic(island.AverageDelta(), 3, 0.15, 9);
                            effectiveRatio *= Math.Pow(1.0 / islandCounts[island], power);//  Math.Sqrt(0.02 * island.AverageDelta()));
                        }
                        else
                        {
                            islandCounts.Add(island, 1);
                        }

                        rhythmComplexitySum += Math.Sqrt(effectiveRatio * startRatio) * currHistoricalDecay;

                        startRatio = effectiveRatio;

                        previousIslandSize = islandSize; // log the last island size.
                        previousIsland = island;

                        if (prevDelta + deltaDifferenceEpsilone < currDelta) // we're slowing down, stop counting
                            firstDeltaSwitch = false; // if we're speeding up, this stays true and  we keep counting island size.

                        islandSize = 1;
                        island = new Island();
                        island.AddDelta((int)currDelta, deltaDifferenceEpsilone);
                    }
                }
                else if (prevDelta > currDelta + deltaDifferenceEpsilone) // we want to be speeding up.
                {
                    // Begin counting island until we change speed again.
                    firstDeltaSwitch = true;
                    startRatio = effectiveRatio;
                    islandSize = 1;
                    island = new Island();
                    island.AddDelta((int)currDelta, deltaDifferenceEpsilone);
                }

                lastObj = prevObj;
                prevObj = currObj;
            }

            return (Math.Sqrt(4 + rhythmComplexitySum * rhythm_multiplier) / 2, islandSize); //produces multiplier that can be applied to strain. range [1, infinity) (not really though)
        }

        private static double logistic(double x, double maxValue, double multiplier, double offset) => (maxValue / (1 + Math.Pow(Math.E, offset - multiplier * x)));
    }
}
