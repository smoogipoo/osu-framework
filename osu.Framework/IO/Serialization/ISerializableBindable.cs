// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.IO.Serialization
{
    /// <summary>
    /// An interface which allows <see cref="Bindable{T}"/> to be json serialized/deserialized.
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJsonBindableConverter))]
    internal interface ISerializableBindable
    {
        void SerializeTo(Newtonsoft.Json.JsonWriter writer, Newtonsoft.Json.JsonSerializer serializer);
        void DeserializeFrom(Newtonsoft.Json.JsonReader reader, Newtonsoft.Json.JsonSerializer serializer);

        void SerializeTo(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonSerializerOptions options);
        void DeserializeFrom(ref System.Text.Json.Utf8JsonReader reader, System.Text.Json.JsonSerializerOptions options);

        public static System.Text.Json.JsonSerializerOptions CreateSerializerOptions(System.Text.Json.JsonSerializerOptions? options = null)
        {
            options = options != null ? new System.Text.Json.JsonSerializerOptions(options) : new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new SystemTextJsonBindableConverterFactory());
            return options;
        }
    }
}
