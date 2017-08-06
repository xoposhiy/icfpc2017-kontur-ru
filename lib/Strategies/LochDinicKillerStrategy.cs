using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using lib.Ai;
using lib.GraphImpl;
using lib.StateImpl;

namespace lib.Strategies
{
    public class LochDinicKillerStrategy : IStrategy
    {
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        private readonly Dictionary<Edge, double> edgesToBlock = new Dictionary<Edge, double>();

        public LochDinicKillerStrategy(State state, IServices services)
        {
            Graph = services.Get<Graph>();
            PunterId = state.punter;
        }

        private Graph Graph { get; }
        private int PunterId { get; }

        public AiSetupDecision Setup()
        {
            return null;
        }

        public List<TurnResult> NextTurns()
        {
            Init();

            return Graph.Vertexes.Values
                .SelectMany(v => v.Edges)
                .Select(
                    edge => new TurnResult
                    {
                        Estimation = EstimateWeight(edge),
                        Move = AiMoveDecision.Claim(PunterId, edge.River.Source, edge.River.Target),
                    })
                .Where(river => river.Estimation > 0)
                .ToList();
        }

        private void Init()
        {
            var maxCount = 10;
            edgesToBlock.Clear();

            var mineToSave = Graph.Mines
                .Where(mine => mine.Value.Edges.All(edge => edge.Owner != PunterId))
                .FirstOrDefault(mine => mine.Value.Edges.Count(edge => edge.Owner < 0) < PunterId)
                .Value;
            if (mineToSave != null)
            {
                var edgeToSave = mineToSave.Edges.OrderBy(_ => Random.Value.Next()).FirstOrDefault(edge => edge.Owner < 0);
                if (edgeToSave != null)
                    edgesToBlock[edgeToSave] = 10;
            }

            var bannedMines = Graph.Mines
                .Where(mine => mine.Value.Edges.Select(edge => edge.Owner).Distinct().Count() == PunterId + 1)
                .Select(mine => mine.Key)
                .ToHashSet();

            var mines = Graph.Mines.Where(mine => mine.Value.Edges.Any(edge => edge.Owner < 0)).ToList();
            for (var i = 0; i < Math.Min(10, mines.Count * (mines.Count - 1)); i++)
            {
                var mine1 = mines[Math.Min(Random.Value.Next(mines.Count), mines.Count - 1)];
                var mine2 = mines[Math.Min(Random.Value.Next(mines.Count), mines.Count - 1)];
                while (mine2.Key == mine1.Key) mine2 = mines[Math.Min(Random.Value.Next(mines.Count), mines.Count - 1)];

                var dinic = new Dinic(Graph, PunterId, mine1.Key, mine2.Key, out var flow);
                if (flow == 0)
                    continue;
                if (flow > maxCount)
                    continue;

                foreach (var edge in dinic.GetMinCut().Select(edge1 => edge1))
                {
                    if (bannedMines.Contains(edge.From) || bannedMines.Contains(edge.To))
                        continue;
                    edgesToBlock[edge] = edgesToBlock.GetOrDefault(edge, 0) + 1.0 / flow;
                }
            }
        }

        private double EstimateWeight(Edge edge)
        {
            return edgesToBlock.GetOrDefault(edge, 0);
        }
    }
}