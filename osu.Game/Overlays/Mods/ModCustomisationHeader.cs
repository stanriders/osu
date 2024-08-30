// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osuTK;
using osuTK.Graphics;
using static osu.Game.Overlays.Mods.ModCustomisationPanel;

namespace osu.Game.Overlays.Mods
{
    public partial class ModCustomisationHeader : OsuHoverContainer
    {
        private Box background = null!;
        private Box backgroundFlash = null!;
        private SpriteIcon icon = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        protected override IEnumerable<Drawable> EffectTargets => new[] { background };

        public readonly Bindable<ModCustomisationPanelState> ExpandedState = new Bindable<ModCustomisationPanelState>(ModCustomisationPanelState.Collapsed);

        private readonly ModCustomisationPanel panel;

        public ModCustomisationHeader(ModCustomisationPanel panel)
        {
            this.panel = panel;
            Enabled.Value = false;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            CornerRadius = 10f;
            Masking = true;

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                backgroundFlash = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White.Opacity(0.4f),
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = ModSelectOverlayStrings.CustomisationPanelHeader,
                    UseFullGlyphHeight = false,
                    Font = OsuFont.Torus.With(size: 20f, weight: FontWeight.SemiBold),
                    Margin = new MarginPadding { Left = 20f },
                },
                new Container
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Size = new Vector2(16f),
                    Margin = new MarginPadding { Right = 20f },
                    Child = icon = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Icon = FontAwesome.Solid.ChevronDown,
                        RelativeSizeAxes = Axes.Both,
                    }
                }
            };

            IdleColour = colourProvider.Dark3;
            HoverColour = colourProvider.Light4;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Enabled.BindValueChanged(e =>
            {
                TooltipText = e.NewValue
                    ? string.Empty
                    : ModSelectOverlayStrings.CustomisationPanelDisabledReason;

                if (e.NewValue)
                {
                    backgroundFlash.FadeInFromZero(150, Easing.OutQuad).Then()
                                   .FadeOutFromOne(350, Easing.OutQuad);
                }
            }, true);

            ExpandedState.BindValueChanged(v =>
            {
                icon.ScaleTo(v.NewValue > ModCustomisationPanelState.Collapsed ? new Vector2(1, -1) : Vector2.One, 300, Easing.OutQuint);
            }, true);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Enabled.Value)
            {
                ExpandedState.Value = ExpandedState.Value switch
                {
                    ModCustomisationPanelState.Collapsed => ModCustomisationPanelState.Expanded,
                    _ => ModCustomisationPanelState.Collapsed
                };
            }

            return base.OnClick(e);
        }

        private bool touchedThisFrame;

        protected override bool OnTouchDown(TouchDownEvent e)
        {
            if (Enabled.Value)
            {
                touchedThisFrame = true;
                Schedule(() => touchedThisFrame = false);
            }

            return base.OnTouchDown(e);
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (Enabled.Value)
            {
                if (!touchedThisFrame && panel.ExpandedState.Value == ModCustomisationPanelState.Collapsed)
                    panel.ExpandedState.Value = ModCustomisationPanelState.ExpandedByHover;
            }

            return base.OnHover(e);
        }
    }
}
