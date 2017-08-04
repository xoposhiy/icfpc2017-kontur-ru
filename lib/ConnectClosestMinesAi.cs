using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using lib.GraphImpl;
using lib.viz;
using NUnit.Framework;
using Shouldly;

namespace lib
{
    public class ConnectClosestMinesAi : IAi
    {
        private readonly HashSet<int> myMines = new HashSet<int>();
        private int punterId;
        private MineDistCalculator mineDistCalulator;

        public string Name => nameof(ConnectClosestMinesAi);

        // ReSharper disable once ParameterHidesMember
        public void StartRound(int punterId, int puntersCount, Map map)
        {
            this.punterId = punterId;
            this.mineDistCalulator = new MineDistCalculator(new Graph(map));
        }

        public IMove GetNextMove(IMove[] prevMoves, Map map)
        {
            mineDistCalulator = mineDistCalulator ?? new MineDistCalculator(new Graph(map));

            var graph = new Graph(map);

            IMove move;

            if (TryExtendComponent(graph, out move))
                return move;

            if (TryBuildNewComponent(graph, out move))
                return move;

            if (TryExtendAnything(graph, out move))
                return move;

            return new Pass(punterId);
        }

        private bool TryExtendAnything(Graph graph, out IMove nextMove)
        {
            var calculator = new ConnectedCalculator(graph, punterId);
            var maxAddScore = long.MinValue;
            Edge bestEdge = null;
            foreach (var vertex in graph.Vertexes.Values)
            {
                foreach (var edge in vertex.Edges.Where(x => x.Owner == -1))
                {
                    var fromMines = calculator.GetConnectedMines(edge.From);
                    var toMines = calculator.GetConnectedMines(edge.To);
                    long addScore;
                    if (fromMines.Count == 0)
                        addScore = Calc(toMines, edge.From);
                    else
                    {
                        if (toMines.Count != 0)
                        {
                            if (!toMines.SetEquals(fromMines))
                                throw new InvalidOperationException("Attempt to connect two not empty components! WTF???");
                            addScore = 0;
                        }
                        else
                            addScore = Calc(fromMines, edge.To);
                    }
                    if (addScore > maxAddScore)
                    {
                        maxAddScore = addScore;
                        bestEdge = edge;
                    }
                }
            }
            if (bestEdge != null)
            {
                nextMove = MakeMove(bestEdge);
                return true;
            }
            nextMove = null;
            return false;
        }

        private bool TryExtendComponent(Graph graph, out IMove move)
        {
            var queue = new Queue<ExtendQueueItem>();
            var used = new HashSet<int>();
            foreach (var mineId in graph.Mines.Keys.Where(id => !myMines.Contains(id)))
            {
                var queueItem = new ExtendQueueItem
                {
                    CurrentVertex = graph.Vertexes[mineId],
                    Edge = null
                };
                queue.Enqueue(queueItem);
                used.Add(mineId);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.CurrentVertex.Edges.Any(x => x.Owner == punterId))
                {
                    if (graph.Mines.ContainsKey(current.Edge.From))
                        myMines.Add(current.Edge.From);
                    if (graph.Mines.ContainsKey(current.Edge.To))
                        myMines.Add(current.Edge.To);
                    move = MakeMove(current.Edge);
                    return true;
                }
                foreach (var edge in current.CurrentVertex.Edges.Where(x => x.Owner == -1))
                {
                    var next = graph.Vertexes[edge.To];
                    if (!used.Contains(next.Id))
                    {
                        var queueItem = new ExtendQueueItem
                        {
                            CurrentVertex = next,
                            Edge = edge
                        };
                        queue.Enqueue(queueItem);
                        used.Add(next.Id);
                    }
                }
            }
            move = null;
            return false;
        }

