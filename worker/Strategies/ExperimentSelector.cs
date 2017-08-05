﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace worker.Strategies
{
    class ExperimentSelector : IExperiment
    {
        public IEnumerable<Tuple<PlayerWithParams, long>> Play(Task task)
        {
            switch (task.Experiment)
            {
                case "DummyAI": return new DummyAIExperiment().Play(task);
                case "Test": return new TestExperiment().Play(task);
                case "Greedy": return new GreedyExperiment().Play(task);
                default: throw new Exception("Experiment type '" + task.Experiment + "' is not recognized");
            }
        }
    }
}