// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays.Profile.Header;
using osu.Game.Overlays.Profile.Header.Components;
using osu.Game.Resources.Localisation.Web;
using osuTK;

namespace osu.Game.Overlays.Profile
{
    public partial class ProfileHeader : TabControlOverlayHeader<LocalisableString>
    {
        public Bindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        private CentreHeaderContainer centreHeaderContainer;
        private DetailHeaderContainer detailHeaderContainer;

        private TopHeaderContainer topHeaderContainer = null!;

        private FillFlowContainer userInfoContainer;
        private FillFlowContainer userNotFoundContainer;

        public ProfileHeader()
        {
            ContentSidePadding = WaveOverlayContainer.HORIZONTAL_PADDING;

            TabControl.AddItem(LayoutStrings.HeaderUsersShow);

            // todo: pending implementation.
            // TabControl.AddItem(LayoutStrings.HeaderUsersModding);

            // Haphazardly guaranteed by OverlayHeader constructor (see CreateBackground / CreateContent).
            Debug.Assert(centreHeaderContainer != null);
            Debug.Assert(detailHeaderContainer != null);
            Debug.Assert(userInfoContainer != null);
            Debug.Assert(userNotFoundContainer != null);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            User.BindValueChanged(x =>
            {
                if (x.NewValue != null)
                {
                    userNotFoundContainer.Hide();
                    TabControlContainer.Show();
                    userInfoContainer.Show();
                }
            });
        }

        public void ShowUserNotFound()
        {
            userNotFoundContainer.Show();
            TabControlContainer.Hide();
            userInfoContainer.Hide();
        }

        protected override Drawable CreateBackground() => Empty();

        protected override Drawable CreateContent() => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                userInfoContainer = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        topHeaderContainer = new TopHeaderContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            User = { BindTarget = User },
                        },
                        new BannerHeaderContainer
                        {
                            User = { BindTarget = User },
                        },
                        new BadgeHeaderContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            User = { BindTarget = User },
                        },
                        detailHeaderContainer = new DetailHeaderContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            User = { BindTarget = User },
                        },
                        new ProfileProcessingNotice(),
                        centreHeaderContainer = new CentreHeaderContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            User = { BindTarget = User },
                        },
                        new BottomHeaderContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            User = { BindTarget = User },
                        },
                    }
                },
                userNotFoundContainer = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding { Vertical = 20, Horizontal = 50 },
                    Spacing = new Vector2(10),
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Font = FontUsage.Default.With(size: 30),
                            Text = UsersStrings.ShowNotFoundTitle
                        },
                        new OsuSpriteText
                        {
                            Font = FontUsage.Default,
                            Text = UsersStrings.ShowNotFoundReason2 // other reasons only make sense in the context of web
                        }
                    }
                }
            }
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // This is basically a tooltip display on hover, so we should display above everything.
            // If this ever breaks let's just trash the design and make it a standard tooltip.
            AddInternal(topHeaderContainer.PreviousUsernamesDisplay.CreateProxy());
        }

        protected override OverlayTitle CreateTitle() => new ProfileHeaderTitle();

        protected override Drawable CreateTabControlContent() => new ProfileRulesetSelector
        {
            User = { BindTarget = User }
        };

        private partial class ProfileHeaderTitle : OverlayTitle
        {
            public ProfileHeaderTitle()
            {
                Title = PageTitleStrings.MainUsersControllerDefault;
                Icon = OsuIcon.Player;
            }
        }
    }
}
