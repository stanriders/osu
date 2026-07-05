// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Difficulty.Skills
{
    /// <summary>
    /// Skill difficulty calculation class
    /// </summary>
    public interface ISkill
    {
        IReadOnlyList<Mod> Mods { get; init; }

        IReadOnlyList<DifficultyHitObject> DifficultyHitObjects { get; init; }

        ISkillAttributes Process();
    }
}
