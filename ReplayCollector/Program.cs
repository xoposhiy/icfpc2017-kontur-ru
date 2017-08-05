﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using lib;
using lib.Ai;
using lib.Arena;
using lib.Interaction;
using lib.Replays;
using lib.viz;
using NLog;

namespace ReplayCollector
{
    internal class Program
    {
        private static readonly ArenaApi ArenaApi = new ArenaApi();
        private static readonly ReplayRepo Repo = new ReplayRepo();
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        
        private static int threadCount;
        private static readonly object Locker = new object();

        private static int completedTasksCount = 0;
        
        public static void Main(string[] args)
        {
            log.Info("Hello");
            threadCount = args.Length == 1 ? int.Parse(args.First()) : 1;
            
            for (var i = 0; i < threadCount; i++)
            {
                var index = i;
                
                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            ArenaMatch match;
                            IAi ai;

                            IServerInteraction interaction;

                            lock (Locker)
                            {
                                match = ArenaApi.GetNextMatch();
                                ai = AiFactoryRegistry.GetNextAi();

                                if (match == null) return;

                                log.Info(
                                    "Collector " + index + ": Match on port " + match.Port + " for " + ai.Name +
                                    " AI...");

                                interaction = new OnlineInteraction(match.Port);
                                interaction.Start();
                            }

                            var metaAndData = interaction.RunGame(ai);

                            Repo.SaveReplay(metaAndData.Item1, metaAndData.Item2);

                            ++completedTasksCount;

                            log.Info("Collector " + index + ": " + completedTasksCount + " replays collected");
                        }
                        catch (Exception e)
                        {
                            log.Error(e, "Collector " + index + " failed: " + e);
                        }
                    }
                });
            }

            Console.ReadLine();
        }
    }
}