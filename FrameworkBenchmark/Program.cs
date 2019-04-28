using System;
using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GoRogue;
using GoRogue.MapGeneration;
using GoRogue.MapViews;
using GoRogue.Pathing;
using AStarImplementer;
using System.IO;

namespace FrameworkBenchmark
{

	class TestMap
	{
		private ArrayMap<bool> _map;
		public IMapView<bool> Map => _map;
		public int MapSize => Map.Width;
		public readonly string MapGenAlgo;

		private List<(Coord start, Coord end)> _paths;
		public IReadOnlyList<(Coord start, Coord end)> Paths => _paths.AsReadOnly();

		private static readonly Dictionary<int, (int maxRooms, int minSize, int maxSize)> rectangleMapGenParams =
		new Dictionary<int, (int maxRooms, int minSize, int maxSize)>()
		{
			{ 50, (10, 3, 7) },
			{ 100, (20, 3, 10) },
			{ 250, (35, 3, 15) },
			{ 500, (100, 3, 15) }
		};

		public TestMap(int mapSize, string mapGenAlgo, int numPaths)
		{
			MapGenAlgo = mapGenAlgo;

			_map = new ArrayMap<bool>(mapSize, mapSize);
			switch (mapGenAlgo)
			{
				case "RECTANGLE":
					QuickGenerators.GenerateRectangleMap(_map);
					break;
				case "RAND ROOMS":
					if (!rectangleMapGenParams.ContainsKey(MapSize))
						throw new Exception("No rectangle map gen parameters given for map size " + MapSize);

					var (maxRooms, minSize, maxSize) = rectangleMapGenParams[MapSize];
					QuickGenerators.GenerateRandomRoomsMap(_map, maxRooms, minSize, maxSize);
					break;

				case "CELL AUTO":
					QuickGenerators.GenerateCellularAutomataMap(_map);
					break;

				default:
					throw new Exception("Unsupported map generation algorithm specified: " + MapGenAlgo);
			}

			_paths = new List<(Coord, Coord)>();
			for (int i = 0; i < numPaths; i++)
				_paths.Add((Map.RandomPosition(true), Map.RandomPosition(true)));
		}
	}

	public class AStarBenchmarks
	{
		// Together define various test cases to run each algorithm through
		[Params(50, 100, 250, 500)]
		public int MapSize;

		// NOTE: Can't be parameterized with the current implementation of TestMap/Dictionary mapping
		public const int NUM_PATHS = 10;

		[ParamsSource(nameof(MapGenAlgosToTest))]
		public string MapGenAlgo;

		[ParamsSource(nameof(DistancesToTest))]
		public Distance DistanceCalc;

		[ParamsSource(nameof(HeuristicsToTest))]
		public string Heuristic;


		public IEnumerable<string> HeuristicsToTest()
		{
			yield return "DEFAULT";
			yield return "FAST";
		}

		// Variables setup in GlobalSetup according to benchmark-controlled params
		private TestMap map;

		private AStar grPather;
		private GR_H_W_T grHWTPather;
		private GR_H_W_T_D grHWTDPather;
		private GR_H_W_T_D_Array grHWTDArrayPather;
		private GR_H_W_T_D_Hash grHWTDHashPather;
		private GR_H_W_T_D_HashInt grHWTDHashIntPather;

		public IEnumerable<Distance> DistancesToTest()
		{
			yield return Distance.MANHATTAN;
			yield return Distance.CHEBYSHEV;
			yield return Distance.EUCLIDEAN;
		}

		public IEnumerable<string> MapGenAlgosToTest()
		{
			yield return "RECTANGLE";
			yield return "RAND ROOMS";
			yield return "CELL AUTO";
		}

