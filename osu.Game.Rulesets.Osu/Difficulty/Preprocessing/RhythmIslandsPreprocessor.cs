// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing
{
    public static class RhythmIslandsPreprocessor
    {
        public static void PopulateIslands(List<DifficultyHitObject> difficultyHitObjects)
        {
            var islands = new List<Island>();

            var currIsland = new Island(int.MaxValue);

            foreach (var currObj in difficultyHitObjects)
            {
                var osuCurrObj = (OsuDifficultyHitObject)currObj;
                var osuPrevObj = (OsuDifficultyHitObject?)currObj.Previous();

                if (osuPrevObj == null)
                    continue;

                // Use custom cap value to ensure that at this point delta time is actually zero
                const double delta_min_value = 1e-7;

                double currDelta = Math.Max(osuCurrObj.DeltaTime, delta_min_value);
                double prevDelta = Math.Max(osuPrevObj.DeltaTime, delta_min_value);

                double deltaDifference = Math.Abs(prevDelta - currDelta);
                double deltaDifferenceEpsilon = osuCurrObj.HitWindowGreat * 0.3;

                if (currIsland.Delta == int.MaxValue)
                {
                    currIsland = new Island((int)currDelta)
                    {
                        PreviousIsland = islands.LastOrDefault()
                    };
                    islands.Add(currIsland);
                }

                if (deltaDifference <= deltaDifferenceEpsilon)
                {
                    // island is still progressing
                    currIsland.AddDelta((int)currDelta);
                }

                if (deltaDifference > deltaDifferenceEpsilon)
                {
                    currIsland = new Island((int)currDelta)
                    {
                        PreviousIsland = islands.LastOrDefault()
                    };
                    islands.Add(currIsland);
                }

                osuCurrObj.Island = currIsland;
            }
        }
    }
}