        private bool TryBuildNewComponent(Graph graph, out IMove move)
        {
            var queue = new Queue<BuildQueueItem>();
            var used = new Dictionary<int, BuildQueueItem>();
            foreach (var mineId in graph.Mines.Keys.Where(id => !myMines.Contains(id)))
            {
                var queueItem = new BuildQueueItem
                {
                    CurrentVertex = graph.Vertexes[mineId],
                    SourceMine = graph.Vertexes[mineId],
                    FirstEdge = null
                };
                queue.Enqueue(queueItem);
                used.Add(mineId, queueItem);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var edge in current.CurrentVertex.Edges.Where(x => x.Owner == -1))
                {
                    var next = graph.Vertexes[edge.To];
                    BuildQueueItem prev;
                    if (used.TryGetValue(next.Id, out prev))
                    {
                        if (prev.SourceMine != current.SourceMine)
                        {
                            var bestMine = SelectBestMine(prev.SourceMine, current.SourceMine);
                            myMines.Add(bestMine.Id);
                            if (bestMine == prev.SourceMine)
                            {
                                move = MakeMove(prev.FirstEdge ?? edge);
                                return true;
                            }
                            {
                                move = MakeMove(current.FirstEdge ?? edge);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        var queueItem = new BuildQueueItem
                        {
                            CurrentVertex = next,
                            SourceMine = current.SourceMine,
                            FirstEdge = current.FirstEdge ?? edge
                        };
                        queue.Enqueue(queueItem);
                        used.Add(next.Id, queueItem);
                    }
                }
            }
            move = null;
            return false;
        }

        private long Calc(HashSet<int> mineIds, int vertexId)
        {
            return mineIds.Sum(
                mineId =>
                {
                    var dist = mineDistCalulator.GetDist(mineId, vertexId);
                    return (long) dist * dist;
                });
        }

        private static Vertex SelectBestMine(Vertex a, Vertex b)
        {
            return a.Edges.Count(x => x.Owner == -1) < b.Edges.Count(x => x.Owner == -1) ? a : b;
        }

        private IMove MakeMove(Edge edge)
        {
            return new Move(punterId, edge.From, edge.To);
        }

        public string SerializeGameState()
        {
            return $"{punterId};{string.Join(";", myMines)}";
    }

        public void DeserializeGameState(string gameState)
        {
            var split = gameState.Split(';');
            punterId = int.Parse(split[0]);
            myMines.Clear();
            myMines.UnionWith(split.Skip(1).Select(int.Parse));
        }

        private class BuildQueueItem
        {
            public Vertex CurrentVertex;
            public Vertex SourceMine;
            public Edge FirstEdge;
        }

        private class ExtendQueueItem
        {
            public Vertex CurrentVertex;
            public Edge Edge;
        }
    }

    [TestFixture]
    public class ConnectClosestMinesAi_Should
    {
        [Test]
        public void Test1()
        {
            var map = MapLoader.LoadMap(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\maps\sample.json")).Map;
            var ai = new ConnectClosestMinesAi();
            ai.StartRound(0, 1, map);
            var move = ai.GetNextMove(null, map);
            move.ShouldBe(new Move(0, 5, 7));
        }

        [Test]
        public void Test2()
        {
            var map = MapLoader.LoadMap(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\maps\sample.json")).Map;
            var ai = new ConnectClosestMinesAi();
            var simulator = new GameSimulator(map);
            simulator.StartGame(new List<IAi> {ai});
            var gameState = simulator.NextMove();
            var move = ai.GetNextMove(null, gameState.CurrentMap);
            move.ShouldBe(new Move(0, 1, 7));
        }

        [Test]
        public void Test3()
        {
            var map = MapLoader.LoadMap(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\maps\sample.json")).Map;
            var ai = new ConnectClosestMinesAi();
            var simulator = new GameSimulator(map);
            simulator.StartGame(new List<IAi> {ai});
            simulator.NextMove();
            var gameState = simulator.NextMove();
            var move = ai.GetNextMove(null, gameState.CurrentMap);
            move.ShouldBe(new Move(0, 0, 1));
        }

        [Test]
        [STAThread]
        [Explicit]
        public void Show()
        {
            var form = new Form();
            var painter = new MapPainter();
            var map = MapLoader.LoadMap(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\maps\sample.json"));

            var ai = new ConnectClosestMinesAi();
            var simulator = new GameSimulator(map.Map);
            simulator.StartGame(new List<IAi> { ai });
            var gameState = simulator.NextMove();
            painter.Map = gameState.CurrentMap;

            var panel = new ScaledViewPanel(painter)
            {
                Dock = DockStyle.Fill
            };
            form.Controls.Add(panel);
            form.ShowDialog();
        }
    }
}