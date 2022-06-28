// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

#if NET6_0_OR_GREATER
using System.Text.Json;
#endif

using Newtonsoft.Json;
using osu.Framework.Bindables;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// An interface which allows <see cref="Bindable{T}"/> to be json serialized/deserialized.
    /// </summary>
    [JsonConverter(typeof(BindableJsonConverter))]
#if NET6_0_OR_GREATER
    [System.Text.Json.Serialization.JsonConverter(typeof(SystemTextBindableJsonConverter))]
#endif
    public interface ISerializableBindable
    {
        void SerializeTo(JsonWriter writer, JsonSerializer serializer);
        void DeserializeFrom(JsonReader reader, JsonSerializer serializer);

#if NET6_0_OR_GREATER
        void SerializeTo(Utf8JsonWriter writer, JsonSerializerOptions options);
        void DeserializeFrom(ref Utf8JsonReader reader, JsonSerializerOptions options);
#endif
    }
}
