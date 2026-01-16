using System;
using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;

namespace KBTV.Tests
{
    public abstract class KBTVTestClass : TestClass
    {
        private readonly List<Exception> _recordedFailures = new();

        protected KBTVTestClass(Node testScene) : base(testScene) { }

        protected void AssertThat(bool condition)
        {
            if (!condition)
                RecordFailure("Assertion failed");
        }

        protected void AssertThat(bool condition, string message)
        {
            if (!condition)
                RecordFailure(message);
        }

        protected void AssertAreEqual<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                RecordFailure($"Expected {expected}, got {actual}");
        }

        protected void AssertAreEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                RecordFailure(message);
        }

        protected void AssertNotNull(object obj)
        {
            if (obj == null)
                RecordFailure("Expected non-null value");
        }

        protected void AssertNull(object obj)
        {
            if (obj != null)
                RecordFailure("Expected null value");
        }

        protected void AssertThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
                RecordFailure($"Expected {typeof(T).Name} to be thrown");
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
                RecordFailure($"Expected {typeof(T).Name} but got {ex.GetType().Name}");
            }
        }

        protected void AssertTrue(bool condition) => AssertThat(condition);
        protected void AssertFalse(bool condition) => AssertThat(!condition);
        protected void AssertGreater<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) <= 0)
                RecordFailure($"Expected {actual} > {limit}");
        }
        protected void AssertLess<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) >= 0)
                RecordFailure($"Expected {actual} < {limit}");
        }
        protected void AssertGreaterOrEqual<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) < 0)
                RecordFailure($"Expected {actual} >= {limit}");
        }
        protected void AssertLessOrEqual<T>(T actual, T limit) where T : IComparable<T>
        {
            if (actual.CompareTo(limit) > 0)
                RecordFailure($"Expected {actual} <= {limit}");
        }

        private void RecordFailure(string message)
        {
            _recordedFailures.Add(new AssertionException(message));
            GD.PrintErr($"Test assertion failed: {message}");
        }

        [Cleanup]
        public void Cleanup()
        {
            if (_recordedFailures.Count > 0)
            {
                GD.PrintErr($"Test suite had {_recordedFailures.Count} failure(s)");
            }
        }
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}
