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
using osu.Framework.Input.Focus;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests
{
    // public class TestSceneFocus : FrameworkTestScene
    // {
    //     [Test]
    //     public void OrphanFocus()
    //     {
    //         AddStep("setup", () =>
    //         {
    //             Child = new FocusSystem
    //             {
    //                 RelativeSizeAxes = Axes.Both,
    //                 Child = new FillFlowContainer
    //                 {
    //                     RelativeSizeAxes = Axes.Both,
    //                     Children =
    //                     [
    //                         new FocusableObject(),
    //                         new FocusableObject()
    //                     ]
    //                 }
    //             };
    //         });
    //     }
    //
    //     [Test]
    //     public void NestedFocus()
    //     {
    //         AddStep("setup", () =>
    //         {
    //             Child = new FocusSystem
    //             {
    //                 RelativeSizeAxes = Axes.Both,
    //                 Child = new FillFlowContainer
    //                 {
    //                     RelativeSizeAxes = Axes.Both,
    //                     Children =
    //                     [
    //                         new FocusableObject
    //                         {
    //                             Child = new FocusableObject()
    //                         },
    //                         new FocusableObject
    //                         {
    //                             Children =
    //                             [
    //                                 new FocusableObject(),
    //                                 new FocusableObject
    //                                 {
    //                                     Child = new FocusableObject()
    //                                 }
    //                             ]
    //                         }
    //                     ]
    //                 }
    //             };
    //         });
    //     }
    //
    //     [Test]
    //     public void IndirectFocus()
    //     {
    //         IFocusSystem system = null!;
    //         FocusableObject obj1 = null!;
    //         FocusableObject obj2 = null!;
    //
    //         AddStep("setup", () =>
    //         {
    //             Child = (Drawable)(system = new FocusSystem
    //             {
    //                 RelativeSizeAxes = Axes.Both,
    //                 Child = new FillFlowContainer
    //                 {
    //                     RelativeSizeAxes = Axes.Both,
    //                     Children =
    //                     [
    //                         new FillFlowContainer
    //                         {
    //                             Anchor = Anchor.Centre,
    //                             Origin = Anchor.Centre,
    //                             RelativeSizeAxes = Axes.Both,
    //                             Size = new Vector2(0.5f),
    //                             Children =
    //                             [
    //                                 new FocusableObject(),
    //                                 obj1 = new FocusableObject
    //                                 {
    //                                     Click = () =>
    //                                     {
    //                                         system.AcquireFocus(obj2);
    //                                         return true;
    //                                     }
    //                                 }
    //                             ]
    //                         },
    //                         new FocusEnvironment
    //                         {
    //                             Anchor = Anchor.Centre,
    //                             Origin = Anchor.Centre,
    //                             RelativeSizeAxes = Axes.Both,
    //                             Size = new Vector2(0.5f),
    //                             Child = new FocusableObject
    //                             {
    //                                 Size = Vector2.One,
    //                                 Child = obj2 = new FocusableObject
    //                                 {
    //                                     Click = () =>
    //                                     {
    //                                         system.ReleaseFocus(obj2);
    //                                         return true;
    //                                     }
    //                                 }
    //                             }
    //                         }
    //                     ]
    //                 }
    //             });
    //         });
    //     }
    //
    //
    //
    //
    //
    //     private class FocusableObject : Container, IFocusableObject
    //     {
    //         public Func<bool>? Click;
    //
    //         protected override Container<Drawable> Content { get; }
    //
    //         private readonly Box background;
    //
    //         public FocusableObject()
    //         {
    //             Anchor = Anchor.Centre;
    //             Origin = Anchor.Centre;
    //
    //             RelativeSizeAxes = Axes.Both;
    //             Size = new Vector2(0.5f);
    //
    //             Masking = true;
    //             BorderColour = Color4.Black;
    //             BorderThickness = 2;
    //
    //             InternalChildren =
    //             [
    //                 background = new Box
    //                 {
    //                     RelativeSizeAxes = Axes.Both,
    //                     Colour = Color4.Red
    //                 },
    //                 Content = new FillFlowContainer
    //                 {
    //                     RelativeSizeAxes = Axes.Both
    //                 }
    //             ];
    //         }
    //
    //         public virtual void OnGainedFocus()
    //             => background.Colour = Color4.Orange;
    //
    //         public void OnBecomeFirstResponder()
    //             => background.Colour = Color4.Green;
    //
    //         public void OnResignFirstResponder()
    //             => background.Colour = Color4.Orange;
    //
    //         public virtual void OnLostFocus()
    //             => background.Colour = Color4.Red;
    //
    //         #region Internal
    //
    //         protected sealed override bool OnClick(ClickEvent e)
    //             => this.FindClosestParent<IFocusSystem>()!.OnClick(this);
    //
    //         // This method doesn't actually exist.
    //         bool IFocusableObject.HandleClick()
    //             => Click?.Invoke() ?? true;
    //
    //         #endregion
    //     }
    // }
}
