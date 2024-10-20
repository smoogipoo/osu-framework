// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
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
                            new FocusEnvironment
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Child = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
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
                                }
                            },
                            new FocusEnvironment
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Child = obj2 = new FocusableObject
                                {
                                    Size = Vector2.One,
                                    Click = () =>
                                    {
                                        system.ReleaseFocus(obj2);
                                        return true;
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
            /// The focused drawable within this environment.
            /// </summary>
            IFocusableObject? CurrentFocus { get; }

            void InternalChangeFocus(IFocusableObject? target);
        }

        /// <summary>
        /// Manages focus and captures the first input responder.
        /// </summary>
        private interface IFocusSystem : IFocusEnvironment
        {
            /// <summary>
            /// The drawable that shall be the first target for keyboard input.
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
            /// Notifies that this drawable shall be the first target for keyboard input.
            /// </summary>
            void OnBecomeFirstResponder();

            /// <summary>
            /// Notifies that this drawable shall no longer be the first target for keyboard input.
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

            void IFocusEnvironment.InternalChangeFocus(IFocusableObject? target)
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
            private readonly Stack<IFocusEnvironment> focusEnvironments = new Stack<IFocusEnvironment>();
            private readonly List<FocusRequest> pendingFocusRequests = new List<FocusRequest>();
            private bool isProcessingRequests;

            public IFocusableObject? FirstResponder
                => currentFocusEnvironment?.CurrentFocus;

            private IFocusEnvironment? currentFocusEnvironment
                => focusEnvironments.TryPeek(out IFocusEnvironment? env) ? env : null;

            public void AcquireFocus(IFocusableObject target)
            {
                pendingFocusRequests.Add(new FocusRequest(target, true));
                processRequests();
            }

            public void ReleaseFocus(IFocusableObject target)
            {
                pendingFocusRequests.Add(new FocusRequest(target, false));
                processRequests();
            }

            protected override bool OnClick(ClickEvent e)
            {
                using (suspendFirstResponder())
                    restoreEnvironment(null);
                return true;
            }

            bool IFocusSystem.OnClick(IFocusableObject target)
            {
                try
                {
                    // It could be the case that, while processing the pending requests, one of them caused an indirect click that re-entered this method.
                    // When this occurs, we need to save the current pending requests to acquire focus at the correct point in time.
                    int requestCount = pendingFocusRequests.Count;

                    if (!target.HandleClick())
                        return false;

                    pendingFocusRequests.Insert(requestCount, new FocusRequest(target, true));
                    return true;
                }
                finally
                {
                    processRequests();
                }
            }

            private void processRequests()
            {
                if (isProcessingRequests)
                    return;

                isProcessingRequests = true;

                try
                {
                    // Note: Do not make this into a foreach - it can be mutated if the user changes focus during one of the event handlers.
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (int i = 0; i < pendingFocusRequests.Count; i++)
                    {
                        FocusRequest request = pendingFocusRequests[i];
                        IFocusEnvironment environment = request.Target.FindClosestParent<IFocusEnvironment>()!;

                        if (request.Acquire)
                        {
                            if (currentFocusEnvironment == environment && FirstResponder == request.Target)
                                return;

                            using (suspendFirstResponder())
                            {
                                restoreEnvironment(environment);
                                environment.InternalChangeFocus(request.Target);
                            }
                        }
                        else
                        {
                            if (currentFocusEnvironment != environment || FirstResponder != request.Target)
                                return;

                            using (suspendFirstResponder())
                            {
                                environment.InternalChangeFocus(null);
                                focusEnvironments.Pop();
                            }
                        }
                    }

                    pendingFocusRequests.Clear();
                }
                finally
                {
                    isProcessingRequests = false;
                }
            }

            private void restoreEnvironment(IFocusEnvironment? environment)
            {
                if (environment == null || focusEnvironments.Contains(environment))
                {
                    while (currentFocusEnvironment != environment)
                    {
                        currentFocusEnvironment!.InternalChangeFocus(null);
                        focusEnvironments.Pop();
                    }
                }
                else
                    focusEnvironments.Push(environment);
            }

            private ValueInvokeOnDisposal<FocusSystem> suspendFirstResponder()
            {
                FirstResponder?.OnResignFirstResponder();
                return new ValueInvokeOnDisposal<FocusSystem>(this, static s => s.FirstResponder?.OnBecomeFirstResponder());
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
