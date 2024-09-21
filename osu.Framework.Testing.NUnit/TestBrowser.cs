// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using osu.Framework.Development;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Testing.Drawables.Steps;
using osuTK.Graphics;

namespace osu.Framework.Testing
{
    public partial class TestBrowser : AdhocTestBrowser
    {
        public TestBrowser(string? assemblyNamespace = null)
            : base(assemblyNamespace)
        {
        }

        protected override void AddStepsForMethods(AdhocTestScene testScene, IEnumerable<MethodInfo> methods)
        {
            bool hadTestAttributeTest = false;

            foreach (var m in methods)
            {
                string name = m.Name;

                if (name == nameof(TestScene.TestConstructor) || m.GetCustomAttribute(typeof(IgnoreAttribute), false) != null)
                    continue;

                if (name.StartsWith("Test", StringComparison.Ordinal))
                    name = name.Substring(4);

                int runCount = 1;

                if (m.GetCustomAttribute(typeof(RepeatAttribute), false) != null)
                {
                    object? count = m.GetCustomAttributesData().Single(a => a.AttributeType == typeof(RepeatAttribute)).ConstructorArguments.Single().Value;
                    Debug.Assert(count != null);

                    runCount += (int)count;
                }

                for (int i = 0; i < runCount; i++)
                {
                    string repeatSuffix = i > 0 ? $" ({i + 1})" : string.Empty;

                    var methodWrapper = new MethodWrapper(m.GetType(), m);

                    if (methodWrapper.GetCustomAttributes<TestAttribute>(false).SingleOrDefault() != null)
                    {
                        var parameters = m.GetParameters();

                        if (parameters.Length > 0)
                        {
                            var valueMatrix = new List<List<object>>();

                            foreach (var p in methodWrapper.GetParameters())
                            {
                                var valueAttrib = p.GetCustomAttributes<ValuesAttribute>(false).SingleOrDefault();
                                if (valueAttrib == null)
                                    throw new ArgumentException($"Parameter is present on a {nameof(TestAttribute)} method without values specification.", p.ParameterInfo.Name);

                                List<object> choices = new List<object>();

                                foreach (object choice in valueAttrib.GetData(p))
                                    choices.Add(choice);

                                valueMatrix.Add(choices);
                            }

                            foreach (var combination in valueMatrix.CartesianProduct())
                            {
                                hadTestAttributeTest = true;
                                CurrentTest.AddLabel($"{name}({string.Join(", ", combination)}){repeatSuffix}");
                                handleTestMethod(testScene, m, combination.ToArray());
                            }
                        }
                        else
                        {
                            hadTestAttributeTest = true;
                            CurrentTest.AddLabel($"{name}{repeatSuffix}");
                            handleTestMethod(testScene, m);
                        }
                    }

                    foreach (var tc in m.GetCustomAttributes(typeof(TestCaseAttribute), false).OfType<TestCaseAttribute>())
                    {
                        hadTestAttributeTest = true;
                        CurrentTest.AddLabel($"{name}({string.Join(", ", tc.Arguments)}){repeatSuffix}");

                        handleTestMethod(testScene, m, tc.BuildFrom(methodWrapper, null).Single().Arguments);
                    }

                    foreach (var tcs in m.GetCustomAttributes(typeof(TestCaseSourceAttribute), false).OfType<TestCaseSourceAttribute>())
                    {
                        IEnumerable sourceValue = getTestCaseSourceValue(m, tcs);

                        if (sourceValue == null)
                        {
                            Debug.Assert(tcs.SourceName != null);
                            throw new InvalidOperationException($"The value of the source member {tcs.SourceName} must be non-null.");
                        }

                        foreach (object argument in sourceValue)
                        {
                            hadTestAttributeTest = true;

                            if (argument is IEnumerable argumentsEnumerable)
                            {
                                object[] arguments = argumentsEnumerable.Cast<object>().ToArray();

                                CurrentTest.AddLabel($"{name}({string.Join(", ", arguments)}){repeatSuffix}");
                                handleTestMethod(testScene, m, arguments);
                            }
                            else
                            {
                                CurrentTest.AddLabel($"{name}({argument}){repeatSuffix}");
                                handleTestMethod(testScene, m, argument);
                            }
                        }
                    }
                }
            }

            // even if no [Test] or [TestCase] methods were found, [SetUp] steps should be added.
            if (!hadTestAttributeTest)
                addSetUpSteps(testScene);
        }

        private void addSetUpSteps(AdhocTestScene testScene)
        {
            var setUpMethods = ReflectionUtils.GetMethodsWithAttribute(testScene.GetType(), typeof(SetUpAttribute), true);

            if (setUpMethods.Any())
            {
                CurrentTest.AddStep(new SingleStepButton(true)
                {
                    Text = "[SetUp]",
                    LightColour = Color4.Teal,
                    Action = () => setUpMethods.ForEach(s => s.Invoke(CurrentTest, null))
                });
            }

            CurrentTest.RunSetUpSteps();
        }

        private void handleTestMethod(AdhocTestScene testScene, MethodInfo methodInfo, params object?[]? arguments)
        {
            addSetUpSteps(testScene);
            methodInfo.Invoke(CurrentTest, arguments);
            CurrentTest.RunTearDownSteps();
        }

        private static IEnumerable getTestCaseSourceValue(MethodInfo testMethod, TestCaseSourceAttribute tcs)
        {
            var sourceDeclaringType = tcs.SourceType ?? testMethod.DeclaringType;
            Debug.Assert(sourceDeclaringType != null);

            if (tcs.SourceType != null && tcs.SourceName == null)
                return (IEnumerable)Activator.CreateInstance(tcs.SourceType)!;

            Debug.Assert(tcs.SourceName != null);

            var sourceMembers = sourceDeclaringType.AsNonNull().GetMember(tcs.SourceName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (sourceMembers.Length == 0)
                throw new InvalidOperationException($"No static member with the name of {tcs.SourceName} exists in {sourceDeclaringType} or its base types.");

            if (sourceMembers.Length > 1)
                throw new NotSupportedException($"There are multiple members with the same source name ({tcs.SourceName}) (e.g. method overloads).");

            var sourceMember = sourceMembers.Single();

            switch (sourceMember)
            {
                case FieldInfo sf:
                    return (IEnumerable)sf.GetValue(null)!;

                case PropertyInfo sp:
                    if (!sp.CanRead)
                        throw new InvalidOperationException($"The source property {sp.Name} in {sp.DeclaringType.ReadableName()} must have a getter.");

                    return (IEnumerable)sp.GetValue(null)!;

                case MethodInfo sm:
                    int methodParamsLength = sm.GetParameters().Length;

                    if (methodParamsLength != (tcs.MethodParams?.Length ?? 0))
                    {
                        throw new InvalidOperationException(
                            $"The given source method parameters count doesn't match the method. (attribute has {tcs.MethodParams?.Length ?? 0}, method has {methodParamsLength})");
                    }

                    return (IEnumerable)sm.Invoke(null, tcs.MethodParams)!;

                default:
                    throw new NotSupportedException($"{sourceMember.MemberType} is not a supported member type for {nameof(TestCaseSourceAttribute)} (must be static field, property or method)");
            }
        }
    }
}
