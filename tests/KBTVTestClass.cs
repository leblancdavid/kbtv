using System;
using Chickensoft.GoDotTest;
using Godot;

namespace KBTV.Tests
{
    public abstract class KBTVTestClass : TestClass
    {
        protected KBTVTestClass(Node testScene) : base(testScene) { }

        protected void AssertThat(bool condition)
        {
            if (!condition)
                throw new AssertionException("Assertion failed");
        }

        protected void AssertThat(bool condition, string message)
        {
            if (!condition)
                throw new AssertionException(message);
        }

        protected void AssertAreEqual<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new AssertionException($"Expected {expected}, got {actual}");
        }

        protected void AssertAreEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new AssertionException(message);
        }

        protected void AssertNotNull(object obj)
        {
            if (obj == null)
                throw new AssertionException("Expected non-null value");
        }

        protected void AssertNull(object obj)
        {
            if (obj != null)
                throw new AssertionException("Expected null value");
        }

        protected void AssertThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
                throw new AssertionException($"Expected {typeof(T).Name} to be thrown");
            }
            catch (T)
            {
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AssertionException($"Expected {typeof(T).Name} but got {ex.GetType().Name}");
            }
        }

        protected void AssertTrue(bool condition) => AssertThat(condition);
        protected void AssertFalse(bool condition) => AssertThat(!condition);
        protected void AssertGreater<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) <= 0)
                throw new AssertionException($"Expected {actual} > {limit}");
        }
        protected void AssertLess<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) >= 0)
                throw new AssertionException($"Expected {actual} < {limit}");
        }
        protected void AssertGreaterOrEqual<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) < 0)
                throw new AssertionException($"Expected {actual} >= {limit}");
        }
        protected void AssertLessOrEqual<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) > 0)
                throw new AssertionException($"Expected {actual} <= {limit}");
        }
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}
