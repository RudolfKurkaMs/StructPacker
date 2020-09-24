using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    internal static class TestExtensions
    {
        [DebuggerStepThrough,  ContractAnnotation("value:notnull=>halt")]
        public static void TestIsNull<T>(this T value, string message = "")
            where T : class
        {
            Assert.IsTrue(value == null, message);
        }

        [DebuggerStepThrough,  ContractAnnotation("value:null=>halt")]
        public static void TestIsNotNull<T>(this T value, string message = "")
            where T : class
        {
            Assert.IsTrue(value != null, message);
        }

        [DebuggerStepThrough]
        public static void TestReferenceEquals<T1, T2>(this T1 actual, T2 expected, string message = "")
            where T1 : class
            where T2 : class
        {
            Assert.IsTrue(ReferenceEquals(actual, expected), message);
        }

        [DebuggerStepThrough]
        public static void TestReferenceDiffer<T1, T2>(this T1 actual, T2 expected, string message = "")
            where T1 : class
            where T2 : class
        {
            Assert.IsTrue(!ReferenceEquals(actual, expected), message);
        }

        [DebuggerStepThrough,  ContractAnnotation("value:true=>halt")]
        public static void TestIsFalse(this bool value, string message = "")
        {
            Assert.IsFalse(value, message);
        }

        [DebuggerStepThrough,  ContractAnnotation("value:false=>halt")]
        public static void TestIsTrue(this bool value, string message = "")
        {
            Assert.IsTrue(value, message);
        }

        [DebuggerStepThrough]
        public static void TestEqual<T>(this T actual, T expected)
        {
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Also compares GetHashCode.
        /// </summary> 
        [DebuggerStepThrough]
        public static void TestEqualHash<T>(this T actual, T expected, bool checkHashCode = true)
            where T : IEquatable<T>
        {
            actual.Equals(expected).TestIsTrue();

            if (checkHashCode)
                Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode(), "Hash codes are not equal.");
        }

        /// <summary>
        /// Also checks CompareTo.
        /// </summary>
        [DebuggerStepThrough]
        public static void TestEqualCmp<T>(this T left, T right)
            where T : IComparable<T>
        {
            TestEqual(left, right);
            Assert.AreEqual(left.CompareTo(right), 0);
        }

        [DebuggerStepThrough]
        public static void TestCollectionEqual<T>(this ICollection<T> source, params T[] expected)
        {
            if (source == null)
            {
                expected.TestIsNull();
                return;
            }

            source.Count.TestEqual(expected.Length);
            source.TestSequenceEqual(expected);
        }

        [DebuggerStepThrough]
        public static void TestSequenceEqual<T>(this IEnumerable<T> source, params T[] expected)
        {
            using (IEnumerator<T> enumerator = source.GetEnumerator())
            {
                if (expected == null || expected.Length <= 0)
                {
                    enumerator.MoveNext().TestIsFalse("Sequence is supposed to be empty.");
                    return;
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                for (int i = 0; i < expected.Length; i++)
                {
                    enumerator.MoveNext().TestIsTrue("Sequence does not contain enough elements.");
                    enumerator.Current.TestEqual(expected[i]);
                }

                enumerator.MoveNext().TestIsFalse("Sequence contains too much elements.");
            }
        }

        [DebuggerStepThrough]
        public static void TestHasValue<T>(this T? value, bool expected = true)
            where T : struct
        {
            Assert.AreEqual(expected, value.HasValue);
        }

        [DebuggerStepThrough]
        public static void TestHasNotValue<T>(this T? value, bool expected = true)
            where T : struct
        {
            Assert.AreEqual(expected, !value.HasValue);
        }

        [DebuggerStepThrough]
        public static void TestNotEqual<T>(this T actual, T expected)
        {
            Assert.AreNotEqual(expected, actual);
        }

        [DebuggerStepThrough]
        public static void TestIsLess<T>(this T left, T right)
            where T : IComparable<T>
        {
            Assert.IsTrue(left.CompareTo(right) < 0);
        }

        [DebuggerStepThrough]
        public static void TestIsLessOrEqual<T>(this T left, T right)
            where T : IComparable<T>
        {
            Assert.IsTrue(left.CompareTo(right) <= 0);
        }

        [DebuggerStepThrough]
        public static void TestIsGreater<T>(this T left, T right)
            where T : IComparable<T>
        {
            Assert.IsTrue(left.CompareTo(right) > 0);
        }

        [DebuggerStepThrough]
        public static void TestIsGreaterOrEqual<T>(this T left, T right)
            where T : IComparable<T>
        {
            Assert.IsTrue(left.CompareTo(right) >= 0);
        }
    }
}