// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
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

                if (name == nameof(TestScene.TestConstructor) || m.GetCustomAttribute(typeof(SkipAttribute), false) != null)
                    continue;

                if (name.StartsWith("Test", StringComparison.Ordinal))
                    name = name.Substring(4);

                int runCount = 1;

                if (m.GetCustomAttribute(typeof(RepeatAttribute), false) != null)
                {
                    object? count = m.GetCustomAttributesData()
                                     .Single(a => a.AttributeType == typeof(RetryAttribute) || a.AttributeType == typeof(RepeatAttribute))
                                     .ConstructorArguments.Single().Value;

                    Debug.Assert(count != null);

                    runCount += (int)count;
                }

                for (int i = 0; i < runCount; i++)
                {
                    string repeatSuffix = i > 0 ? $" ({i + 1})" : string.Empty;

                    if (!m.GetCustomAttributes<TestAttribute>(false).Any())
                        continue;

                    var parameters = m.GetParameters();

                    if (parameters.Length > 0)
                    {
                        foreach (var argsAttribute in m.GetCustomAttributes<ArgumentsAttribute>())
                        {
                            object?[] values = argsAttribute.Values;

                            hadTestAttributeTest = true;
                            CurrentTest.AddLabel($"{name}({string.Join(", ", values)}){repeatSuffix}");
                            handleTestMethod(testScene, m, values.ToArray());
                        }

                        foreach (var dataAttribute in m.GetCustomAttributes<MethodDataSourceAttribute>())
                        {
                            IEnumerable sourceValue = getMethodDataSourceValue(m, dataAttribute);

                            if (sourceValue == null)
                            {
                                Debug.Assert(dataAttribute.MethodNameProvidingDataSource != null);
                                throw new InvalidOperationException($"The value of the source member {dataAttribute.MethodNameProvidingDataSource} must be non-null.");
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
                    else
                    {
                        hadTestAttributeTest = true;
                        CurrentTest.AddLabel($"{name}{repeatSuffix}");
                        handleTestMethod(testScene, m);
                    }
                }
            }

            // even if no [Test] or [TestCase] methods were found, [SetUp] steps should be added.
            if (!hadTestAttributeTest)
                addSetUpSteps(testScene);
        }

        private void addSetUpSteps(AdhocTestScene testScene)
        {
            MethodInfo[] beforeTestMethods = testScene.GetType().GetMethods().Where(m =>
            {
                foreach (var data in m.GetCustomAttributesData())
                {
                    if (data.AttributeType != typeof(BeforeAttribute))
                        continue;

                    return data.ConstructorArguments[0].Value is Test;
                }

                return false;
            }).ToArray();

            if (beforeTestMethods.Any())
            {
                CurrentTest.AddStep(new SingleStepButton(true)
                {
                    Text = "[SetUp]",
                    LightColour = Color4.Teal,
                    Action = () => beforeTestMethods.ForEach(s => s.Invoke(CurrentTest, null))
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

        private static IEnumerable getMethodDataSourceValue(MethodInfo testMethod, MethodDataSourceAttribute dataAttribute)
        {
            var sourceDeclaringType = dataAttribute.ClassProvidingDataSource ?? testMethod.DeclaringType;
            Debug.Assert(sourceDeclaringType != null);

            var sourceMethod = sourceDeclaringType.AsNonNull().GetMethod(dataAttribute.MethodNameProvidingDataSource,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (sourceMethod == null)
                throw new InvalidOperationException($"No static method with the name of {dataAttribute.MethodNameProvidingDataSource} exists in {sourceDeclaringType} or its base types.");

            return (IEnumerable)sourceMethod.Invoke(null, null)!;
        }
    }
}
