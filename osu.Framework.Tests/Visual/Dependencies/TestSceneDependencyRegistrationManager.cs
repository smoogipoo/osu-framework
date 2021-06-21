// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Dependencies
{
    public class TestSceneDependencyRegistrationManager : TestScene
    {
        public TestSceneDependencyRegistrationManager()
        {
            Child = new ClassC();
        }
    }

    public static class ClassDependencyActivatorResolver
    {
        private static readonly ConcurrentDictionary<Type, IDependencyActivator> activators = new ConcurrentDictionary<Type, IDependencyActivator>();

        public static void Register(Type type, IDependencyActivator activator) => activators[type] = activator;

        public static void Activate(object receiver, IReadOnlyDependencyContainer dependencyContainer)
        {
            foreach (var type in receiver.GetType().EnumerateBaseTypes().Reverse())
            {
                if (activators.TryGetValue(type, out var activator))
                    activator.Activate(receiver, dependencyContainer);
            }
        }
    }

    public interface IDependencyActivator
    {
        void Activate(object receiver, IReadOnlyDependencyContainer dependencyContainer);
    }

    // File: ClassA.cs
    public partial class ClassA : Drawable
    {
        [Resolved]
        private GameHost host { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
        }
    }

    // File: __ClassA.deps.g.cs
    public partial class ClassA
    {
        private static readonly __ClassA_g_DependencyActivator dependency_activator = new __ClassA_g_DependencyActivator();

        // ReSharper disable once InconsistentNaming
        private class __ClassA_g_DependencyActivator : IDependencyActivator
        {
            public __ClassA_g_DependencyActivator()
            {
                ClassDependencyActivatorResolver.Register(typeof(ClassA), this);
            }

            public void Activate(object receiver, IReadOnlyDependencyContainer dependencyContainer)
            {
                ((ClassA)receiver).host = dependencyContainer.Get<GameHost>();
                ((ClassA)receiver).load();
            }
        }
    }

    // File: ClassB.cs
    public partial class ClassB : Drawable
    {
        [Resolved]
        private GameHost host { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
        }
    }

    // File: __ClassB.deps.g.cs
    public partial class ClassB : IGeneratedDependencyInjector<ClassB>
    {
        void IGeneratedDependencyInjector<ClassB>.Inject(IReadOnlyDependencyContainer dependencyContainer)
        {
            host = dependencyContainer.Get<GameHost>();
            load();
        }
    }

    // File: ClassC.cs
    public partial class ClassC : ClassB
    {
        [BackgroundDependencyLoader]
        private void load()
        {
        }
    }

    // File: __ClassC.deps.g.cs
    public partial class ClassC : IGeneratedDependencyInjector<ClassC>
    {
        void IGeneratedDependencyInjector<ClassC>.Inject(IReadOnlyDependencyContainer dependencyContainer)
        {
            dependencyContainer.GetValue<CancellationToken?>();
            load();
        }
    }
}
