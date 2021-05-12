using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Processes;
using NzbDrone.Integration.Test.Client;
using NzbDrone.SignalR;
using NzbDrone.Test.Common.Categories;
using Prowlarr.Api.V1.Config;
using Prowlarr.Api.V1.History;
using Prowlarr.Api.V1.System.Tasks;
using Prowlarr.Api.V1.Tags;
using RestSharp;
using RestSharp.Serializers.SystemTextJson;

namespace NzbDrone.Integration.Test
{
    [IntegrationTest]
    public abstract class IntegrationTestBase
    {
        protected RestClient RestClient { get; private set; }

        public CommandClient Commands;
        public ClientBase<TaskResource> Tasks;
        public ClientBase<HistoryResource> History;
        public ClientBase<HostConfigResource> HostConfig;
        public IndexerClient Indexers;
        public LogsClient Logs;
        public NotificationClient Notifications;

        //public ReleaseClient Releases;
        public ClientBase<TagResource> Tags;

        private List<SignalRMessage> _signalRReceived;

        private HubConnection _signalrConnection;

        protected IEnumerable<SignalRMessage> SignalRMessages => _signalRReceived;

        public IntegrationTestBase()
        {
            new StartupContext();

            LogManager.Configuration = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget { Layout = "${level}: ${message} ${exception}" };
            LogManager.Configuration.AddTarget(consoleTarget.GetType().Name, consoleTarget);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));
        }

        public string TempDirectory { get; private set; }

        public abstract string MovieRootFolder { get; }

        protected abstract string RootUrl { get; }

        protected abstract string ApiKey { get; }

        protected abstract void StartTestTarget();

        protected abstract void InitializeTestTarget();

        protected abstract void StopTestTarget();

        [OneTimeSetUp]
        public void SmokeTestSetup()
        {
            StartTestTarget();
            InitRestClients();
            InitializeTestTarget();
        }

        protected virtual void InitRestClients()
        {
            RestClient = new RestClient(RootUrl + "api/v1/");
            RestClient.AddDefaultHeader("Authentication", ApiKey);
            RestClient.AddDefaultHeader("X-Api-Key", ApiKey);
            RestClient.UseSystemTextJson();

            Commands = new CommandClient(RestClient, ApiKey);
            Tasks = new ClientBase<TaskResource>(RestClient, ApiKey, "system/task");
            History = new ClientBase<HistoryResource>(RestClient, ApiKey);
            HostConfig = new ClientBase<HostConfigResource>(RestClient, ApiKey, "config/host");
            Indexers = new IndexerClient(RestClient, ApiKey);
            Logs = new LogsClient(RestClient, ApiKey);
            Notifications = new NotificationClient(RestClient, ApiKey);

            //Releases = new ReleaseClient(RestClient, ApiKey);
            Tags = new ClientBase<TagResource>(RestClient, ApiKey);
        }

        [OneTimeTearDown]
        public void SmokeTestTearDown()
        {
            StopTestTarget();
        }

        [SetUp]
        public void IntegrationSetUp()
        {
            TempDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "_test_" + ProcessProvider.GetCurrentProcessId() + "_" + DateTime.UtcNow.Ticks);

            // Wait for things to get quiet, otherwise the previous test might influence the current one.
            Commands.WaitAll();
        }

        [TearDown]
        public async Task IntegrationTearDown()
        {
            if (_signalrConnection != null)
            {
                await _signalrConnection.StopAsync();

                _signalrConnection = null;
                _signalRReceived = new List<SignalRMessage>();
            }

            if (Directory.Exists(TempDirectory))
            {
                try
                {
                    Directory.Delete(TempDirectory, true);
                }
                catch
                {
                }
            }
        }

        public string GetTempDirectory(params string[] args)
        {
            var path = Path.Combine(TempDirectory, Path.Combine(args));

            Directory.CreateDirectory(path);

            return path;
        }

        protected async Task ConnectSignalR()
        {
            _signalRReceived = new List<SignalRMessage>();
            _signalrConnection = new HubConnectionBuilder().WithUrl("http://localhost:9696/signalr/messages").Build();

            var cts = new CancellationTokenSource();

            _signalrConnection.Closed += e =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            };

            _signalrConnection.On<SignalRMessage>("receiveMessage", (message) =>
            {
                _signalRReceived.Add(message);
            });

            var connected = false;
            var retryCount = 0;

            while (!connected)
            {
                try
                {
                    Console.WriteLine("Connecting to signalR");

                    await _signalrConnection.StartAsync();
                    connected = true;
                    break;
                }
                catch (Exception)
                {
                    if (retryCount > 25)
                    {
                        Assert.Fail("Couldn't establish signalR connection");
                    }
                }

                retryCount++;
                Thread.Sleep(200);
            }
        }

        public static void WaitForCompletion(Func<bool> predicate, int timeout = 10000, int interval = 500)
        {
            var count = timeout / interval;
            for (var i = 0; i < count; i++)
            {
                if (predicate())
                {
                    return;
                }

                Thread.Sleep(interval);
            }

            if (predicate())
            {
                return;
            }

            Assert.Fail("Timed on wait");
        }

        public TagResource EnsureTag(string tagLabel)
        {
            var tag = Tags.All().FirstOrDefault(v => v.Label == tagLabel);

            if (tag == null)
            {
                tag = Tags.Post(new TagResource { Label = tagLabel });
            }

            return tag;
        }

        public void EnsureNoTag(string tagLabel)
        {
            var tag = Tags.All().FirstOrDefault(v => v.Label == tagLabel);

            if (tag != null)
            {
                Tags.Delete(tag.Id);
            }
        }
    }
}
