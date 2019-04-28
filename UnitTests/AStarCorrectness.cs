using AStarImplementer;
using System.Collections.Generic;
using GoRogue;
using GoRogue.MapGeneration;
using GoRogue.MapViews;
using GoRogue.Pathing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class AStarCorrectness
	{
		[TestMethod]
		public void GR_H_W_T()
		{
			var map = new ArrayMap<bool>(250, 250);
			QuickGenerators.GenerateRandomRoomsMap(map, 75, 3, 15);

			var paths = GetRandomPaths(map, 100);

			AStar grAStar = new AStar(map, Distance.CHEBYSHEV);
			var testAStar = new GR_H_W_T(map, Distance.CHEBYSHEV);

			foreach (var path in paths)
			{
				Path aStarPath = grAStar.ShortestPath(path.start, path.end);
				InternalPath testPath = testAStar.ShortestPath(path.start, path.end);

				Assert.AreEqual(aStarPath.LengthWithStart, testPath.LengthWithStart);
			}
		}

		[TestMethod]
		public void GR_H_W_T_D()
		{
			var map = new ArrayMap<bool>(250, 250);
			QuickGenerators.GenerateRandomRoomsMap(map, 75, 3, 15);

			var paths = GetRandomPaths(map, 100);

			AStar grAStar = new AStar(map, Distance.CHEBYSHEV);
			var testAStar = new GR_H_W_T_D(map, Distance.CHEBYSHEV);

			foreach (var path in paths)
			{
				Path aStarPath = grAStar.ShortestPath(path.start, path.end);
				InternalPath testPath = testAStar.ShortestPath(path.start, path.end);

				Assert.AreEqual(aStarPath.LengthWithStart, testPath.LengthWithStart);
			}
		}

		[TestMethod]
		public void GR_H_W_T_D_Array()
		{
			var map = new ArrayMap<bool>(250, 250);
			QuickGenerators.GenerateRandomRoomsMap(map, 75, 3, 15);

			var paths = GetRandomPaths(map, 100);

			AStar grAStar = new AStar(map, Distance.CHEBYSHEV);
			var testAStar = new GR_H_W_T_D_Array(map, Distance.CHEBYSHEV);

			foreach (var path in paths)
			{
				Path aStarPath = grAStar.ShortestPath(path.start, path.end);
				InternalPath testPath = testAStar.ShortestPath(path.start, path.end);

				Assert.AreEqual(aStarPath.LengthWithStart, testPath.LengthWithStart);
			}
		}

		[TestMethod]
		public void GR_H_W_T_D_Hash()
		{
			var map = new ArrayMap<bool>(250, 250);
			QuickGenerators.GenerateRandomRoomsMap(map, 75, 3, 15);

			var paths = GetRandomPaths(map, 100);

			AStar grAStar = new AStar(map, Distance.CHEBYSHEV);
			var testAStar = new GR_H_W_T_D_Hash(map, Distance.CHEBYSHEV);

			foreach (var path in paths)
			{
				Path aStarPath = grAStar.ShortestPath(path.start, path.end);
				InternalPath testPath = testAStar.ShortestPath(path.start, path.end);

				Assert.AreEqual(aStarPath.LengthWithStart, testPath.LengthWithStart);
			}
		}

		[TestMethod]
		public void GR_H_W_T_D_HashInt()
		{
			var map = new ArrayMap<bool>(250, 250);
			QuickGenerators.GenerateRandomRoomsMap(map, 75, 3, 15);

			var paths = GetRandomPaths(map, 100);

			AStar grAStar = new AStar(map, Distance.CHEBYSHEV);
			var testAStar = new GR_H_W_T_D_HashInt(map, Distance.CHEBYSHEV);

			foreach (var path in paths)
			{
				Path aStarPath = grAStar.ShortestPath(path.start, path.end);
				InternalPath testPath = testAStar.ShortestPath(path.start, path.end);

				Assert.AreEqual(aStarPath.LengthWithStart, testPath.LengthWithStart);
			}
		}

		private List<(Coord start, Coord end)> GetRandomPaths(IMapView<bool> map,int numPaths)
		{
			var paths = new List<(Coord, Coord)>();
			for (int i = 0; i < numPaths; i++)
				paths.Add((map.RandomPosition(true), map.RandomPosition(true)));

			return paths;
		}
	}
}
