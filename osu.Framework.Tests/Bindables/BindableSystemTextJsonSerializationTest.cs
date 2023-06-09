// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.IO.Serialization;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableSystemTextJsonSerializationTest
    {
        private readonly JsonSerializerOptions options = ISerializableBindable.CreateSerializerOptions();

        [Test]
        public void TestInt()
        {
            var toSerialize = new Bindable<int> { Value = 1337 };

            var deserialized = JsonSerializer.Deserialize<Bindable<int>>(JsonSerializer.Serialize(toSerialize, options), options);

            Assert.AreEqual(toSerialize.Value, deserialized?.Value);
        }

        [Test]
        public void TestIntFromDerivedType()
        {
            var toSerialize = new BindableInt { Value = 1337 };

            var deserialized = JsonSerializer.Deserialize<Bindable<int>>(JsonSerializer.Serialize(toSerialize, options), options);

            Assert.AreEqual(toSerialize.Value, deserialized?.Value);
        }

        [Test]
        public void TestDouble()
        {
            var toSerialize = new BindableDouble { Value = 1337.0 };

            var deserialized = JsonSerializer.Deserialize<Bindable<double>>(JsonSerializer.Serialize(toSerialize, options), options);

            Assert.AreEqual(toSerialize.Value, deserialized?.Value);
        }

        [Test]
        public void TestString()
        {
            var toSerialize = new Bindable<string> { Value = "1337" };

            var deserialized = JsonSerializer.Deserialize<Bindable<string>>(JsonSerializer.Serialize(toSerialize, options), options);

            Assert.AreEqual(toSerialize.Value, deserialized?.Value);
        }

        [Test]
        public void TestClassWithInitialisationFromCtorArgs()
        {
            var toSerialize = new CustomObjWithCtorInit
            {
                Bindable1 = { Value = 5 }
            };

            var deserialized = JsonSerializer.Deserialize<CustomObjWithCtorInit>(JsonSerializer.Serialize(toSerialize, options), options);

            Assert.AreEqual(toSerialize.Bindable1.Value, deserialized?.Bindable1.Value);
        }

        [Test]
        public void TestIntWithBounds()
        {
            var toSerialize = new CustomObj2
            {
                Bindable =
                {
                    MaxValue = int.MaxValue,
                    Value = 1337,
                }
            };

            var deserialized = JsonSerializer.Deserialize<CustomObj2>(JsonSerializer.Serialize(toSerialize, options), options);

            Assert.AreEqual(deserialized?.Bindable.MaxValue, deserialized?.Bindable.Value);
        }

        [Test]
        public void TestMultipleBindables()
        {
            var toSerialize = new CustomObj
            {
                Bindable1 = { Value = 1337 },
                Bindable2 = { Value = 1338 },
            };

            var deserialized = JsonSerializer.Deserialize<CustomObj>(JsonSerializer.Serialize(toSerialize, options), options)!;

            Assert.NotNull(deserialized);
            Assert.AreEqual(toSerialize.Bindable1.Value, deserialized.Bindable1.Value);
            Assert.AreEqual(toSerialize.Bindable2.Value, deserialized.Bindable2.Value);
        }

        [Test]
        public void TestComplexGeneric()
        {
            var toSerialize = new Bindable<CustomObj>
            {
                Value = new CustomObj
                {
                    Bindable1 = { Value = 1337 },
                    Bindable2 = { Value = 1338 },
                }
            };

            var deserialized = JsonSerializer.Deserialize<Bindable<CustomObj>>(JsonSerializer.Serialize(toSerialize, options), options)!;

            Assert.NotNull(deserialized);
            Assert.AreEqual(toSerialize.Value.Bindable1.Value, deserialized.Value.Bindable1.Value);
            Assert.AreEqual(toSerialize.Value.Bindable2.Value, deserialized.Value.Bindable2.Value);
        }

        private class CustomObjWithCtorInit
        {
            public Bindable<int> Bindable1 { get; set; } = new Bindable<int>();

            public CustomObjWithCtorInit(int value = 0)
            {
                Bindable1.Value = value;
            }

            public CustomObjWithCtorInit()
            {
            }
        }

        private class CustomObj
        {
            public Bindable<int> Bindable1 { get; set; } = new Bindable<int>();
            public Bindable<int> Bindable2 { get; set; } = new Bindable<int>();
        }

        private class CustomObj2 : IJsonOnDeserialized
        {
            public BindableInt Bindable { get; set; } = new BindableInt { MaxValue = 100 };

            public void OnDeserialized()
            {
                Bindable.MaxValue = 100;
            }
        }
    }
}
