// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace osu.Framework.IO.Serialization
{
    internal class SystemTextJsonBindableConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableTo(typeof(ISerializableBindable));
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new Converter();

        private class Converter : JsonConverter<ISerializableBindable>
        {
            public override bool CanConvert(Type typeToConvert) => true;

            public override void Write(Utf8JsonWriter writer, ISerializableBindable value, JsonSerializerOptions options)
                => value.SerializeTo(writer, options);

            public override ISerializableBindable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var bindable = (ISerializableBindable?)Activator.CreateInstance(typeToConvert, true);
                Debug.Assert(bindable != null);

                bindable.DeserializeFrom(ref reader, options);

                return bindable;
            }
        }
    }
}
