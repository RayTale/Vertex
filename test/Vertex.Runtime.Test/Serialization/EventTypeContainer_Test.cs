using Microsoft.Extensions.DependencyInjection;
using System;
using Vertex.Abstractions.Event;
using Vertex.Runtime.Test.Events;
using Vertex.Runtime.Core;
using Vertext.Abstractions.Event;
using Xunit;

namespace Vertex.Runtime.Test.Serialization
{
    [Collection(ProviderCollection.Name)]
    public class EventTypeContainer_Test
    {
        private readonly IEventTypeContainer eventTypeContainer;

        public EventTypeContainer_Test(ProviderFixture fixture)
        {
            eventTypeContainer = fixture.Provider.GetService<IEventTypeContainer>();
        }
        [Theory]
        [InlineData(typeof(TopupEvent))]
        [InlineData(typeof(TransferArrivedEvent))]
        [InlineData(typeof(TransferEvent))]
        [InlineData(typeof(TransferRefundsEvent))]
        [InlineData(typeof(NoNamedEvent))]
        public void TryGet_Name(Type type)
        {
            if (type != typeof(NoNamedEvent))
            {
                var result = eventTypeContainer.TryGet(type, out var name);
                Assert.Equal(name, type.Name);
                Assert.True(result);
            }
            else
            {
                var result = eventTypeContainer.TryGet(type, out var _);
                Assert.False(result);
            }
        }
        [Theory]
        [InlineData(nameof(TopupEvent))]
        [InlineData(nameof(TransferArrivedEvent))]
        [InlineData(nameof(TransferEvent))]
        [InlineData(nameof(TransferRefundsEvent))]
        [InlineData(nameof(NoNamedEvent))]
        public void TryGet_Type(string name)
        {
            if (name != nameof(NoNamedEvent))
            {
                var result = eventTypeContainer.TryGet(name, out var type);
                Assert.Equal(name, type.Name);
                Assert.True(result);
            }
            else
            {
                var result = eventTypeContainer.TryGet(name, out var _);
                Assert.False(result);
            }
        }
    }
    public class NoNamedEvent : IEvent
    {
    }
}
