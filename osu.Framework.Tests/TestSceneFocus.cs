// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests
{
    public class TestSceneFocus : FrameworkTestScene
    {
        [Test]
        public void OrphanFocus()
        {
            AddStep("setup", () =>
            {
                Child = new FocusSystem
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children =
                        [
                            new FocusableObject(),
                            new FocusableObject()
                        ]
                    }
                };
            });
        }

        [Test]
        public void NestedFocus()
        {
            AddStep("setup", () =>
            {
                Child = new FocusSystem
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children =
                        [
                            new FocusableObject
                            {
                                Child = new FocusableObject()
                            },
                            new FocusableObject
                            {
                                Children =
                                [
                                    new FocusableObject(),
                                    new FocusableObject
                                    {
                                        Child = new FocusableObject()
                                    }
                                ]
                            }
                        ]
                    }
                };
            });
        }

        [Test]
        public void IndirectFocus()
        {
            IFocusSystem system = null!;
            FocusableObject obj1 = null!;
            FocusableObject obj2 = null!;

            AddStep("setup", () =>
            {
                Child = (Drawable)(system = new FocusSystem
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children =
                        [
                            new FillFlowContainer
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children =
                                [
                                    new FocusableObject(),
                                    obj1 = new FocusableObject
                                    {
                                        Click = () =>
                                        {
                                            system.AcquireFocus(obj2);
                                            return true;
                                        }
                                    }
                                ]
                            },
                            new FocusEnvironment
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Child = new FocusableObject
                                {
                                    Size = Vector2.One,
                                    Child = obj2 = new FocusableObject
                                    {
                                        Click = () =>
                                        {
                                            system.ReleaseFocus(obj2);
                                            return true;
                                        }
                                    }
                                }
                            }
                        ]
                    }
                });
            });
        }

        /// <summary>
        /// Captures the focus state of a hierarchy.
        /// </summary>
        private interface IFocusEnvironment : IDrawable
        {
            /// <summary>
            /// The immediate focused drawable within this environment.
            /// </summary>
            IFocusableObject? CurrentFocus { get; }

            void ChangeFocus(IFocusableObject? target);
        }

        /// <summary>
        /// Manages focus and captures the first input responder.
        /// </summary>
        private interface IFocusSystem : IFocusEnvironment
        {
            /// <summary>
            /// The drawable that will be the first target for keyboard input.
            /// </summary>
            IFocusableObject? FirstResponder { get; }

            /// <summary>
            /// Requests a drawable to be focused.
            /// </summary>
            /// <param name="target">The drawable.</param>
            void AcquireFocus(IFocusableObject target);

            /// <summary>
            /// Requests focus to be removed from a given drawable.
            /// </summary>
            /// <param name="target">The drawable.</param>
            void ReleaseFocus(IFocusableObject target);

            #region Internal

            // NB: This method doesn't exist! It short-circuits input handling to mimic InputManager.
            bool OnClick(IFocusableObject target);

            #endregion
        }

        /// <summary>
        /// Represents a drawable in the focus system.
        /// </summary>
        private interface IFocusableObject : IDrawable
        {
            /// <summary>
            /// Notifies that this drawable will be the first target for keyboard input.
            /// </summary>
            void OnBecomeFirstResponder();

            /// <summary>
            /// Notifies that this drawable will no longer be the first target for keyboard input.
            /// </summary>
            void OnResignFirstResponder();

            #region Internal

            // NB: This method doesn't exist! It's a local implementation of Drawable.OnFocus().
            void OnGainedFocus();

            // NB: This method doesn't exist! It's a local implementation of Drawable.OnFocusLost().
            void OnLostFocus();

            // NB: This method doesn't exist! It short-circuits input handling ot mimic InputManager.
            bool HandleClick();

            #endregion
        }

        private class FocusEnvironment : Container, IFocusEnvironment
        {
            public IFocusableObject? CurrentFocus { get; private set; }

            void IFocusEnvironment.ChangeFocus(IFocusableObject? target)
            {
                if (CurrentFocus == target)
                    return;

                HashSet<IFocusableObject> newFocusSet = [..buildFocusSet(target)];
                HashSet<IFocusableObject> oldFocusSet = [..buildFocusSet(CurrentFocus)];

                foreach (var d in buildFocusSet(CurrentFocus).Except(newFocusSet))
                    d.OnLostFocus();

                foreach (var d in buildFocusSet(target).Reverse().Except(oldFocusSet))
                    d.OnGainedFocus();

                CurrentFocus = target;
            }

            private static IEnumerable<IFocusableObject> buildFocusSet(IFocusableObject? target)
            {
                IDrawable? d = target;

                while (d != null && d is not IFocusEnvironment)
                {
                    if (d is IFocusableObject obj)
                        yield return obj;

                    d = d.Parent;
                }
            }
        }

        private class FocusSystem : FocusEnvironment, IFocusSystem
        {
            public IFocusableObject? FirstResponder { get; private set; }

            private readonly Queue<FocusRequest> pendingRequests = new Queue<FocusRequest>();
            private bool isDeferringRequests;

            protected override bool OnClick(ClickEvent e)
            {
                while (FirstResponder != null)
                    ReleaseFocus(FirstResponder);
                return false;
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            public void AcquireFocus(IFocusableObject target)
                => handleRequest(new FocusRequest(target, true));

            public void ReleaseFocus(IFocusableObject target)
                => handleRequest(new FocusRequest(target, false));

            bool IFocusSystem.OnClick(IFocusableObject target)
            {
                isDeferringRequests = true;

                bool acquire = false;

                try
                {
                    if (!target.HandleClick())
                        return false;

                    acquire = true;
                    return true;
                }
                finally
                {
                    isDeferringRequests = false;

                    if (acquire)
                        handleRequest(new FocusRequest(target, acquire));

                    while (pendingRequests.TryDequeue(out FocusRequest req))
                        handleRequest(req);
                }
            }

            private void handleRequest(FocusRequest request)
            {
                if (isDeferringRequests)
                {
                    pendingRequests.Enqueue(request);
                    return;
                }

                IFocusEnvironment environment = request.Target.FindClosestParent<IFocusEnvironment>()!;

                if (request.Acquire)
                {
                    if (FirstResponder != request.Target)
                    {
                        FirstResponder?.OnResignFirstResponder();
                        FirstResponder = null;
                    }

                    environment.ChangeFocus(request.Target);

                    if (FirstResponder == null)
                    {
                        FirstResponder = request.Target;
                        FirstResponder.OnBecomeFirstResponder();
                    }
                }
                else
                {
                    if (environment.CurrentFocus != request.Target)
                        return;

                    if (FirstResponder == request.Target)
                    {
                        FirstResponder?.OnResignFirstResponder();
                        FirstResponder = null;
                    }

                    environment.ChangeFocus(null);

                    if (FirstResponder == null)
                    {
                        FirstResponder = this.ChildrenOfType<IFocusEnvironment>().Select(e => e.CurrentFocus).FirstOrDefault(d => d != null);
                        FirstResponder?.OnBecomeFirstResponder();
                    }
                }
            }

            private readonly record struct FocusRequest(IFocusableObject Target, bool Acquire);
        }

        private class FocusableObject : Container, IFocusableObject
        {
            public Func<bool>? Click;

            protected override Container<Drawable> Content { get; }

            private readonly Box background;

            public FocusableObject()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                RelativeSizeAxes = Axes.Both;
                Size = new Vector2(0.5f);

                Masking = true;
                BorderColour = Color4.Black;
                BorderThickness = 2;

                InternalChildren =
                [
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Red
                    },
                    Content = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both
                    }
                ];
            }

            public virtual void OnGainedFocus()
                => background.Colour = Color4.Orange;

            public void OnBecomeFirstResponder()
                => background.Colour = Color4.Green;

            public void OnResignFirstResponder()
                => background.Colour = Color4.Orange;

            public virtual void OnLostFocus()
                => background.Colour = Color4.Red;

            #region Internal

            protected sealed override bool OnClick(ClickEvent e)
                => this.FindClosestParent<IFocusSystem>()!.OnClick(this);

            // This method doesn't actually exist.
            bool IFocusableObject.HandleClick()
                => Click?.Invoke() ?? true;

            #endregion
        }
    }
}
