using System;
using Microsoft.Extensions.DependencyInjection;
using Vertex.Abstractions.Event;
using Vertex.Runtime.Core;
using Vertex.Runtime.Test.Events;
using Xunit;

namespace Vertex.Runtime.Test.Serialization
{
    [Collection(ProviderCollection.Name)]
    public class EventTypeContainer_Test
    {
        private readonly IEventTypeContainer eventTypeContainer;

        public EventTypeContainer_Test(ProviderFixture fixture)
        {
            this.eventTypeContainer = fixture.Provider.GetService<IEventTypeContainer>();
        }

        [Theory]
        [InlineData(typeof(TopupEvent))]
        [InlineData(typeof(TransferArrivedEvent))]
        [InlineData(typeof(TransferEvent))]
        [InlineData(typeof(TransferRefundsEvent))]
        [InlineData(typeof(NoNamedEvent))]
        public void TryGet_Name(Type type)
        {
            var result = this.eventTypeContainer.TryGet(type, out var name);
            Assert.Equal(name, type.Name);
            Assert.True(result);
        }

        [Theory]
        [InlineData(nameof(TopupEvent))]
        [InlineData(nameof(TransferArrivedEvent))]
        [InlineData(nameof(TransferEvent))]
        [InlineData(nameof(TransferRefundsEvent))]
        [InlineData(nameof(NoNamedEvent))]
        public void TryGet_Type(string name)
        {
            var result = this.eventTypeContainer.TryGet(name, out var type);
            Assert.Equal(name, type.Name);
            Assert.True(result);
        }
    }
}
