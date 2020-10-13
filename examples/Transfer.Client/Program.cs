using IdGen;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Transfer.IGrains.Common;
using Transfer.IGrains.DTx;

namespace Transfer.Client
{
    class Program
    {
        readonly static IdGenerator idGen = new IdGenerator(0);
        static async Task Main(string[] args)
        {
            using var client = await StartClientWithRetries();

            while (true)
            {
                Console.WriteLine("Please select the type(1:normal,2:DTx)");
                var type = int.Parse(Console.ReadLine() ?? "1");
                if (type == 1)
                    await Normal(client);
                else if (type == 2)
                    await DTx(client);
            }
        }
        private static async Task Normal(IClusterClient client)
        {
            try
            {
                Console.WriteLine("Please enter the number of account");
                var accountCount = int.Parse(Console.ReadLine() ?? "10");
                Console.WriteLine("Please enter the number of executions");
                var times = int.Parse(Console.ReadLine() ?? "10");
                var topupWatch = new Stopwatch();
                var topupTaskList = new List<Task>();
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    topupTaskList.AddRange(Enumerable.Range(0, times).Select(x => client.GetGrain<IAccount>(account).TopUp(100, idGen.CreateId().ToString())));
                }
                topupWatch.Start();
                await Task.WhenAll(topupTaskList);
                topupWatch.Stop();
                Console.WriteLine($"{times * accountCount} Recharge completed, taking: {topupWatch.ElapsedMilliseconds}ms");
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    Console.WriteLine($"The balance of account {account} is{await client.GetGrain<IAccount>(account).GetBalance()}");
                }
                var transferWatch = new Stopwatch();
                var transferTaskList = new List<Task>();
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    transferTaskList.AddRange(Enumerable.Range(0, times).Select(x => client.GetGrain<IAccount>(account).Transfer(account + accountCount, 50, idGen.CreateId().ToString())));
                }
                transferWatch.Start();
                await Task.WhenAll(transferTaskList);
                transferWatch.Stop();
                Console.WriteLine(
                    $"{times * accountCount}The transfer is completed, taking: {transferWatch.ElapsedMilliseconds}ms");
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    Console.WriteLine($"The balance of account {account} is{await client.GetGrain<IAccount>(account).GetBalance()}");
                }
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    Console.WriteLine($"The balance of account {account} is{await client.GetGrain<IAccount>(account + accountCount).GetBalance()}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private static async Task DTx(IClusterClient client)
        {
            try
            {
                Console.WriteLine("Please enter the number of account");
                var accountCount = int.Parse(Console.ReadLine() ?? "10");
                Console.WriteLine("Please enter the number of executions");
                var times = int.Parse(Console.ReadLine() ?? "10");
                var topupWatch = new Stopwatch();
                var topupTaskList = new List<Task>();
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    topupTaskList.AddRange(Enumerable.Range(0, times).Select(x => client.GetGrain<IDTxAccount>(account).TopUp(100, idGen.CreateId().ToString())));
                }
                topupWatch.Start();
                await Task.WhenAll(topupTaskList);
                topupWatch.Stop();
                Console.WriteLine($"{times * accountCount} Recharge completed, taking: {topupWatch.ElapsedMilliseconds}ms");
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    Console.WriteLine($"The balance of account {account} is{await client.GetGrain<IDTxAccount>(account).GetBalance()}");
                }
                var transferWatch = new Stopwatch();
                var transferTaskList = new List<Task<bool>>();

                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    var txUnit = client.GetGrain<ITransferDtxUnit>(account / 50);
                    transferTaskList.AddRange(Enumerable.Range(0, times).Select(x => txUnit.Ask(new TransferRequest
                    {
                        FromId = account,
                        ToId = account + accountCount,
                        Amount = 50,
                        Id = idGen.CreateId().ToString()
                    })));
                }
                transferWatch.Start();
                await Task.WhenAll(transferTaskList);
                transferWatch.Stop();
                Console.WriteLine(
                    $"{times * accountCount}The transfer is completed, taking: {transferWatch.ElapsedMilliseconds}ms");
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    Console.WriteLine($"The balance of account {account} is{await client.GetGrain<IDTxAccount>(account).GetBalance()}");
                }
                foreach (var account in Enumerable.Range(0, accountCount))
                {
                    Console.WriteLine($"The balance of account {account} is{await client.GetGrain<IDTxAccount>(account + accountCount).GetBalance()}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private static async Task<IClusterClient> StartClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            int attempt = 0;
            IClusterClient client;
            while (true)
            {
                try
                {
                    var builder = new ClientBuilder()
                        .UseLocalhostClustering()
                        .ConfigureApplicationParts(parts =>
                            parts.AddApplicationPart(typeof(IAccount).Assembly).WithReferences())
                        .ConfigureLogging(logging => logging.AddConsole());
                    client = builder.Build();
                    await client.Connect();
                    Console.WriteLine("Client successfully connect to silo host");
                    break;
                }
                catch (Exception)
                {
                    attempt++;
                    Console.WriteLine(
                        $"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }

            return client;
        }
    }
}
