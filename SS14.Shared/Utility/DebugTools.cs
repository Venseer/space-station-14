﻿using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace SS14.Shared.Utility
{
    public static class DebugTools
    {
        /// <summary>
        ///     An assertion that will always <see langword="throw" /> an exception.
        /// </summary>
        /// <param name="message">Exception message.</param>
        [Conditional("DEBUG")]
        [ContractAnnotation("=> halt")]
        public static void Assert(string message)
        {
            throw new DebugAssertException(message);
        }

        /// <summary>
        ///     An assertion that will <see langword="throw" /> an exception if the
        ///     <paramref name="condition" /> is not true.
        /// </summary>
        /// <param name="condition">Condition that must be true.</param>
        [Conditional("DEBUG")]
        [AssertionMethod]
        public static void Assert([AssertionCondition(AssertionConditionType.IS_TRUE)]
            bool condition)
        {
            if (!condition)
                throw new DebugAssertException();
        }

        /// <summary>
        ///     An assertion that will <see langword="throw" /> an exception if the
        ///     <paramref name="condition" /> is not true.
        /// </summary>
        /// <param name="condition">Condition that must be true.</param>
        /// <param name="message">Exception message.</param>
        [Conditional("DEBUG")]
        [AssertionMethod]
        public static void Assert([AssertionCondition(AssertionConditionType.IS_TRUE)]
            bool condition, string message)
        {
            if (!condition)
                throw new DebugAssertException(message);
        }

        /// <summary>
        ///     An assertion that will <see langword="throw" /> an exception if the
        ///     <paramref name="arg" /> is <see langword="null" />.
        /// </summary>
        /// <param name="arg">Condition that must be true.</param>
        [Conditional("DEBUG")]
        [AssertionMethod]
        public static void AssertNotNull([AssertionCondition(AssertionConditionType.IS_NOT_NULL)]
            object arg)
        {
            if (arg == null)
            {
                throw new DebugAssertException();
            }
        }

        /// <summary>
        ///     An assertion that will <see langword="throw" /> an exception if the
        ///     <paramref name="arg" /> is not <see langword="null" />.
        /// </summary>
        /// <param name="arg">Condition that must be true.</param>
        [Conditional("DEBUG")]
        [AssertionMethod]
        public static void AssertNull([AssertionCondition(AssertionConditionType.IS_NULL)]
            object arg)
        {
            if (arg != null)
            {
                throw new DebugAssertException();
            }
        }
    }

    public class DebugAssertException : Exception
    {
        public DebugAssertException()
        {
        }

        public DebugAssertException(string message) : base(message)
        {
        }
    }
}
