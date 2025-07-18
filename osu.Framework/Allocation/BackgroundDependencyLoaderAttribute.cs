// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Statistics;
using osu.Framework.Utils;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Marks a method as the (potentially asynchronous) initialization method of a <see cref="Graphics.Drawable"/>, allowing for automatic injection of dependencies via the parameters of the method.
    /// </summary>
    [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    [AttributeUsage(AttributeTargets.Method)]
    public class BackgroundDependencyLoaderAttribute : Attribute
    {
        private static readonly GlobalStatistic<int> count_reflection_attributes = GlobalStatistics.Get<int>("Dependencies", "Reflected [BackgroundDependencyLoader]s");

        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private bool permitNulls { get; }

        /// <summary>
        /// Marks this method as the (potentially asynchronous) initializer for a class in the context of dependency injection.
        /// </summary>
        public BackgroundDependencyLoaderAttribute()
        {
        }

        /// <summary>
        /// Marks this method as the (potentially asynchronous) initializer for a class in the context of dependency injection.
        /// </summary>
        /// <param name="permitNulls">If true, the initializer may be passed null for the dependencies we can't fulfill.</param>
        public BackgroundDependencyLoaderAttribute(bool permitNulls)
        {
            this.permitNulls = permitNulls;
        }

        [StackTraceHidden]
        internal static InjectDependencyDelegate CreateActivator(Type type)
        {
            count_reflection_attributes.Value++;

            var loaderMethods = type.GetMethods(activator_flags).Where(m => m.GetCustomAttribute<BackgroundDependencyLoaderAttribute>() != null).ToArray();

            switch (loaderMethods.Length)
            {
                case 0:
                    return (_, _) => { };

                case 1:
                    var method = loaderMethods[0];

                    var modifier = method.GetAccessModifier();
                    if (modifier != AccessModifier.Private)
                        throw new AccessModifierNotAllowedForLoaderMethodException(modifier, method);

                    var attribute = method.GetCustomAttribute<BackgroundDependencyLoaderAttribute>();
                    Debug.Assert(attribute != null);

                    ParameterExpression targetParam = Expression.Parameter(typeof(object), "target");
                    ParameterExpression dcParam = Expression.Parameter(typeof(IReadOnlyDependencyContainer), "dc");
                    MethodCallExpression loaderInvocation = Expression.Call(
                        Expression.Convert(targetParam, type),
                        method,
                        method.GetParameters().Select(p =>
                            Expression.Convert(
                                Expression.Invoke(
                                    Expression.Constant(getDependency(p.ParameterType, type, attribute.permitNulls || p.IsNullable())),
                                    dcParam),
                                p.ParameterType)));

                    return Expression.Lambda<InjectDependencyDelegate>(loaderInvocation, $"{type}.{method.Name}()", [targetParam, dcParam]).Compile();

                default:
                    throw new MultipleDependencyLoaderMethodsException(type);
            }
        }

        private static Func<IReadOnlyDependencyContainer, object?> getDependency(Type type, Type requestingType, bool permitNulls)
            => dc => SourceGeneratorUtils.GetDependency(dc, type, requestingType, null, null, permitNulls, false);
    }
}
