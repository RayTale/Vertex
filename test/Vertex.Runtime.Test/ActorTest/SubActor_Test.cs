using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Concurrency;
using Orleans.TestingHost;
using Vertex.Abstractions.Event;
using Vertex.Abstractions.Serialization;
using Vertex.Protocol;
using Vertex.Runtime.Core;
using Vertex.Runtime.Event;
using Vertex.Runtime.Test.IActors;
using Vertext.Abstractions.Event;
using Xunit;

namespace Vertex.Runtime.Test.ActorTest
{
    [Collection(ClusterCollection.Name)]
    public class SubActor_Test
    {
        private readonly TestCluster cluster;
        private readonly ISerializer serializer;
        private readonly IEventTypeContainer eventTypeContainer;

        public SubActor_Test(ClusterFixture fixture)
        {
            this.cluster = fixture.Cluster;
            this.serializer = fixture.Provider.GetService<ISerializer>();
            this.eventTypeContainer = fixture.Provider.GetService<IEventTypeContainer>();
        }

        [Theory]
        [InlineData(100, 10000, false)]
        [InlineData(498, 10001, true)]
        [InlineData(888, 10002, false)]
        [InlineData(1800, 100003, true)]
        public async Task Tell(int count, int id, bool current)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = this.cluster.GrainFactory.GetGrain<IAccount>(id);
            var accountSubActor = this.cluster.GrainFactory.GetGrain<IAccountSubTest>(id);
            var isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent);
            await accountSubActor.SetConcurrentHandle(current);
            isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent == current);

            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            var eventDocuments = await accountActor.GetEventDocuments(1, count);
            var eventUnits = this.ConvertToEventUnitList(eventDocuments, id);

            var snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == 0);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var testUnits = eventUnits.GetRange(0, 20).ToList();

            await accountSubActor.Tell_Test(testUnits);
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Count);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Count);

            await accountSubActor.Tell_Test(testUnits);
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Count);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Count);

            // Auto retrieve event
            testUnits = eventUnits.GetRange(30, 20).ToList();
            await accountSubActor.Tell_Test(testUnits);
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Max(e => e.Meta.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Max(e => e.Meta.Version));

            // Re enter the test
            testUnits = eventUnits.GetRange(40, count - 40).ToList();
            await accountSubActor.Tell_Test(testUnits);
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Max(e => e.Meta.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            Assert.True(snapshot.Version == count);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Max(e => e.Meta.Version));
        }

        [Theory]
        [InlineData(100, 10100, false)]
        [InlineData(498, 10101, true)]
        [InlineData(888, 10102, false)]
        [InlineData(1800, 10103, true)]
        public async Task ConcurrentTell(int count, int id, bool current)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = this.cluster.GrainFactory.GetGrain<IAccount>(id);
            var accountSubActor = this.cluster.GrainFactory.GetGrain<IAccountSubTest>(id);
            var isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent);
            await accountSubActor.SetConcurrentHandle(current);
            isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent == current);

            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            var eventDocuments = await accountActor.GetEventDocuments(1, count);
            var eventUnits = this.ConvertToEventUnitList(eventDocuments, id);
            var snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == 0);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var testUnits = eventUnits.GetRange(0, 20).ToList();

            await accountSubActor.Tell_Test(testUnits);
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Count);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Count);

            // Auto retrieve event
            testUnits = eventUnits.GetRange(60, 20).ToList();
            await accountSubActor.ConcurrentTell_Test(testUnits);
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Max(e => e.Meta.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Max(e => e.Meta.Version));

            // Re enter the test
            testUnits = eventUnits.GetRange(40, count - 40).ToList();
            await accountSubActor.ConcurrentTell_Test(testUnits);
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Max(e => e.Meta.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            Assert.True(snapshot.Version == count);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Max(e => e.Meta.Version));
        }

        [Theory]
        [InlineData(100, 10200, true)]
        [InlineData(498, 10201, false)]
        [InlineData(888, 10202, false)]
        [InlineData(1800, 10203, true)]
        public async Task OnBatchNext(int count, int id, bool current)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = this.cluster.GrainFactory.GetGrain<IAccount>(id);
            var accountSubActor = this.cluster.GrainFactory.GetGrain<IAccountSubTest>(id);

            var isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent);
            await accountSubActor.SetConcurrentHandle(current);
            isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent == current);

            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            var eventDocuments = await accountActor.GetEventDocuments(1, count);
            var eventUnits = this.ConvertToBytesList(eventDocuments, id);

            var snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == 0);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var testUnits = eventUnits.GetRange(0, 20).ToList();

            await accountSubActor.OnNext(new Immutable<List<byte[]>>(testUnits));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Count);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Count);

            await accountSubActor.OnNext(new Immutable<List<byte[]>>(testUnits));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Count);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Count);

            // Auto retrieve event
            testUnits = eventUnits.GetRange(30, 20).ToList();
            var testDocuments = eventDocuments.ToList().GetRange(30, 20).ToList();
            await accountSubActor.OnNext(new Immutable<List<byte[]>>(testUnits));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testDocuments.Max(e => e.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testDocuments.Max(e => e.Version));

            // Re enter the test
            testUnits = eventUnits.GetRange(40, count - 40).ToList();
            testDocuments = eventDocuments.ToList().GetRange(40, count - 40).ToList();
            await accountSubActor.OnNext(new Immutable<List<byte[]>>(testUnits));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testDocuments.Max(e => e.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            Assert.True(snapshot.Version == count);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testDocuments.Max(e => e.Version));
        }

        [Theory]
        [InlineData(100, 10300, true)]
        [InlineData(498, 10301, false)]
        [InlineData(888, 10302, false)]
        [InlineData(1800, 10303, true)]
        public async Task OnNext(int count, int id, bool current)
        {
            decimal topupAmount = 100;
            var guids = Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()).ToList();
            var accountActor = this.cluster.GrainFactory.GetGrain<IAccount>(id);
            var accountSubActor = this.cluster.GrainFactory.GetGrain<IAccountSubTest>(id);

            var isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent);
            await accountSubActor.SetConcurrentHandle(current);
            isCurrent = await accountSubActor.IsConcurrentHandle();
            Assert.True(isCurrent == current);

            await Task.WhenAll(Enumerable.Range(0, count).Select(i => accountActor.TopUp(topupAmount, guids[i])));

            var eventDocuments = await accountActor.GetEventDocuments(1, count);
            var eventUnits = this.ConvertToBytesList(eventDocuments, id);

            var snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == 0);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var testUnits = eventUnits.GetRange(0, 20).ToList();

            await Task.WhenAll(testUnits.Select(item => accountSubActor.OnNext(new Immutable<byte[]>(item))));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Count);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            var executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Count);

            await Task.WhenAll(testUnits.Select(item => accountSubActor.OnNext(new Immutable<byte[]>(item))));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testUnits.Count);
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testUnits.Count);

            // Auto retrieve event
            testUnits = eventUnits.GetRange(30, 20).ToList();
            var testDocuments = eventDocuments.ToList().GetRange(30, 20).ToList();
            await Task.WhenAll(testUnits.Select(item => accountSubActor.OnNext(new Immutable<byte[]>(item))));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testDocuments.Max(e => e.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testDocuments.Max(e => e.Version));

            // Re enter the test
            testUnits = eventUnits.GetRange(40, count - 40).ToList();
            testDocuments = eventDocuments.ToList().GetRange(40, count - 40).ToList();
            await Task.WhenAll(testUnits.Select(item => accountSubActor.OnNext(new Immutable<byte[]>(item))));
            snapshot = await accountSubActor.GetSnapshot();
            Assert.True(snapshot.Version == testDocuments.Max(e => e.Version));
            Assert.True(snapshot.Version == snapshot.DoingVersion);
            Assert.True(snapshot.Version == count);
            executedTimes = await accountSubActor.GetExecutedTimes();
            Assert.True(executedTimes == testDocuments.Max(e => e.Version));
        }

        private List<byte[]> ConvertToBytesList(IList<EventDocumentDto> eventDocuments, long id)
        {
            return eventDocuments.Select(document => this.ConvertToBytes(document, id)).ToList();
        }

        private byte[] ConvertToBytes(EventDocumentDto document, long id)
        {
            var meta = new EventMeta { Version = document.Version, Timestamp = document.Timestamp, FlowId = document.FlowId };
            using var baseBytes = EventExtensions.ConvertToBytes(meta);
            var transUnit = new EventTransUnit(document.Name, id, baseBytes.AsSpan(), Encoding.UTF8.GetBytes(document.Data));
            using var buffer = EventConverter.ConvertToBytes(transUnit);
            return buffer.ToArray();
        }

        private List<EventUnit<long>> ConvertToEventUnitList(IList<EventDocumentDto> documents, long id)
        {
            return documents.Select(document => this.ConvertToEventUnit(document, id)).ToList();
        }

        private EventUnit<long> ConvertToEventUnit(EventDocumentDto document, long id)
        {
            if (!this.eventTypeContainer.TryGet(document.Name, out var type))
            {
                throw new NoNullAllowedException($"event name of {document.Name}");
            }
            var data = this.serializer.Deserialize(document.Data, type);
            return new EventUnit<long>
            {
                ActorId = id,
                Event = data as IEvent,
                Meta = new EventMeta { Version = document.Version, Timestamp = document.Timestamp, FlowId = document.FlowId }
            };
        }
    }
}
