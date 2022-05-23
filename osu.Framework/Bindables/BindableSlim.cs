// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Serialization;

namespace osu.Framework.Bindables
{
    public struct BindableSlim<T> : IBindable<T>, IBindable, IParseable, ISerializableBindable
    {
        public event Action<ValueChangedEvent<T>> ValueChanged;
        public event Action<ValueChangedEvent<T>> DefaultChanged;
        public event Action<bool> DisabledChanged;

        private Bindable<T> underlyingBindable;

        private T value;
        private T defaultValue;
        private bool disabled;

        public T Value
        {
            get
            {
                if (underlyingBindable is Bindable<T> bindable)
                    return bindable.Value;

                return value;
            }
            set
            {
                if (underlyingBindable is Bindable<T> bindable)
                    bindable.Value = value;
                else
                {
                    if (Disabled)
                        throw new InvalidOperationException($"Can not set value to \"{value.ToString()}\" as bindable is disabled.");

                    this.value = value;
                }
            }
        }

        public T Default
        {
            get
            {
                if (underlyingBindable is Bindable<T> bindable)
                    return bindable.Default;

                return defaultValue;
            }
            set
            {
                if (underlyingBindable is Bindable<T> bindable)
                    bindable.Default = value;
                else
                {
                    if (Disabled)
                        throw new InvalidOperationException($"Can not set default value to \"{value.ToString()}\" as bindable is disabled.");

                    defaultValue = value;
                }
            }
        }

        public bool Disabled
        {
            get
            {
                if (underlyingBindable is Bindable<T> bindable)
                    return bindable.Disabled;

                return disabled;
            }
            set
            {
                if (underlyingBindable is Bindable<T> bindable)
                    bindable.Disabled = value;
                else
                    disabled = value;
            }
        }

        public bool IsDefault => EqualityComparer<T>.Default.Equals(Value, Default);

        public string Description { get; set; }

        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
        {
            DisabledChanged += onChange;
            if (runOnceImmediately)
                onChange(Disabled);
        }

        public void BindValueChanged(Action<ValueChangedEvent<T>> onChange, bool runOnceImmediately = false)
        {
            ValueChanged += onChange;
            if (runOnceImmediately)
                onChange(new ValueChangedEvent<T>(Value, Value));
        }

        public void UnbindEvents()
        {
            ValueChanged = null;
            DefaultChanged = null;
            DisabledChanged = null;
        }

        public void UnbindBindings() => underlyingBindable?.UnbindBindings();

        public void UnbindAll()
        {
            if (underlyingBindable is Bindable<T> bindable)
                bindable.UnbindAll();
            else
                UnbindEvents();
        }

        public void UnbindFrom(IUnbindable them) => underlyingBindable?.UnbindFrom(them);

        void IBindable<T>.BindTo(IBindable<T> them)
        {
            if (them is BindableSlim<T> slim)
                them = slim.GetOrCreateUnderGetUnderlyingBindable();

            if (!(them is Bindable<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        void IBindable.BindTo(IBindable them)
        {
            if (them is BindableSlim<T> slim)
                them = slim.GetOrCreateUnderGetUnderlyingBindable();

            if (!(them is Bindable<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        public void BindTo(ref BindableSlim<T> them) => GetOrCreateUnderGetUnderlyingBindable().BindTo(ref them);

        /// <summary>
        /// Binds this bindable to another such that bi-directional updates are propagated.
        /// This will adopt any values and value limitations of the bindable bound to.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager).</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to bind to an already bound object.</exception>
        public void BindTo(Bindable<T> them) => GetOrCreateUnderGetUnderlyingBindable().BindTo(them);

        IBindable IBindable.GetBoundCopy() => GetBoundCopy();

        IBindable<T> IBindable<T>.GetBoundCopy() => GetBoundCopy();

        /// <inheritdoc cref="IBindable{T}.GetBoundCopy"/>
        public Bindable<T> GetBoundCopy() => GetOrCreateUnderGetUnderlyingBindable().GetBoundCopy();

        IBindable IBindable.CreateInstance() => new BindableSlim<T>();

        public void Parse(object input)
        {
            Type underlyingType = typeof(T).GetUnderlyingNullableType() ?? typeof(T);

            switch (input)
            {
                case T t:
                    Value = t;
                    break;

                case IBindable _:
                    if (!(input is IBindable<T> bindable))
                        throw new ArgumentException($"Expected bindable of type {nameof(IBindable)}<{typeof(T)}>, got {input.GetType()}", nameof(input));

                    Value = bindable.Value;
                    break;

                default:
                    if (underlyingType.IsEnum)
                        Value = (T)Enum.Parse(underlyingType, input.ToString());
                    else
                        Value = (T)Convert.ChangeType(input, underlyingType, CultureInfo.InvariantCulture);

                    break;
            }
        }

        public void SerializeTo(JsonWriter writer, JsonSerializer serializer)
        {
            serializer.Serialize(writer, Value);
        }

        public void DeserializeFrom(JsonReader reader, JsonSerializer serializer)
        {
            Value = serializer.Deserialize<T>(reader);
        }

        internal Bindable<T> GetOrCreateUnderGetUnderlyingBindable()
        {
            if (underlyingBindable != null)
                return underlyingBindable;

            underlyingBindable = new Bindable<T>
            {
                Value = Value,
                Default = Default,
                Disabled = Disabled,
                Description = Description,
            };

            if (ValueChanged != null)
            {
                foreach (var del in ValueChanged.GetInvocationList().OfType<Action<ValueChangedEvent<T>>>())
                    underlyingBindable.ValueChanged += del;
            }

            if (DefaultChanged != null)
            {
                foreach (var del in DefaultChanged.GetInvocationList().OfType<Action<ValueChangedEvent<T>>>())
                    underlyingBindable.DefaultChanged += del;
            }

            if (DisabledChanged != null)
            {
                foreach (var del in DisabledChanged.GetInvocationList().OfType<Action<bool>>())
                    underlyingBindable.DisabledChanged += del;
            }

            ValueChanged = null;
            DefaultChanged = null;
            DisabledChanged = null;

            return underlyingBindable;
        }
    }
}
