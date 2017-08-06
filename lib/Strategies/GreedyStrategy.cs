﻿using System.Collections.Generic;
using System.Linq;
using lib.GraphImpl;
using lib.StateImpl;

namespace lib.Strategies
{
    public class GreedyStrategy : IStrategy
    {
        public GreedyStrategy(int punterId, MineDistCalculator mineDistCalculator)
        {
            PunterId = punterId;
            MineDistCalulator = mineDistCalculator;
        }

        private MineDistCalculator MineDistCalulator { get; }
        private int PunterId { get; }

        public List<TurnResult> Turn(State state, IServices services)
        {
            var graph = services.Get<GraphService>(state).Graph;

            var calculator = new ConnectedCalculator(graph, PunterId);
            var result = new List<TurnResult>();
            foreach (var vertex in graph.Vertexes.Values)
            foreach (var edge in vertex.Edges.Where(x => x.Owner == -1))
            {
                var fromMines = calculator.GetConnectedMines(edge.From);
                var toMines = calculator.GetConnectedMines(edge.To);
                var addScore = CalcVertexScore(toMines, fromMines, edge.From) + CalcVertexScore(fromMines, toMines, edge.To);
                result.Add(
                    new TurnResult
                    {
                        Estimation = addScore,
                        River = edge.River,
                    });
            }
            return result;
        }

        private long CalcVertexScore(HashSet<int> mineIds, HashSet<int> usedMineIds, int vertexId)
        {
            return mineIds.Where(x => !usedMineIds.Contains(x))
                .Select(mineId => MineDistCalulator.GetDist(mineId, vertexId))
                .Sum(x => x * x);
        }
    }
}