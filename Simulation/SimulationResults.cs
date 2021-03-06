using Simulation.Entities;

namespace Simulation;

public class SimulationResults : SimulationStandard.SimulationResults
{
    private readonly Simulation _simulation;

    public SimulationResults(Simulation simulation)
    {
        _simulation = simulation;
        CreateResultsObjects();
        SubscribeToSimulationEvents();
    }

    private void SubscribeToSimulationEvents()
    {
        _simulation.EntityAdded -= SimulationEntityAdded;
        _simulation.EntityAdded += SimulationEntityAdded;

        _simulation.EntityRemoved -= SimulationEntityRemoved;
        _simulation.EntityRemoved += SimulationEntityRemoved;
    }

    private void SimulationEntityRemoved(Entity obj)
    {
        CreateSimulationSnapshot();
    }

    private void SimulationEntityAdded(Entities.Entity entity)
    {
        CreateSimulationSnapshot();

        switch (entity)
        {
            case Creature creature:
                switch (creature)
                {
                    case Rabbit:
                        TotalRabbits++;
                        if (creature.Generation > RabbitsGenerations)
                            RabbitsGenerations = (int)creature.Generation;
                        break;
                    case Wolf:
                        TotalWolves++;
                        if (creature.Generation > WolvesGenerations)
                            WolvesGenerations = (int)creature.Generation;
                        break;
                }
                break;
            case Fruit:
                TotalFruits++;
                break;
        }
    }

    public void StoreInitialValues()
    {
        var allEntities = _simulation.World.GetAllEntities();

        AddTimestamp(0);
        AddRabbitsAlive(allEntities.Count(x => x is Rabbit));
        AddWolvesAlive(allEntities.Count(x => x is Wolf));
        AddFruitsPresent(allEntities.Count(x => x is Fruit));
    }

    private void CreateSimulationSnapshot()
    {
        var allEntities = _simulation.World.GetAllEntities();
        var timeStamp = _simulation.World.CurrentSimulationTime;

        AddTimestamp(timeStamp);
        AddRabbitsAlive(allEntities.Count(x => x is Rabbit));
        AddWolvesAlive(allEntities.Count(x => x is Wolf));
        AddFruitsPresent(allEntities.Count(x => x is Fruit));
    }

    private void CreateResultsObjects()
    {
        Results[SimulationBuilder.SimulationResultsEnum.SimulationTime.ToString()] = new List<long>();
        Results[SimulationBuilder.SimulationResultsEnum.RabbitsAlive.ToString()] = new List<long>();
        Results[SimulationBuilder.SimulationResultsEnum.WolvesAlive.ToString()] = new List<long>();
        Results[SimulationBuilder.SimulationResultsEnum.FruitsPresent.ToString()] = new List<long>();

        TotalRabbits = _simulation.World.GetAllEntities().OfType<Rabbit>().Count();
        TotalWolves = _simulation.World.GetAllEntities().OfType<Wolf>().Count();
        TotalFruits = 0;
        RabbitsGenerations = 0;
        WolvesGenerations = 0;
    }

    public long TotalRabbits { get => (long)Results[SimulationBuilder.SimulationResultsEnum.TotalRabbits.ToString()]; private set => Results[SimulationBuilder.SimulationResultsEnum.TotalRabbits.ToString()] = value; }
    public long TotalWolves { get => (long)Results[SimulationBuilder.SimulationResultsEnum.TotalWolves.ToString()]; private set => Results[SimulationBuilder.SimulationResultsEnum.TotalWolves.ToString()] = value; }
    public long TotalFruits { get => (long)Results[SimulationBuilder.SimulationResultsEnum.TotalFruits.ToString()]; private set => Results[SimulationBuilder.SimulationResultsEnum.TotalFruits.ToString()] = value; }
    public long RabbitsGenerations { get => (long)Results[SimulationBuilder.SimulationResultsEnum.RabbitsGenerations.ToString()]; private set => Results[SimulationBuilder.SimulationResultsEnum.RabbitsGenerations.ToString()] = value; }
    public long WolvesGenerations { get => (long)Results[SimulationBuilder.SimulationResultsEnum.WolvesGenerations.ToString()]; private set => Results[SimulationBuilder.SimulationResultsEnum.WolvesGenerations.ToString()] = value; }

    public IReadOnlyList<Wolf> Wolves => _simulation.World.GetLegacyCreatures().OfType<Wolf>().ToList().AsReadOnly();
    public IReadOnlyList<Rabbit> Rabbits => _simulation.World.GetLegacyCreatures().OfType<Rabbit>().ToList().AsReadOnly();

    private void AddTimestamp(long timestamp) => ((IList<long>)Results[SimulationBuilder.SimulationResultsEnum.SimulationTime.ToString()]).Add(timestamp);
    private void AddRabbitsAlive(long rabbitsAlive) => ((IList<long>)Results[SimulationBuilder.SimulationResultsEnum.RabbitsAlive.ToString()]).Add(rabbitsAlive);
    private void AddWolvesAlive(long wolvesAlive) => ((IList<long>)Results[SimulationBuilder.SimulationResultsEnum.WolvesAlive.ToString()]).Add(wolvesAlive);
    private void AddFruitsPresent(long fruitsPresent) => ((IList<long>)Results[SimulationBuilder.SimulationResultsEnum.FruitsPresent.ToString()]).Add(fruitsPresent);
}
