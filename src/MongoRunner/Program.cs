using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using Mongo2Go.Helper;

namespace MongoRunner
{
    class Program
    {
        private static readonly object ExitLock = new();
        private static bool _disposed;
        
        static void Main(string[] args)
        {
            Parser
                .Default
                .ParseArguments<CmdOptions>(args)
                .WithParsed(Run)
                .WithNotParsed(RunErrors);
        }

        static void Run(CmdOptions options)
        {
            var logLevel = options.Verbose ? LogLevel.Information : LogLevel.Warning;
            var logger = new ConsoleLogger(logLevel);
            var dataDir = options.DataDirectory;
            if (string.IsNullOrEmpty(dataDir))
            {
                var homePath = Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
                if (string.IsNullOrEmpty(homePath))
                {
                    throw new InvalidOperationException("Could not locate home path");
                }

                dataDir = Path.Combine(homePath, "mongodb", "data");
            }
            

            var mongod = Process.GetProcessesByName("mongod").FirstOrDefault();
            if (mongod != null)
            {
                logger.LogDirect("'mongod' already running. stopping it...");
                mongod.Kill();
                if (mongod.WaitForExit(1000))
                {
                    logger.LogDirect("successfully stopped 'mongod'");
                }
                else
                {
                    logger.LogDirect("Could not stop 'mongod', please try manually");
                    return;
                }
            }

            
            var mongoBinSearch = @"mongodb-windows*\bin"; // windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                mongoBinSearch = "mongodb-macos*/bin";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                mongoBinSearch = "mongodb-linux*/bin";
            }

            var mongoBin = FolderSearch.CurrentExecutingDirectory().FindFolder(mongoBinSearch);
            if (string.IsNullOrEmpty(mongoBin))
            {
                var mongoBinaryLocator = new MongoBinaryLocator(null, null);
                try
                {
                    mongoBin = mongoBinaryLocator.Directory;
                }
                catch
                {
                    // ignored
                }

                if (string.IsNullOrEmpty(mongoBin))
                {
                    logger.LogDirect($"Could not find mongdb binaries: searched: {mongoBinSearch}", LogLevel.Error);
                    return;    
                }
            }
            
            var fileSystem = new FileSystem();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                fileSystem.MakeFileExecutable(Path.Combine(mongoBin, MongoDbDefaults.MongodExecutable));
                fileSystem.MakeFileExecutable(Path.Combine(mongoBin, MongoDbDefaults.MongoExportExecutable));
                fileSystem.MakeFileExecutable(Path.Combine(mongoBin, MongoDbDefaults.MongoImportExecutable));
            }
            
            fileSystem.CreateFolder(dataDir);
            fileSystem.DeleteFile($"{dataDir}{Path.DirectorySeparatorChar}mongod.lock");
            
            var processStarter = new LocalMongoDbProcessStarter();
            IMongoDbProcess mongoDbProcess;
            
            try
            {
                mongoDbProcess = processStarter.Start(
                    binariesDirectory: mongoBin,
                    dataDir,
                    options.Port,
                    singleNodeReplSet: true,
                    additionalMongodArguments: null,
                    singleNodeReplSetWaitTimeout: 10,
                    logger: logger);
            }
            
            catch (Exception e)
            {
                logger.LogDirect(e.Message, LogLevel.Error);
                return;
            }
            logger.LogDirect("Successfully started 'mongod'...");
            logger.LogDirect($"ConnectionString:   'mongodb://127.0.0.1:{options.Port}/?connect=direct&replicaSet=singleNodeReplSet&readPreference=primary'");
            logger.LogDirect($"Directory:           {dataDir}");
            var exitSignal = new ManualResetEvent(false);
            
            AppDomain.CurrentDomain.ProcessExit += (_, _) => ShuttingDown(mongoDbProcess, exitSignal, logger);
            Console.CancelKeyPress += (_, _) => ShuttingDown(mongoDbProcess, exitSignal, logger);
            
            exitSignal.WaitOne();
        }

        static void RunErrors(IEnumerable<Error> errors)
        {
            
        }
        
        private static void ShuttingDown(
            IDisposable mongoDbProcess, 
            EventWaitHandle exitSignal,
            ConsoleLogger logger)
        {
            lock (ExitLock)
            {
                if(_disposed) return;

                mongoDbProcess?.Dispose();
                
                var mongoInstances = Process.GetProcessesByName("mongod");
                foreach (var process in mongoInstances)
                {
                    logger.LogDirect($"stopped process: {process.ProcessName}");
                    process.Kill();
                    process.WaitForExit(1000);
                }
                exitSignal.Set();
                _disposed = true;
            }
            
        }
    }
}