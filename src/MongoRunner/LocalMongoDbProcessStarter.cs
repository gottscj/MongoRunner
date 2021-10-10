using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using Mongo2Go.Helper;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;

namespace MongoRunner
{
    public class LocalMongoDbProcessStarter : IMongoDbProcessStarter
    {
        private const string ProcessReadyIdentifier = "waiting for connections";
        private const string Space = " ";
        private const string ReplicaSetName = "singleNodeReplSet";

        private const string ReplicaSetReadyIdentifier =
            "transition to primary complete; database writes are now permitted";

        /// <summary>
        /// Starts a new process. Process can be killed
        /// </summary>
        public IMongoDbProcess Start(string binariesDirectory, string dataDirectory, int port, bool singleNodeReplSet,
            string additionalMongodArguments,
            ushort singleNodeReplSetWaitTimeout = MongoDbDefaults.SingleNodeReplicaSetWaitTimeout,
            ILogger logger = null)
        {
            return Start(binariesDirectory, dataDirectory, port, false, singleNodeReplSet, additionalMongodArguments,
                singleNodeReplSetWaitTimeout, logger);
        }

        /// <summary>
        /// Starts a new process.
        /// </summary>
        public IMongoDbProcess Start(string binariesDirectory, string dataDirectory, int port, bool doNotKill,
            bool singleNodeReplSet, string additionalMongodArguments,
            ushort singleNodeReplSetWaitTimeout = MongoDbDefaults.SingleNodeReplicaSetWaitTimeout,
            ILogger logger = null)
        {
            string fileName = @"{0}{1}{2}".Formatted(binariesDirectory,
                System.IO.Path.DirectorySeparatorChar.ToString(), MongoDbDefaults.MongodExecutable);

            string arguments = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                ? @"--dbpath ""{0}"" --port {1} --bind_ip 127.0.0.1".Formatted(dataDirectory, port)
                : @"--tlsMode disabled --dbpath ""{0}"" --port {1} --bind_ip 127.0.0.1".Formatted(dataDirectory, port);

            arguments = singleNodeReplSet ? arguments + Space + "--replSet" + Space + ReplicaSetName : arguments;
            arguments += MongodArguments.GetValidAdditionalArguments(arguments, additionalMongodArguments);

            WrappedProcess wrappedProcess = ProcessControl.ProcessFactory(fileName, arguments);
            wrappedProcess.DoNotKill = doNotKill;

            ProcessOutput output =
                ProcessControl.StartAndWaitForReady(wrappedProcess, 5, ProcessReadyIdentifier, logger);
            if (singleNodeReplSet)
            {
                var replicaSetReady = false;

                // subscribe to output from mongod process and check for replica set ready message
                wrappedProcess
                    .OutputDataReceived += (_, args) => replicaSetReady |= !string.IsNullOrWhiteSpace(args.Data) &&
                                                                           args.Data.Contains(ReplicaSetReadyIdentifier,
                                                                               StringComparison.OrdinalIgnoreCase);
                MongoClient client =
                    new MongoClient(
                        "mongodb://127.0.0.1:{0}/?connect=direct;replicaSet={1}".Formatted(port, ReplicaSetName));
                var admin = client.GetDatabase("admin");
                var replConfig = new BsonDocument(new List<BsonElement>()
                {
                    new BsonElement("_id", ReplicaSetName),
                    new BsonElement("members",
                        new BsonArray {new BsonDocument {{"_id", 0}, {"host", "127.0.0.1:{0}".Formatted(port)}}})
                });
                
                var command = new BsonDocument("replSetInitiate", replConfig);
                try
                {
                    admin.RunCommand<BsonDocument>(command);
                }
                catch (MongoDB.Driver.MongoCommandException e)
                {
                    // dont throw if already initialized
                    if (!e.Message.Contains("Command replSetInitiate failed: already initialized"))
                    {
                        throw;
                    }
                    replicaSetReady = true;
                    logger.LogWarning(e.Message);
                }


                // wait until replica set is ready or until the timeout is reached
                SpinWait.SpinUntil(() => replicaSetReady, TimeSpan.FromSeconds(singleNodeReplSetWaitTimeout));

                if (!replicaSetReady)
                {
                    throw new TimeoutException(
                        $"Replica set initialization took longer than the specified timeout of {singleNodeReplSetWaitTimeout} seconds. Please consider increasing the value of {nameof(singleNodeReplSetWaitTimeout)}.");
                }

                // wait until transaction is ready or until the timeout is reached
                SpinWait.SpinUntil(() =>
                        client.Cluster.Description.Servers.Any(s =>
                            s.State == ServerState.Connected && s.IsDataBearing),
                    TimeSpan.FromSeconds(singleNodeReplSetWaitTimeout));

                if (!client.Cluster.Description.Servers.Any(s => s.State == ServerState.Connected && s.IsDataBearing))
                {
                    throw new TimeoutException(
                        $"Cluster readiness for transactions took longer than the specified timeout of {singleNodeReplSetWaitTimeout} seconds. Please consider increasing the value of {nameof(singleNodeReplSetWaitTimeout)}.");
                }
            }

            LocalMongoDbProcess mongoDbProcess = new LocalMongoDbProcess(wrappedProcess)
            {
                ErrorOutput = output.ErrorOutput,
                StandardOutput = output.StandardOutput
            };

            return mongoDbProcess;
        }
    }
}