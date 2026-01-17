#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;

namespace KBTV.Tests.Unit.Core
{
    public class TestEvent
    {
        public string Message { get; set; } = "";
        public int Count { get; set; }
    }

    public class AnotherTestEvent
    {
        public float Value { get; set; }
    }

    public class EventAggregatorTests : KBTVTestClass
    {
        private EventAggregator _eventAggregator = null!;

        public EventAggregatorTests(Node testScene) : base(testScene) { }

        [Setup]
        public void Setup()
        {
            _eventAggregator = new EventAggregator();
        }

        [Test]
        public void Subscribe_AddsHandler()
        {
            bool handlerCalled = false;
            _eventAggregator.Subscribe<TestEvent>(this, (evt) => handlerCalled = true);
            _eventAggregator.Publish(new TestEvent());
            AssertThat(handlerCalled);
            AssertThat(_eventAggregator.IsSubscribed(this));
        }

        [Test]
        public void Subscribe_NullSubscriber_DoesNotThrow()
        {
            _eventAggregator.Subscribe<TestEvent>(null!, (obj) => { });
            AssertThat(!_eventAggregator.IsSubscribed(null!));
        }

        [Test]
        public void Subscribe_NullHandler_DoesNotThrow()
        {
            _eventAggregator.Subscribe<TestEvent>(this, null!);
            AssertThat(!_eventAggregator.IsSubscribed(this));
        }

        [Test]
        public void Publish_DeliversEventToHandler()
        {
            TestEvent? receivedEvent = null;
            _eventAggregator.Subscribe<TestEvent>(this, (evt) => receivedEvent = evt);
            var testEvent = new TestEvent { Message = "Hello", Count = 42 };

            _eventAggregator.Publish(testEvent);

            AssertThat(receivedEvent != null);
            AssertThat(receivedEvent!.Message == "Hello");
            AssertThat(receivedEvent.Count == 42);
        }

        [Test]
        public void Publish_MultipleSubscribers_AllReceiveEvent()
        {
            int callCount = 0;
            var subscriber1 = new object();
            var subscriber2 = new object();
            var subscriber3 = new object();
            _eventAggregator.Subscribe<TestEvent>(subscriber1, (_) => callCount++);
            _eventAggregator.Subscribe<TestEvent>(subscriber2, (_) => callCount++);
            _eventAggregator.Subscribe<TestEvent>(subscriber3, (_) => callCount++);

            _eventAggregator.Publish(new TestEvent());

            AssertThat(callCount == 3);
        }

        [Test]
        public void Publish_DifferentEventTypes_DoNotInterfere()
        {
            int testEventCount = 0;
            int anotherEventCount = 0;
            var subscriber = new object();

            _eventAggregator.Subscribe<TestEvent>(subscriber, (_) => testEventCount++);
            _eventAggregator.Subscribe<AnotherTestEvent>(subscriber, (_) => anotherEventCount++);

            _eventAggregator.Publish(new TestEvent());
            AssertThat(testEventCount == 1);
            AssertThat(anotherEventCount == 0);

            _eventAggregator.Publish(new AnotherTestEvent { Value = 1.5f });
            AssertThat(testEventCount == 1);
            AssertThat(anotherEventCount == 1);
        }

        [Test]
        public void Unsubscribe_RemovesHandler()
        {
            var subscriber = new object();
            _eventAggregator.Subscribe<TestEvent>(subscriber, (_) => { });
            AssertThat(_eventAggregator.IsSubscribed(subscriber));

            _eventAggregator.Unsubscribe(subscriber);
            AssertThat(!_eventAggregator.IsSubscribed(subscriber));
        }

        [Test]
        public void Unsubscribe_TEvent_RemovesSpecificHandler()
        {
            int testEventCount = 0;
            int anotherEventCount = 0;
            var subscriber = new object();

            _eventAggregator.Subscribe<TestEvent>(subscriber, (_) => testEventCount++);
            _eventAggregator.Subscribe<AnotherTestEvent>(subscriber, (_) => anotherEventCount++);

            AssertThat(_eventAggregator.IsSubscribed(subscriber));

            _eventAggregator.Unsubscribe<TestEvent>(subscriber);

            _eventAggregator.Publish(new AnotherTestEvent { Value = 1.5f });
            AssertThat(anotherEventCount == 1);

            AssertThat(_eventAggregator.IsSubscribed(subscriber));
        }

        [Test]
        public void Unsubscribe_NullSubscriber_DoesNotThrow()
        {
            _eventAggregator.Subscribe<TestEvent>(this, (_) => { });
            _eventAggregator.Unsubscribe(null!);
            AssertThat(!_eventAggregator.IsSubscribed(null!));
        }

        [Test]
        public void Publish_AfterUnsubscribe_NoHandlerCalled()
        {
            int callCount = 0;
            var subscriber = new object();
            _eventAggregator.Subscribe<TestEvent>(subscriber, (_) => callCount++);
            _eventAggregator.Unsubscribe(subscriber);

            _eventAggregator.Publish(new TestEvent());

            AssertThat(callCount == 0);
            AssertThat(!_eventAggregator.IsSubscribed(subscriber));
        }

        [Test]
        public void IsSubscribed_NoSubscribers_ReturnsFalse()
        {
            var subscriber = new object();
            AssertThat(!_eventAggregator.IsSubscribed(subscriber));
        }

        [Test]
        public void IsSubscribed_AfterSubscribe_ReturnsTrue()
        {
            var subscriber = new object();
            _eventAggregator.Subscribe<TestEvent>(subscriber, (_) => { });
            AssertThat(_eventAggregator.IsSubscribed(subscriber));
        }

        [Test]
        public void Publish_ManyEvents_HandlerReceivesAll()
        {
            int receivedCount = 0;
            var subscriber = new object();
            _eventAggregator.Subscribe<TestEvent>(subscriber, (_) => receivedCount++);

            for (int i = 0; i < 10; i++)
            {
                _eventAggregator.Publish(new TestEvent());
            }

            AssertThat(receivedCount == 10);
        }
    }
}
