using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using System.Net.Http;

namespace ByteForge.Toolkit.Tests.Helpers
{
    /// <summary>
    /// Provides extension methods for performing fluent assertions on various types of subjects.
    /// </summary>
    /// <remarks>
    /// This static helper class contains a collection of assertion methods that extend the
    /// functionality of FluentAssertions. These methods allow users to assert that a subject satisfies specific
    /// conditions, such as meeting all or any of a set of predicates. The methods are designed to work with a variety
    /// of types, including reference types, value types, nullable types, and specific types like 
    /// <see cref="DateTime"/>, <see cref="Guid"/>, and <see cref="HttpResponseMessage"/> 
    /// <para> Each method returns an <see cref="AndConstraint{T}"/> object, enabling further assertions to be 
    /// chained fluently. The methods also support optional "because" arguments to provide context for assertion failures. 
    /// </para>
    /// </remarks>
    internal static class FluentAssertionsExtensions
    {
        /// <summary>
        /// Asserts that the subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <typeparam name="TSubject">The type of the subject being asserted.</typeparam>
        /// <typeparam name="TAssertions">The type of the assertion object.</typeparam>
        /// <param name="assertion">The assertion object containing the subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed. This is optional and can include placeholders for arguments.</param>
        /// <param name="predicates">An array of predicates to evaluate against the subject. At least one predicate must return <see langword="true"/> for the assertion to pass.</param>
        /// <returns>An <see cref="AndConstraint{TAssertions}"/> that can be used to chain additional assertions.</returns>
        /// <remarks>
        /// This method is typically used to verify that a subject meets at least one of a set of
        /// conditions. If no predicates are provided, the assertion will fail.
        /// </remarks>
        public static AndConstraint<TAssertions> SatisfyAny<TSubject, TAssertions>(
            this ReferenceTypeAssertions<TSubject, TAssertions> assertion,
            string because = "",
            params Func<TSubject, bool>[] predicates)
            where TAssertions : ReferenceTypeAssertions<TSubject, TAssertions>
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<TAssertions>((TAssertions)assertion);
        }

