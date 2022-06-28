// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NET6_0_OR_GREATER
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace osu.Framework.IO.Serialization
{
    public class SystemTextBindableJsonConverter : JsonConverter<ISerializableBindable>
    {
        public override bool CanConvert(Type typeToConvert) => typeof(ISerializableBindable).IsAssignableFrom(typeToConvert);

        public override ISerializableBindable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var bindable = (ISerializableBindable)Activator.CreateInstance(typeToConvert, true)!;
            bindable.DeserializeFrom(ref reader, options);
            return bindable;
        }

        public override void Write(Utf8JsonWriter writer, ISerializableBindable value, JsonSerializerOptions options)
        {
            value.SerializeTo(writer, options);
        }
    }
}

#endif
