#nullable enable

using System.Collections.Generic;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Core;

public class EventBusTests : KBTVTestClass
{
    public EventBusTests(Node testScene) : base(testScene) { }

    [Test]
    public void Publish_DeliversEventToSubscribedHandler()
    {
        var eventBus = new EventBus();
        var receivedEvents = new List<AudioCompletedEvent>();

        eventBus.Subscribe<AudioCompletedEvent>(@event => receivedEvents.Add(@event));

        var testEvent = new AudioCompletedEvent("test_line", Speaker.Vern);
        eventBus.Publish(testEvent);

        AssertThat(receivedEvents.Contains(testEvent));
    }

    [Test]
    public void Publish_DeliversEventToCorrectTypeHandler()
    {
        var eventBus = new EventBus();
        var receivedEvents = new List<AudioCompletedEvent>();

        eventBus.Subscribe<AudioCompletedEvent>(@event => receivedEvents.Add(@event));

        var audioEvent = new AudioCompletedEvent("test_line", Speaker.Caller);
        var otherEvent = new AudioCompletedEvent("other_line", Speaker.Vern);

        eventBus.Publish(audioEvent);
        eventBus.Publish(otherEvent);

        AssertThat(receivedEvents.Contains(audioEvent));
        AssertThat(receivedEvents.Contains(otherEvent));
    }

    [Test]
    public void Unsubscribe_RemovesHandler()
    {
        var eventBus = new EventBus();
        var receivedEvents = new List<AudioCompletedEvent>();

        void Handler(AudioCompletedEvent @event) => receivedEvents.Add(@event);

        eventBus.Subscribe<AudioCompletedEvent>(Handler);
        eventBus.Unsubscribe<AudioCompletedEvent>(Handler);

        var testEvent = new AudioCompletedEvent("test_line", Speaker.Vern);
        eventBus.Publish(testEvent);

        AssertThat(receivedEvents.Count == 0);
    }
}