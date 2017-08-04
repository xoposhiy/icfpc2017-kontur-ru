﻿using System.Collections.Generic;
using lib.Scores;

namespace lib
{
    public class GameSimulatorRunner
    {
        private readonly IScoreCalculator scoreCalculator;
        public GameSimulatorRunner(IScoreCalculator scoreCalculator)
        {
            this.scoreCalculator = scoreCalculator;
        }

        public List<GameSimulationResult> SimulateGame(List<IAi> gamers, Map map)
        {
            var gameSimulator = new GameSimulator(map);
            gameSimulator.StartGame(gamers);
            var state = gameSimulator.NextMove();
            while (!state.IsGameOver)
            {
                state = gameSimulator.NextMove();
            }

            var results = new List<GameSimulationResult>();
            for (int i = 0; i < gamers.Count; i++)
            {
                results.Add(new GameSimulationResult(gamers[i], scoreCalculator.GetScore(i, map))); 
            }

            return results;
        }
    }

    public class GameSimulationResult
    {
        public GameSimulationResult(IAi gamer, int gamerScore)
        {
            Gamer = gamer;
            Score = gamerScore;
        }

        public IAi Gamer { get; }
        public int Score { get; }
    }
    
}