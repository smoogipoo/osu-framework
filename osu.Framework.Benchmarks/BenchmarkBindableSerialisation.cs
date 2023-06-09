// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Bindables;
using osu.Framework.IO.Serialization;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkBindableSerialisation
    {
        private readonly System.Text.Json.JsonSerializerOptions options = ISerializableBindable.CreateSerializerOptions();
        private readonly BindableInt bindable = new BindableInt();
        private const string bindable_string = "1337";

        [Benchmark]
        public string SerialiseNewtonsoftJson() => Newtonsoft.Json.JsonConvert.SerializeObject(bindable);

        [Benchmark]
        public object? DeserialiseNewtonsoftJson() => Newtonsoft.Json.JsonConvert.DeserializeObject<BindableInt>(bindable_string);

        [Benchmark]
        public string SerialiseSystemTextJson() => System.Text.Json.JsonSerializer.Serialize(bindable, options);

        [Benchmark]
        public object? DeserialiseSystemTextJson() => System.Text.Json.JsonSerializer.Deserialize<BindableInt>(bindable_string, options);
    }
}
