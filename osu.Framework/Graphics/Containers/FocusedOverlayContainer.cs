// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.Focus;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An overlay container that eagerly holds keyboard focus.
    /// </summary>
    public abstract partial class FocusedOverlayContainer : OverlayContainer, IFocusEnvironment
    {
        public override bool RequestsFocus => State.Value == Visibility.Visible;

        public override bool AcceptsFocus => State.Value == Visibility.Visible;

        public Drawable? CurrentFocus { get; private set; }

        protected override void UpdateState(ValueChangedEvent<Visibility> state)
        {
            base.UpdateState(state);

            switch (state.NewValue)
            {
                case Visibility.Visible:
                    GetContainingFocusSystem()?.AcquireFocus(this);
                    break;

                case Visibility.Hidden:
                    if (CurrentFocus != null)
                        GetContainingFocusSystem()?.ResignFocus(CurrentFocus);
                    break;
            }
        }

        Drawable? IFocusEnvironment.CurrentFocus
        {
            get => CurrentFocus;
            set => CurrentFocus = value;
        }
    }
}
