using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Simulation.Entities;

namespace Visualization
{
    /// <summary>
    /// Interaction logic for GraphsWindow.xaml
    /// </summary>
    public partial class GraphsWindow : Window
    {
        private readonly Thread _thread;

        private bool _threadRun;

        private static string GenerateTimeString(double simulationTimeMinutes)
        {
            var minutes = (int)(simulationTimeMinutes % 60);
            var hours = (int)(simulationTimeMinutes % (60 * 24) / 60);
            var days = (int)(simulationTimeMinutes / (60 * 24));
            return $"Simulation Time: Days: {days}, Hours: {hours}, Minutes: {minutes}";
        }

        private enum CreatureType
        {
            Rabbits,
            Wolves
        }

        private static string GenerateCreatureGenerationString(CreatureType creatureType, uint generation)
        {
            return $"{creatureType} Generation: {generation}";
        }

        private void RunUpdateStatus()
        {
            var timeout = 1000 / 5;
            var world = _simulation.World;
            double simulationTimeMinutes;
            uint rabbitsGeneration = 0;
            uint wolvesGeneration = 0;
            _threadRun = true;
            while (_threadRun)
            {
                Thread.Sleep(timeout);
                simulationTimeMinutes = world.StartTime != default(DateTime) ? (DateTime.Now - world.StartTime).TotalMilliseconds * world.WorldConfig.TimeRate / (1000 * 60) : 0;

                var rabbits = world.GetAllEntities().OfType<Rabbit>();
                if (rabbits.Any())
                {
                    rabbitsGeneration = (uint)rabbits.Max<Rabbit>(rabbit => rabbit.Generation);
                }

                var wolves = world.GetAllEntities().OfType<Wolf>();
                if (wolves.Any())
                {
                    wolvesGeneration = (uint)wolves.Max<Wolf>(wolf => wolf.Generation);
                }

                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        SimulationTimeText.Text = GenerateTimeString(simulationTimeMinutes);
                        RabbitsGenerationText.Text = GenerateCreatureGenerationString(CreatureType.Rabbits, rabbitsGeneration);
                        WolvesGenerationText.Text = GenerateCreatureGenerationString(CreatureType.Wolves, wolvesGeneration);
                    });
                }
                catch (TaskCanceledException)
                {
                }

                _rabbitsData.Points.Add(new DataPoint(simulationTimeMinutes, world.GetAllEntities().OfType<Rabbit>().Where(rabbit => rabbit.IsAlive).Count()));
                _wolvesData.Points.Add(new DataPoint(simulationTimeMinutes, world.GetAllEntities().OfType<Wolf>().Where(wolf => wolf.IsAlive).Count()));
                RabbitsPlot.InvalidatePlot(true);
                WolvesPlot.InvalidatePlot(true);
            }
        }

        private static (Axis, Axis) GenerateAxesFor(string name)
        {
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 5000,
                Title = "Time [minutes in simulation]"
            };

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 50, //World.Instance.WorldConfig.MaxCreatures;
                Title = name
            };

            return (xAxis, yAxis);
        }

        public GraphsWindow(Simulation.Simulation simulation)
        {
            InitializeComponent();
            DataContext = this;

            _simulation = simulation;

            _rabbitsData = new LineSeries();
            _wolvesData = new LineSeries();

            RabbitsPlot = new PlotModel
            {
                Title = "Rabbits",
            };

            var rabbitAxes = GenerateAxesFor("Rabbits");
            RabbitsPlot.Axes.Add(rabbitAxes.Item1);
            RabbitsPlot.Axes.Add(rabbitAxes.Item2);

            RabbitsPlot.Series.Add(_rabbitsData);

            WolvesPlot = new PlotModel
            {
                Title = "Wolves"
            };

            var wolvesAxes = GenerateAxesFor("Wolves");
            WolvesPlot.Axes.Add(wolvesAxes.Item1);
            WolvesPlot.Axes.Add(wolvesAxes.Item2);

            WolvesPlot.Series.Add(_wolvesData);

            _thread = new Thread(RunUpdateStatus)
            {
                IsBackground = true
            };
            _thread.Start();
        }

        private readonly Simulation.Simulation _simulation;

        private readonly LineSeries _rabbitsData;

        private readonly LineSeries _wolvesData;

        public PlotModel RabbitsPlot { get; private set; }

        public PlotModel WolvesPlot { get; private set; }

        public void StopAndClose()
        {
            Close();
            _threadRun = false;
        }
    }
}