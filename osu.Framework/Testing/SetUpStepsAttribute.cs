// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Testing
{
    /// <summary>
    /// Denotes a method which adds <see cref="TestCase"/> steps.
    /// Invoked via <see cref="TestCase.RunSetUpSteps"/> (which is called from nUnit's [SetUp] or <see cref="TestBrowser.LoadTest"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpStepsAttribute : Attribute
    {
    }
}
