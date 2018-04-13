﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLocalisation : TestCase
    {
        private DependencyContainer dependencies;
        protected override IReadOnlyDependencyContainer CreateLocalDependencies(IReadOnlyDependencyContainer parent) => dependencies = new DependencyContainer(parent);

        private readonly LocalisationEngine engine;
        private readonly FrameworkConfigManager config;
        private SpriteText sprite;

        public TestCaseLocalisation()
        {
            config = new FakeFrameworkConfigManager();
            engine = new LocalisationEngine(config);

            engine.AddLanguage("en", new FakeStorage());
            engine.AddLanguage("zh-CHS", new FakeStorage());
            engine.AddLanguage("ja", new FakeStorage());

            const string basic_text = "not localisable (for now)";
            AddStep("make not localisable", () => sprite.Text = basic_text);
            AddAssert("text correct", () => sprite.Text == basic_text);

            // this should never actually be done (recreate the LocalisableString when changing 2+ properties)
            // this just makes sure nothing crashes even if you do
            AddStep("change existing", () =>
            {
                sprite.LocalisableText.Text.Value = "new {0} {1}";
                sprite.LocalisableText.Type.Value = LocalisationType.All;
                sprite.LocalisableText.Args.Value = new object[] { "string value! Time:", DateTime.Now };
            });
        }

        [Test]
        [SetCulture("en")]
        public void TestLocalised()
        {
            AddStep("make localisable", () =>
            {
                sprite.LocalisableText = new LocalisableString("localisable", LocalisationType.Localised);
            });
            changeLanguage("english", "en");
            AddAssert("text localised", () => sprite.Text == "localisable in English");
        }

        [Test]
        [SetCulture("ja")]
        public void TestFormatted()
        {
            var formattedDate = DateTime.Now;
            AddStep("make formattable", () =>
            {
                sprite.LocalisableText = new LocalisableString("{0}", LocalisationType.Formatted, args: formattedDate);
            });
            changeLanguage("japanese", "ja");
            AddAssert("text formatted correctly", () => sprite.Text == formattedDate.ToString(new CultureInfo("ja")));

            const string formattable_string = "{0}";
            AddStep("fail formatting on purpose", () =>
            {
                // no args for formatting, this will throw internally but should not crash / fail the test
                sprite.LocalisableText = new LocalisableString(formattable_string, LocalisationType.Formatted);
            });
            AddAssert("text reverted", () => sprite.Text == formattable_string);
        }

        [Test]
        [SetCulture("zh-CHS")]
        public void TestFormattedLocalised()
        {
            AddStep("Make localisable & formattable", () =>
            {
                sprite.LocalisableText = new LocalisableString("localisableformat", LocalisationType.Localised | LocalisationType.Formatted, args: "formatted");
            });
            changeLanguage("simplified chinese", "zh-CHS");
            AddAssert("text localised & formatted", () => sprite.Text == "formatted in locale zh-CHS");
        }

        [Test]
        public void TestUnicodePreference()
        {
            AddStep("create unicode preference & 'localise'", () =>
            {
                // LocalisationType is 'too much' on purpose here - formatting shouldnt break anything
                sprite.LocalisableText = new LocalisableString("yes Unicode", LocalisationType.All, "no Unicode");
            });
            AddStep("activate unicode", () => config.Set(FrameworkSetting.ShowUnicode, true));
            AddAssert("text is unicode", () => sprite.Text == "Unicode localised!");
            AddStep("deactivate unicode", () => config.Set(FrameworkSetting.ShowUnicode, false));
            AddAssert("text is not unicode", () => sprite.Text == "non-Unicode localised!");

            AddStep("set non-unicode to empty", () => sprite.LocalisableText.NonUnicode.Value = string.Empty);
            AddAssert("text disappeared", () => sprite.Text == string.Empty);
            AddStep("set non-unicode to null", () => sprite.LocalisableText.NonUnicode.Value = null);
            AddAssert("text reverted", () => sprite.Text == "Unicode localised!");
        }

        private void changeLanguage(string language, string locale)
        {
            AddStep($"language: {language}", () => config.Set(FrameworkSetting.Locale, locale));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            dependencies.Cache(engine);

            Add(new FillFlowContainer<SpriteText>
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Padding = new MarginPadding(10),
                AutoSizeAxes = Axes.Both,
                Children = new[]
                {
                    sprite = new SpriteText
                    {
                        Text = "not localisable (for now)",
                        TextSize = 48,
                        Colour = Color4.White
                    }
                }
            });
        }

        private class FakeFrameworkConfigManager : FrameworkConfigManager
        {
            protected override string Filename => null;

            public FakeFrameworkConfigManager() : base(null) { }

            protected override void InitialiseDefaults()
            {
                Set(FrameworkSetting.Locale, "");
                Set(FrameworkSetting.ShowUnicode, false);
            }
        }

        private class FakeStorage : IResourceStore<string>
        {
            public string Get(string name)
            {
                switch (name)
                {
                    case "localisable":
                        return $"{name} in {CultureInfo.CurrentCulture.EnglishName}";
                    case "localisableformat":
                        return $"{{0}} in locale {CultureInfo.CurrentCulture.Name}";
                    case "no Unicode":
                        return "non-Unicode localised!";
                    case "yes Unicode":
                        return "Unicode localised!";
                    default:
                        return name;
                }
            }
            public Stream GetStream(string name) => throw new NotSupportedException();
        }
    }
}
