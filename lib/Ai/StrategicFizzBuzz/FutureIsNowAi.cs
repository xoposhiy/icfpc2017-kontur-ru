using lib.Strategies;
using lib.Strategies.EdgeWeighting;
using static lib.Strategies.EdgeWeighting.MetaStrategyHelpers;

namespace lib.Ai.StrategicFizzBuzz
{
    public class FutureIsNowAi : CompositeStrategicAi
    {
        public FutureIsNowAi() : this(1, 100)
        {
        }

        public FutureIsNowAi(double pathMultiplier, double mineWeight)
            : base(
                (state, services) => new FutureIsNowSetupStrategy(pathMultiplier, state, services),
                (state, services) => new FutureIsNowStrategy(state, services),
                BiggestComponentEWStrategy((state, services) => new MaxVertextWeighter(mineWeight, state, services)))
        {
        }

        public override string Version => "1";
    }
}