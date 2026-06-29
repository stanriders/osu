// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Difficulty.Skills
{
    /// <summary>
    /// A bare minimal abstract skill for fully custom skill implementations.
    /// </summary>
    /// <remarks>
    /// This class should be considered a "processing" class and not persisted.
    /// </remarks>
    public abstract class Skill
    {
        /// <summary>
        /// Mods for use in skill calculations.
        /// </summary>
        protected IReadOnlyList<Mod> Mods => mods;

        protected IReadOnlyList<DifficultyHitObject> DifficultyHitObjects => difficultyHitObjects;

        private readonly Mod[] mods;
        private readonly DifficultyHitObject[] difficultyHitObjects;

        protected Skill(Mod[] mods, DifficultyHitObject[] difficultyHitObjects)
        {
            this.mods = mods;
            this.difficultyHitObjects = difficultyHitObjects;
        }

        public abstract SkillAttributes Process();
    }
}
