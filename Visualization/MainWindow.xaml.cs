using CsvHelper;
using Simulation;
using Simulation.Attributes;
using Simulation.Entities;
using SimulationStandard;
using SimulationStandard.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Visualization.CsvExport;

namespace Visualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string CLI_COMMAND_PREFIX = "--";

        private Simulation.Simulation? _simulation;

        private SimulationWindow? _simulationWindow;

        private GraphsWindow? _graphsWindow;

        // TODO Add check for invalid values (or too big/small)

        public MainWindow()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                if (args.Length % 2 == 1)
                {
                    Visibility = Visibility.Hidden;

                    try
                    {
                        ConsoleAllocator.ShowConsoleWindow();
                        RunCLI(args);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("CLI ERROR");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid number of command line arguments.");
                }
                Close();
            }
        }

        private void RunCLI(string[] args)
        {
            var simulationBuilder = new SimulationBuilder();
            var simulationParams = CreateConfigurationFromCliArgs(args);
            _simulation = (Simulation.Simulation)simulationBuilder.CreateSimulation(simulationParams);
            Console.WriteLine("Starting simulation.");
            var results = _simulation.Run();
            Console.WriteLine("Simulation has finished.");
            Console.WriteLine("Exporting results.");
            ExportResultsToCSV(results);
        }

        private ISimulationParams CreateConfigurationFromCliArgs(string[] args)
        {
            Console.WriteLine("Creating Config...");
            // Get possible settings
            var fields = typeof(SimulationBuilder.SimulationParamsEnum).GetFields();
            var fieldToTypeDict = new Dictionary<FieldInfo, Type>();
            foreach (var field in fields.Where(field => field.CustomAttributes.Any()))
            {
                fieldToTypeDict[field] = field.CustomAttributes
                    .FirstOrDefault(x => x.AttributeType.IsAssignableTo(typeof(TypeAttribute)))?
                    .ConstructorArguments.FirstOrDefault().Value as Type 
                    ?? throw new Exception();
            }

            var enumerator = args.ToList().GetEnumerator();
            // Skip first argument
            enumerator.MoveNext();

            var simulationParams = CreateDefaultSimulationParams();

            while (enumerator.MoveNext())
            {
                var setting = enumerator.Current;
                enumerator.MoveNext();
                var value = enumerator.Current;

                var field = fieldToTypeDict.Keys.FirstOrDefault(key => $"{CLI_COMMAND_PREFIX}{key.Name}" == setting) ?? throw new Exception();
                var settingType = fieldToTypeDict[field];

                switch (settingType.Name)
                {
                    case "Int32":
                        simulationParams.Params[field.Name] = int.Parse(value);
                        break;
                    case "Double":
                        simulationParams.Params[field.Name] = double.Parse(value);
                        break;
                    case "Boolean":
                        simulationParams.Params[field.Name] = bool.Parse(value);
                        break;
                }
            }

            return simulationParams;
        }

        private ISimulationParams CreateDefaultSimulationParams()
        {
            var @params = new SimulationParams();

            // Rabbits
            @params.Params[SimulationBuilder.SimulationParamsEnum.RabbitsInitialPopulation.ToString()] = 24;
            @params.Params[SimulationBuilder.SimulationParamsEnum.RabbitsMinChildren.ToString()] = 1;
            @params.Params[SimulationBuilder.SimulationParamsEnum.RabbitsMaxChildren.ToString()] = 6;
            @params.Params[SimulationBuilder.SimulationParamsEnum.RabbitsPregnancyDuration.ToString()] = 1;
            @params.Params[SimulationBuilder.SimulationParamsEnum.RabbitsLifeExpectancy.ToString()] = 10;
            
            // Wolves
            @params.Params[SimulationBuilder.SimulationParamsEnum.WolvesInitialPopulation.ToString()] = 12;
            @params.Params[SimulationBuilder.SimulationParamsEnum.WolvesMinChildren.ToString()] = 1;
            @params.Params[SimulationBuilder.SimulationParamsEnum.WolvesMaxChildren.ToString()] = 6;
            @params.Params[SimulationBuilder.SimulationParamsEnum.WolvesPregnancyDuration.ToString()] = 2;
            @params.Params[SimulationBuilder.SimulationParamsEnum.WolvesLifeExpectancy.ToString()] = 15;

            // Rest
            @params.Params[SimulationBuilder.SimulationParamsEnum.TimeRate.ToString()] = 1800;
            @params.Params[SimulationBuilder.SimulationParamsEnum.DeathFromOldAge.ToString()] = false;
            @params.Params[SimulationBuilder.SimulationParamsEnum.MaxCreatures.ToString()] = 200;
            @params.Params[SimulationBuilder.SimulationParamsEnum.FruitsPerDay.ToString()] = 60;
            @params.Params[SimulationBuilder.SimulationParamsEnum.MapSize.ToString()] = 800;
            @params.Params[SimulationBuilder.SimulationParamsEnum.MutationChance.ToString()] = 0.1;
            @params.Params[SimulationBuilder.SimulationParamsEnum.MutationImpact.ToString()] = 0.1;
            @params.Params[SimulationBuilder.SimulationParamsEnum.OffspringGenerationMethod.ToString()] = 0;

            return @params;
        }

        private void NumericTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void FloatTextBox(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                Regex regex = new Regex(@"^([0-9]+\.[0-9]*|[0-9]+)$");
                e.Handled = !regex.IsMatch(textBox.Text + e.Text);
            }
        }

        private void TimeRateChange(object sender, TextChangedEventArgs args)
        {
            Regex regex = new Regex("^0+$");
            if (TimeRateInput.Text.Length == 0 || regex.IsMatch(TimeRateInput.Text))
            {
                TimeRateLabel.Content = "Invalid Value!";
            }
            else
            {
                var timeRate = Double.Parse(TimeRateInput.Text);
                var newText = $"Time Rate ({3600 / timeRate} seconds = 1 real-time hour)";
                TimeRateLabel.Content = newText;
            }
        }

        private void StartSimulationClicked(object sender, RoutedEventArgs e)
        {
            ConfigGrid.ColumnDefinitions[0].IsEnabled = false;
            ConfigGrid.ColumnDefinitions[1].IsEnabled = false;
            StartStopButton.Content = "Stop";
            StartStopButton.Click -= StartSimulationClicked;
            StartStopButton.Click += StopSimulationClicked;

            var simulationBuilder = new SimulationBuilder();
            var simulationParams = CreateConfigFromUserInput();
            _simulation = (Simulation.Simulation)simulationBuilder.CreateSimulation(simulationParams);

            _simulationWindow = new SimulationWindow(_simulation);
            _graphsWindow = new GraphsWindow(_simulation);
            _graphsWindow.Show();
            _simulationWindow.Show();

            var task = new Task<Simulation.SimulationResults>(() => _simulation.Run());
            task.ContinueWith(t =>
            {
                OnSimulationStopped();

                ShowResults(t.Result);
            });
            task.Start();
        }

        private void ShowResults(Simulation.SimulationResults result)
        {
            Dispatcher.Invoke(() =>
            {
                var resultsWindow = new ResultsWindow(result);
                resultsWindow.ShowDialog();

                if (ExportResultsToCSVInput.IsChecked != null && ExportResultsToCSVInput.IsChecked.Value)
                    ExportResultsToCSV(result);
            });
        }

        private SimulationParams CreateConfigFromUserInput()
        {
            var rabbitsInitialPopulation = long.Parse(RabbitsInitialPopulationInput.Text);
            var rabbitsMinChildren = long.Parse(RabbitsMinChildrenInput.Text);
            var rabbitsMaxChildren = long.Parse(RabbitsMaxChildrenInput.Text);
            var rabbitsPregnancyDuration = long.Parse(RabbitsPregnancyDurationInput.Text);
            var rabbitsLifeExpectancy = long.Parse(RabbitsLifeExpectancy.Text);

            var wolvesInitialPopulation = long.Parse(WolvesInitialPopulationInput.Text);
            var wolvesMinChildren = long.Parse(WolvesMinChildrenInput.Text);
            var wolvesMaxChildren = long.Parse(WolvesMaxChildrenInput.Text);
            var wolvesPregnancyDuration = long.Parse(WolvesPregnancyDurationInput.Text);
            var wolvesLifeExpectancy = long.Parse(WolvesLifeExpectancy.Text);

            var timeRate = double.Parse(TimeRateInput.Text);
            var deathFromOldAge = (bool)DeathFromOldAgeInput.IsChecked!;
            var maxCreatures = long.Parse(MaxCreaturesInput.Text);
            var fruitsPerDay = long.Parse(FruitsPerDayInput.Text);
            var mapSize = long.Parse(MapSizeInput.Text);
            VisualizationConfig.Instance.DrawRanges = (bool)DrawRangesInput.IsChecked!;
            // TODO Fix
            var exportResultsToCSV = (bool)ExportResultsToCSVInput.IsChecked!;
            var mutationChance = double.Parse(MutationChanceInput.Text, CultureInfo.InvariantCulture);
            var mutationImpact = double.Parse(MutationImpactInput.Text, CultureInfo.InvariantCulture);

            var simulationParams = new SimulationParams();

            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.RabbitsInitialPopulation.ToString()] = rabbitsInitialPopulation;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.RabbitsMinChildren.ToString()] = rabbitsMinChildren;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.RabbitsMaxChildren.ToString()] = rabbitsMaxChildren;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.RabbitsPregnancyDuration.ToString()] = rabbitsPregnancyDuration;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.RabbitsLifeExpectancy.ToString()] = rabbitsLifeExpectancy;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.WolvesInitialPopulation.ToString()] = wolvesInitialPopulation;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.WolvesMinChildren.ToString()] = wolvesMinChildren;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.WolvesMaxChildren.ToString()] = wolvesMaxChildren;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.WolvesPregnancyDuration.ToString()] = wolvesPregnancyDuration;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.WolvesLifeExpectancy.ToString()] = wolvesLifeExpectancy;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.TimeRate.ToString()] = timeRate;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.DeathFromOldAge.ToString()] = deathFromOldAge;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.MaxCreatures.ToString()] = maxCreatures;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.FruitsPerDay.ToString()] = fruitsPerDay;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.MapSize.ToString()] = mapSize;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.MutationChance.ToString()] = mutationChance;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.MutationImpact.ToString()] = mutationImpact;
            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.OffspringGenerationMethod.ToString()] = (long)OffspringGenerationMethodInput.SelectedIndex;

            simulationParams.Params[SimulationBuilder.SimulationParamsEnum.Timeout.ToString()] = -1L;

            return simulationParams;
        }

        private void ExportResultsToCSV(Simulation.SimulationResults results)
        {
            var pathToFileFormat = $"results{Path.DirectorySeparatorChar}{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}--{OffspringGenerationMethodInput.Text}--{{0}}.csv";

            var pathToRabbitsFile = string.Format(pathToFileFormat, "Rabbits");
            var rabbitsFileInfo = new FileInfo(pathToRabbitsFile);
            if (rabbitsFileInfo.Directory != null && !rabbitsFileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(rabbitsFileInfo.DirectoryName!);
            }
            using var rabbitsWriter = new StreamWriter(pathToRabbitsFile);
            using var rabbitsCSV = new CsvWriter(rabbitsWriter, CultureInfo.InvariantCulture);
            rabbitsCSV.Context.RegisterClassMap<RabbitMap>();
            rabbitsCSV.WriteHeader<Rabbit>();
            rabbitsCSV.NextRecord();
            foreach (var rabbit in results.Rabbits)
            {
                rabbitsCSV.WriteRecord(rabbit);
                rabbitsCSV.NextRecord();
            }

            var pathToWolvesFile = string.Format(pathToFileFormat, "Wolves");
            var wolvesFileInfo = new FileInfo(pathToWolvesFile);
            if (wolvesFileInfo.Directory != null && !wolvesFileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(wolvesFileInfo.DirectoryName!);
            }
            using var wolvesWriter = new StreamWriter(pathToWolvesFile);
            using var wolvesCSV = new CsvWriter(wolvesWriter, CultureInfo.InvariantCulture);
            wolvesCSV.Context.RegisterClassMap<WolfMap>();
            wolvesCSV.WriteHeader<Wolf>();
            wolvesCSV.NextRecord();
            foreach (var wolf in results.Wolves)
            {
                wolvesCSV.WriteRecord(wolf);
                wolvesCSV.NextRecord();
            }
        }

        private void StopSimulationClicked(object sender, RoutedEventArgs e) => StopSimulation();
        private void StopSimulation() => _simulation?.Stop();

        private void OnSimulationStopped()
        {
            Dispatcher.Invoke(() =>
            {
                ConfigGrid.ColumnDefinitions[0].IsEnabled = true;
                ConfigGrid.ColumnDefinitions[1].IsEnabled = true;
                StartStopButton.Content = "Run";
                StartStopButton.Click -= StopSimulationClicked;
                StartStopButton.Click += StartSimulationClicked;

                _graphsWindow?.StopAndClose();
                _simulationWindow?.StopAndClose();
            });
        }
    }
}