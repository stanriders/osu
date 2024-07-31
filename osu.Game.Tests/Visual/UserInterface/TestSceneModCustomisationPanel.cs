// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Tests.Visual.UserInterface
{
    public partial class TestSceneModCustomisationPanel : OsuManualInputManagerTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private ModCustomisationPanel panel = null!;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(20f),
                Child = panel = new ModCustomisationPanel
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Width = 400f,
                    State = { Value = Visibility.Visible },
                    SelectedMods = { BindTarget = SelectedMods },
                }
            };
        });

        [Test]
        public void TestDisplay()
        {
            AddStep("set DT", () =>
            {
                SelectedMods.Value = new[] { new OsuModDoubleTime() };
                panel.Enabled.Value = panel.Expanded.Value = true;
            });
            AddStep("set DA", () =>
            {
                SelectedMods.Value = new Mod[] { new OsuModDifficultyAdjust() };
                panel.Enabled.Value = panel.Expanded.Value = true;
            });
            AddStep("set FL+WU+DA+AD", () =>
            {
                SelectedMods.Value = new Mod[] { new OsuModFlashlight(), new ModWindUp(), new OsuModDifficultyAdjust(), new OsuModApproachDifferent() };
                panel.Enabled.Value = panel.Expanded.Value = true;
            });
            AddStep("set empty", () =>
            {
                SelectedMods.Value = Array.Empty<Mod>();
                panel.Enabled.Value = panel.Expanded.Value = false;
            });
        }
    }
}