		[GlobalSetup]
		public void GlobalSetup()
		{
			if (!File.Exists("seed.txt"))
				using (var seedW = new StreamWriter("seed.txt"))
					seedW.WriteLine(GoRogue.Random.SingletonRandom.DefaultRNG.Seed);
			else
			{
				var seedStr = File.ReadLines("seed.txt").Where(i => i.Length != 0).Single(); // Throw exception if not...
				InitializeRng(uint.Parse(seedStr));
			}

			map = new TestMap(MapSize, MapGenAlgo, NUM_PATHS);

			Func<Coord, Coord, double> heuristic = null;
			switch(Heuristic)
			{
				case "DEFAULT":
					heuristic = DistanceCalc.Calculate;
					break;
				case "FAST":
					heuristic = Distance.MANHATTAN.Calculate;
					break;
				default:
					throw new Exception("Unspecified heuristic type given.");
			}

			// Instantiate instances of pathers used
			grPather = new AStar(map.Map, DistanceCalc);
			grHWTPather = new GR_H_W_T(map.Map, DistanceCalc, heuristic);
			grHWTDPather = new GR_H_W_T_D(map.Map, DistanceCalc, heuristic);
			grHWTDArrayPather = new GR_H_W_T_D_Array(map.Map, DistanceCalc, heuristic);
			grHWTDHashPather = new GR_H_W_T_D_Hash(map.Map, DistanceCalc, heuristic);
			grHWTDHashIntPather = new GR_H_W_T_D_HashInt(map.Map, DistanceCalc, heuristic);
		}

		
		[Benchmark(Description = "GoRogue AStar")]
		public void GRAStar()
		{
			GoRogue.Pathing.Path path = null;

			for (int i = 0; i < NUM_PATHS; i++)
				path = grPather.ShortestPath(map.Paths[i].start, map.Paths[i].end);
		}
		

		
		[Benchmark(Description = "GoRogue AStar - H,W,T")]
		public void GRAStarHeuristicWeightsTiebreaker()
		{
			InternalPath path = null;

			for (int i = 0; i < NUM_PATHS; i++)
				path = grHWTPather.ShortestPath(map.Paths[i].start, map.Paths[i].end);
		}

		[Benchmark(Description = "GoRogue AStar - H,W,T,D")]
		public void GRAStarHeuristicWeightsTiebreakerDynamic()
		{
			InternalPath path = null;

			for (int i = 0; i < NUM_PATHS; i++)
				path = grHWTDPather.ShortestPath(map.Paths[i].start, map.Paths[i].end);
		}
		
		[Benchmark(Description = "GoRogue AStar - H,W,T,D,Array")]
		public void GRAStarHeuristicWeightsTiebreakerDynamicArray()
		{
			InternalPath path = null;

			for (int i = 0; i < NUM_PATHS; i++)
				path = grHWTDArrayPather.ShortestPath(map.Paths[i].start, map.Paths[i].end);
		}
		
		[Benchmark(Description = "GoRogue AStar - H,W,T,D,Hash")]
		public void GRAStarHeuristicWeightsTiebreakerDynamicHash()
		{
			InternalPath path = null;

			for (int i = 0; i < NUM_PATHS; i++)
				path = grHWTDHashPather.ShortestPath(map.Paths[i].start, map.Paths[i].end);
		}

		
		[Benchmark(Description = "GoRogue AStar - H,W,T,D,HashInt")]
		public void GRAStarHeuristicWeightsTiebreakerDynamicHashInt()
		{
			InternalPath path = null;

			for (int i = 0; i < NUM_PATHS; i++)
				path = grHWTDHashIntPather.ShortestPath(map.Paths[i].start, map.Paths[i].end);
		}
		
		private static void InitializeRng(uint seed)
		{
			GoRogue.Random.SingletonRandom.DefaultRNG = new Troschuetz.Random.Generators.XorShift128Generator(seed);

			for (int i = 0; i < 3; i++) // Primes same as GoRogue to account for Random bug
				GoRogue.Random.SingletonRandom.DefaultRNG.Next();
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<AStarBenchmarks>();
		}
	}
}