        /// <summary>
        /// Asserts that the subject satisfies all of the specified predicates.
        /// </summary>
        /// <typeparam name="TSubject">The type of the subject being asserted.</typeparam>
        /// <typeparam name="TAssertions">The type of the assertion object.</typeparam>
        /// <param name="assertion">The assertion object containing the subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed. This is optional and can include placeholders for arguments.</param>
        /// <param name="predicates">An array of predicates to evaluate against the subject. All predicates must return <see langword="true"/> for the assertion to pass.</param>
        /// <returns>An <see cref="AndConstraint{TAssertions}"/> that can be used to chain additional assertions.</returns>
        /// <remarks>
        /// This method is typically used to verify that a subject meets all of a set of
        /// conditions. If no predicates are provided, the assertion will fail.
        /// </remarks>
        public static AndConstraint<TAssertions> SatisfyAll<TSubject, TAssertions>(
            this ReferenceTypeAssertions<TSubject, TAssertions> assertion,
            string because = "",
            params Func<TSubject, bool>[] predicates)
            where TAssertions : ReferenceTypeAssertions<TSubject, TAssertions>
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<TAssertions>((TAssertions)assertion);
        }

        /// <summary>
        /// Asserts that the boolean subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the boolean subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the boolean subject.</param>
        /// <returns>An <see cref="AndConstraint{BooleanAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<BooleanAssertions> SatisfyAny(
            this BooleanAssertions assertion,
            string because = "",
            params Func<bool?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<BooleanAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the boolean subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the boolean subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the boolean subject.</param>
        /// <returns>An <see cref="AndConstraint{BooleanAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<BooleanAssertions> SatisfyAll(
            this BooleanAssertions assertion,
            string because = "",
            params Func<bool?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<BooleanAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the DateTime subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the DateTime subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the DateTime subject.</param>
        /// <returns>An <see cref="AndConstraint{DateTimeAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<DateTimeAssertions> SatisfyAny(
            this DateTimeAssertions assertion,
            string because = "",
            params Func<DateTime?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<DateTimeAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the DateTime subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the DateTime subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the DateTime subject.</param>
        /// <returns>An <see cref="AndConstraint{DateTimeAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<DateTimeAssertions> SatisfyAll(
            this DateTimeAssertions assertion,
            string because = "",
            params Func<DateTime?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<DateTimeAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the DateTimeOffset subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the DateTimeOffset subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the DateTimeOffset subject.</param>
        /// <returns>An <see cref="AndConstraint{DateTimeOffsetAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<DateTimeOffsetAssertions> SatisfyAny(
            this DateTimeOffsetAssertions assertion,
            string because = "",
            params Func<DateTimeOffset?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<DateTimeOffsetAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the DateTimeOffset subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the DateTimeOffset subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the DateTimeOffset subject.</param>
        /// <returns>An <see cref="AndConstraint{DateTimeOffsetAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<DateTimeOffsetAssertions> SatisfyAll(
            this DateTimeOffsetAssertions assertion,
            string because = "",
            params Func<DateTimeOffset?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<DateTimeOffsetAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the Guid subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the Guid subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the Guid subject.</param>
        /// <returns>An <see cref="AndConstraint{GuidAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<GuidAssertions> SatisfyAny(
            this GuidAssertions assertion,
            string because = "",
            params Func<Guid?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<GuidAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the Guid subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the Guid subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the Guid subject.</param>
        /// <returns>An <see cref="AndConstraint{GuidAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<GuidAssertions> SatisfyAll(
            this GuidAssertions assertion,
            string because = "",
            params Func<Guid?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<GuidAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the enum subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="assertion">The assertion object containing the enum subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the enum subject.</param>
        /// <returns>An <see cref="AndConstraint{EnumAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<EnumAssertions<TEnum>> SatisfyAny<TEnum>(
            this EnumAssertions<TEnum> assertion,
            string because = "",
            params Func<TEnum?, bool>[] predicates)
            where TEnum : struct, Enum
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<EnumAssertions<TEnum>>(assertion);
        }

        /// <summary>
        /// Asserts that the enum subject satisfies all of the specified predicates.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="assertion">The assertion object containing the enum subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the enum subject.</param>
        /// <returns>An <see cref="AndConstraint{EnumAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<EnumAssertions<TEnum>> SatisfyAll<TEnum>(
            this EnumAssertions<TEnum> assertion,
            string because = "",
            params Func<TEnum?, bool>[] predicates)
            where TEnum : struct, Enum
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<EnumAssertions<TEnum>>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable boolean subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable boolean subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable boolean subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableBooleanAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableBooleanAssertions> SatisfyAny(
            this NullableBooleanAssertions assertion,
            string because = "",
            params Func<bool?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<NullableBooleanAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable boolean subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable boolean subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable boolean subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableBooleanAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableBooleanAssertions> SatisfyAll(
            this NullableBooleanAssertions assertion,
            string because = "",
            params Func<bool?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<NullableBooleanAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable DateTime subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable DateTime subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable DateTime subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableDateTimeAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableDateTimeAssertions> SatisfyAny(
            this NullableDateTimeAssertions assertion,
            string because = "",
            params Func<DateTime?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<NullableDateTimeAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable DateTime subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable DateTime subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable DateTime subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableDateTimeAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableDateTimeAssertions> SatisfyAll(
            this NullableDateTimeAssertions assertion,
            string because = "",
            params Func<DateTime?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<NullableDateTimeAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable DateTimeOffset subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable DateTimeOffset subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable DateTimeOffset subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableDateTimeOffsetAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableDateTimeOffsetAssertions> SatisfyAny(
            this NullableDateTimeOffsetAssertions assertion,
            string because = "",
            params Func<DateTimeOffset?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<NullableDateTimeOffsetAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable DateTimeOffset subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable DateTimeOffset subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable DateTimeOffset subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableDateTimeOffsetAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableDateTimeOffsetAssertions> SatisfyAll(
            this NullableDateTimeOffsetAssertions assertion,
            string because = "",
            params Func<DateTimeOffset?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<NullableDateTimeOffsetAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable enum subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="assertion">The assertion object containing the nullable enum subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable enum subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableEnumAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableEnumAssertions<TEnum>> SatisfyAny<TEnum>(
            this NullableEnumAssertions<TEnum> assertion,
            string because = "",
            params Func<TEnum?, bool>[] predicates)
            where TEnum : struct, Enum
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<NullableEnumAssertions<TEnum>>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable enum subject satisfies all of the specified predicates.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="assertion">The assertion object containing the nullable enum subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable enum subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableEnumAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableEnumAssertions<TEnum>> SatisfyAll<TEnum>(
            this NullableEnumAssertions<TEnum> assertion,
            string because = "",
            params Func<TEnum?, bool>[] predicates)
            where TEnum : struct, Enum
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<NullableEnumAssertions<TEnum>>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable Guid subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable Guid subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable Guid subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableGuidAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableGuidAssertions> SatisfyAny(
            this NullableGuidAssertions assertion,
            string because = "",
            params Func<Guid?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<NullableGuidAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the nullable Guid subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the nullable Guid subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the nullable Guid subject.</param>
        /// <returns>An <see cref="AndConstraint{NullableGuidAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<NullableGuidAssertions> SatisfyAll(
            this NullableGuidAssertions assertion,
            string because = "",
            params Func<Guid?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<NullableGuidAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the HttpResponseMessage subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the HttpResponseMessage subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the HttpResponseMessage subject.</param>
        /// <returns>An <see cref="AndConstraint{HttpResponseMessageAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<HttpResponseMessageAssertions> SatisfyAny(
            this HttpResponseMessageAssertions assertion,
            string because = "",
            params Func<HttpResponseMessage, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<HttpResponseMessageAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the HttpResponseMessage subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the HttpResponseMessage subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the HttpResponseMessage subject.</param>
        /// <returns>An <see cref="AndConstraint{HttpResponseMessageAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<HttpResponseMessageAssertions> SatisfyAll(
            this HttpResponseMessageAssertions assertion,
            string because = "",
            params Func<HttpResponseMessage, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<HttpResponseMessageAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the TimeSpan subject satisfies at least one of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the TimeSpan subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the TimeSpan subject.</param>
        /// <returns>An <see cref="AndConstraint{SimpleTimeSpanAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<SimpleTimeSpanAssertions> SatisfyAny(
            this SimpleTimeSpanAssertions assertion,
            string because = "",
            params Func<TimeSpan?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.Any(p => p(assertion.Subject)))
                .FailWith("Expected any predicate to be satisfied, but none were.");

            return new AndConstraint<SimpleTimeSpanAssertions>(assertion);
        }

        /// <summary>
        /// Asserts that the TimeSpan subject satisfies all of the specified predicates.
        /// </summary>
        /// <param name="assertion">The assertion object containing the TimeSpan subject to evaluate.</param>
        /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
        /// <param name="predicates">An array of predicates to evaluate against the TimeSpan subject.</param>
        /// <returns>An <see cref="AndConstraint{SimpleTimeSpanAssertions}"/> for chaining further assertions.</returns>
        public static AndConstraint<SimpleTimeSpanAssertions> SatisfyAll(
            this SimpleTimeSpanAssertions assertion,
            string because = "",
            params Func<TimeSpan?, bool>[] predicates)
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(predicates.All(p => p(assertion.Subject)))
                .FailWith("Expected all predicates to be satisfied, but not all were.");

            return new AndConstraint<SimpleTimeSpanAssertions>(assertion);
        }
    }
}
