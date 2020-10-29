using System;
using System.Text;
using Xunit;

namespace Vertex.Protocol.Test
{
    public class Event_Converter
    {
        [Theory]
        [InlineData(9999L)]
        [InlineData("9999L")]
        [InlineData("")]
        public void ConvertToBytes(object id)
        {
            if (id is string strId && string.IsNullOrEmpty(strId))
            {
                id = Guid.NewGuid();
            }

            var eventName = "TestName";
            var baseBytes = Encoding.UTF8.GetBytes(eventName);
            var eventBytes = Encoding.UTF8.GetBytes(eventName);
            var eventUnit = new EventTransUnit(eventName, id, baseBytes, eventBytes);
            using var array = eventUnit.ConvertToBytes();
            var bytes = array.ToArray();
            var tryResult = EventConverter.TryParse(bytes, out var bitUnit);
            Assert.True(tryResult);
            Assert.Equal(id, bitUnit.ActorId);
            Assert.Equal(eventName, bitUnit.EventName);
            Assert.Equal(eventName, Encoding.UTF8.GetString(bitUnit.MetaBytes));
            Assert.Equal(eventName, Encoding.UTF8.GetString(bitUnit.EventBytes));
            tryResult = EventConverter.TryParseWithNoId(bytes, out bitUnit);
            Assert.True(tryResult);
            Assert.Null(bitUnit.ActorId);
            Assert.Equal(eventName, bitUnit.EventName);
            Assert.Equal(eventName, Encoding.UTF8.GetString(bitUnit.MetaBytes));
            Assert.Equal(eventName, Encoding.UTF8.GetString(bitUnit.EventBytes));
            tryResult = EventConverter.TryParseActorId(bytes, out var actorId);
            Assert.True(tryResult);
            Assert.Equal(id, actorId);
        }
    }
}
