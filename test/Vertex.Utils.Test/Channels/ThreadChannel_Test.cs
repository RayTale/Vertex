using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Vertex.Utils.Channels;
using Xunit;

namespace Vertex.Utils.Test.Channels
{
    public class ThreadChannel_Test
    {
        private readonly ServiceProvider serviceProvider;

        public ThreadChannel_Test()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddTransient(typeof(ThreadChannel<>));
            this.serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void BindConsumer()
        {
            var channel = this.serviceProvider.GetService<ThreadChannel<string>>();
            channel.BindConsumer(list => Task.CompletedTask);
            var ex = Assert.Throws<RebindConsumerException>(() => { channel.BindConsumer(list => Task.CompletedTask); });
            Assert.True(ex is RebindConsumerException);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        public async Task WriteAndConsumer(int count)
        {
            var channel = this.serviceProvider.GetService<ThreadChannel<string>>();
            var consumerList = new List<string>();
            var consumerTask = new TaskCompletionSource();
            channel.BindConsumer(list =>
            {
                consumerList.AddRange(list);
                if (consumerList.Count >= count)
                {
                    _ = consumerTask.TrySetResult();
                }

                return Task.CompletedTask;
            });
            for (int i = 0; i < count; i++)
            {
                await channel.WriteAsync(i.ToString());
            }

            await Task.WhenAny(consumerTask.Task, Task.Delay(3 * 1000));
            Assert.True(consumerTask.Task.IsCompletedSuccessfully);
            Assert.True(consumerList.Count == count);
        }

        [Fact]
        public async Task Dispose()
        {
            var channel = this.serviceProvider.GetService<ThreadChannel<string>>();
            channel.BindConsumer(list => Task.CompletedTask);
            var waitTask = channel.WaitToReadAsync();
            channel.Dispose();
            var success = await waitTask;
            Assert.False(success);
            Assert.True(channel.IsDisposed);
        }

        [Fact]
        public async Task Sequence_Join()
        {
            var channel = this.serviceProvider.GetService<ThreadChannel<string>>();
            var channel_1 = this.serviceProvider.GetService<ThreadChannel<string>>();
            channel_1.BindConsumer(list => Task.CompletedTask, true);
            await Task.Delay(500);
            var ex = Assert.Throws<ArgumentException>(() => { channel.Join(channel_1); });
            Assert.True(ex is ArgumentException);
        }

        [Fact]
        public async Task Sequence_Consumer()
        {
            var channel = this.serviceProvider.GetService<ThreadChannel<string>>();
            var channel_1 = this.serviceProvider.GetService<ThreadChannel<string>>();
            var channel_2 = this.serviceProvider.GetService<ThreadChannel<string>>();
            var channel_3 = this.serviceProvider.GetService<ThreadChannel<string>>();
            var consumerTask = new TaskCompletionSource();
            var consumerTask_1 = new TaskCompletionSource();
            var consumerTask_2 = new TaskCompletionSource();
            var consumerTask_3 = new TaskCompletionSource();

            channel.BindConsumer(list =>
            {
                consumerTask.TrySetResult();
                return Task.CompletedTask;
            });
            channel_1.BindConsumer(list =>
            {
                consumerTask_1.TrySetResult();
                return Task.CompletedTask;
            }, false);
            channel_2.BindConsumer(list =>
            {
                consumerTask_2.TrySetResult();
                return Task.CompletedTask;
            }, false);
            channel_3.BindConsumer(list =>
            {
                consumerTask_3.TrySetResult();
                return Task.CompletedTask;
            }, false);
            channel.Join(channel_1);
            channel.Join(channel_2);
            channel.Join(channel_3);
            Assert.True(await channel_1.WriteAsync("test"));
            Assert.True(await channel_2.WriteAsync("test"));
            Assert.True(await channel_3.WriteAsync("test"));
            Assert.True(await channel.WriteAsync("test"));
            await Task.WhenAny(Task.WhenAll(consumerTask.Task, consumerTask_1.Task, consumerTask_2.Task, consumerTask_3.Task), Task.Delay(3 * 1000));
            Assert.True(consumerTask.Task.IsCompletedSuccessfully);
            Assert.True(consumerTask_1.Task.IsCompletedSuccessfully);
            Assert.True(consumerTask_2.Task.IsCompletedSuccessfully);
            Assert.True(consumerTask_3.Task.IsCompletedSuccessfully);
        }
    }
}
