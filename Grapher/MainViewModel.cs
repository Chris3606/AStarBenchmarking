using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.IO;
using System.Linq;

namespace Grapher
{
	class Record
	{
		public readonly string Algorithm;
		public readonly int MapSize;
		public readonly string MapGenAlgo;
		public readonly string DistanceCalc;
		public readonly string Heuristic;

		public readonly double Time;

		public Record(string algorithm, int mapSize, string mapGenAlgo, string distanceCalc, string heuristic, double time)
		{
			Algorithm = algorithm;
			MapSize = mapSize;
			MapGenAlgo = mapGenAlgo;
			DistanceCalc = distanceCalc;
			Heuristic = heuristic;
			Time = time;
		}
	}

	public class MainViewModel
	{
		public static readonly string INPUT_FILE = "results_parsable.log";
		public PlotModel MyModel { get; private set; }
		public MainViewModel()
		{
			this.MyModel = new PlotModel
			{
				Title = "AStar Comparison",
				LegendPlacement = LegendPlacement.Outside,
				LegendPosition = LegendPosition.BottomCenter,
				LegendOrientation = LegendOrientation.Horizontal,
				LegendBorderThickness = 0
			};

			var records = ParseData();
			var recordsByAlgorithm = records.GroupBy(r => r.Algorithm);

			var categoriesHash = new HashSet<string>();
			var categories = new List<string>();
			foreach (var algoGroup in recordsByAlgorithm)
			{
				var series = new BarSeries
				{
					Title = algoGroup.Key,
					StrokeColor = OxyColors.Black,
					StrokeThickness = 1
				};

				var orderedRecords = algoGroup.OrderBy(r => r.MapSize).ThenBy(r => r.MapGenAlgo).ThenBy(r => r.DistanceCalc).
									  ThenBy(r => r.Heuristic).ThenBy(r => r.Time);

				foreach (var record in orderedRecords)
				{
					// Add categories in order for labelling later
					string category = $"{record.MapSize.ToString()}{record.MapGenAlgo}{record.DistanceCalc}{record.Heuristic}";
					if (!categoriesHash.Contains(category))
					{
						categoriesHash.Add(category);
						categories.Add(category);
					}

					series.Items.Add(new BarItem { Value = record.Time });
				}

				MyModel.Series.Add(series);
			}

			var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
			foreach (var category in categories)
			{
				categoryAxis.Labels.Add(category);
			}

			var valueAxis = new LinearAxis
			{
				Position = AxisPosition.Bottom,
				MinimumPadding = 0,
				MaximumPadding = 0.06,
				AbsoluteMinimum = 0
			};

			MyModel.Axes.Add(categoryAxis);
			MyModel.Axes.Add(valueAxis);


			// Debug Calcs
			System.Console.WriteLine("Totals over entire runtime: ");
			foreach (var timeTuple in recordsByAlgorithm.
					Select(group => (algo: group.Key, time: group.Sum(rec => rec.Time))).OrderBy(tup => tup.time))
				System.Console.WriteLine($"{timeTuple.algo}: {timeTuple.time}");

			foreach (var sizeRecs in records.GroupBy(rec => rec.MapSize))
			{
				System.Console.WriteLine($"\nTotals for map size {sizeRecs.Key}:");

				foreach (var timeTuple in sizeRecs.GroupBy(rec => rec.Algorithm).
						Select(group => (algo: group.Key, time: group.Sum(rec => rec.Time))).OrderBy(tup => tup.time))
					System.Console.WriteLine($"\t{timeTuple.algo}: {timeTuple.time}");
			}

			foreach (var sizeRecs in records.GroupBy(rec => rec.MapSize))
			{
				System.Console.WriteLine($"\nTotals for map size by by heur {sizeRecs.Key}:");

				foreach (var heurRecs in sizeRecs.GroupBy(rec => rec.Heuristic))
				{
					System.Console.WriteLine($"\tHeuristic {heurRecs.Key}:");
					foreach (var timeTuple in heurRecs.GroupBy(rec => rec.Algorithm).
						Select(group => (algo: group.Key, time: group.Sum(rec => rec.Time))).OrderBy(tup => tup.time))
						System.Console.WriteLine($"\t\t{timeTuple.algo}: {timeTuple.time}");
				}
			}
		}

		private List<Record> ParseData()
		{
			var records = new List<Record>();
			foreach (var line in File.ReadLines(INPUT_FILE).Skip(2))
			{
				var splitString = line.Split('|').Where(i => i.Length != 0).ToList();
				var algo = splitString[0].Trim(' ', '\t', '\n', '\r', '\'').Substring(8);
				var mapSize = int.Parse(splitString[1].Trim());
				var mapGenAlgo = splitString[2].Trim().Substring(0, 3);
				var distance = splitString[3].Trim().Substring(0, 3);
				var heuristic = splitString[4].Trim().Substring(0, 3);
				var time = double.Parse(splitString[5].Trim().Split(' ')[0]);

				records.Add(new Record(algo, mapSize, mapGenAlgo, distance, heuristic, time));
			}

			return records;
		}
	}
}
