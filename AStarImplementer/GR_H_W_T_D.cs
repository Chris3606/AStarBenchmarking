using GoRogue;
using GoRogue.MapViews;
using Priority_Queue;
using System;
using System.Collections.Generic;

namespace AStarImplementer
{
	// GoRogue AStar, Heuristic/Weight added, using nudging Tiebreaker instead of direction ordering, optimized for dynamic allocation of nodes.
	public class GR_H_W_T_D
	{
		// Used to calculate neighbors of a given cell
		private GRAStarNode[] nodes;

		private int nodesHeight;

		// Width and of the walkability map at the last path -- used to determine whether
		// reallocation of nodes array is necessary
		private int nodesWidth;

		// Node objects used under the hood for the priority queue
		// Priority queue of the open nodes.
		private FastPriorityQueue<GRAStarNode> openNodes;

		private Func<Coord, Coord, double> Heuristic;
		private IMapView<double> Weights { get; }

		private double maxPathMultiplier;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="walkabilityMap">
		/// Map view used to determine whether or not a given location can be traversed -- true indicates
		/// walkable, false unwalkable.
		/// </param>
		/// <param name="distanceMeasurement">
		/// <see cref="Distance"/> measurement used to determine the method of measuring distance between two
		/// points, the heuristic AStar uses when pathfinding, and whether locations are connected in
		/// a 4-way or 8-way pattern.
		/// </param>
		public GR_H_W_T_D(IMapView<bool> walkabilityMap, Distance distanceMeasurement, Func<Coord, Coord, double> heuristic = null, IMapView<double> weights = null)
		{
			maxPathMultiplier = 1.0 + (1.0 / (walkabilityMap.Width * walkabilityMap.Height + 1));
			// TODO: DONT override default heurtistic like this, this is only for testing!
			var h = heuristic ?? distanceMeasurement.Calculate; // Add heuristic on top of this, just for testing
			Heuristic = (c1, c2) => h(c1, c2) * maxPathMultiplier;

			Weights = weights;

			WalkabilityMap = walkabilityMap;
			DistanceMeasurement = distanceMeasurement;

			int maxSize = walkabilityMap.Width * walkabilityMap.Height;
			nodes = new GRAStarNode[maxSize];
			//for (int i = 0; i < maxSize; i++)
			//	nodes[i] = new GRAStarNode(Coord.ToCoord(i, walkabilityMap.Width), null);
			nodesWidth = walkabilityMap.Width;
			nodesHeight = walkabilityMap.Height;

			openNodes = new FastPriorityQueue<GRAStarNode>(maxSize);
		}

		/// <summary>
		/// The distance calculation being used to determine distance between points. <see cref="Distance.MANHATTAN"/>
		/// implies 4-way connectivity, while <see cref="Distance.CHEBYSHEV"/> or <see cref="Distance.EUCLIDEAN"/> imply
		/// 8-way connectivity for the purpose of determining adjacent coordinates.
		/// </summary>
		public Distance DistanceMeasurement;

		/// <summary>
		/// The map view being used to determine whether or not each tile is walkable.
		/// </summary>
		public IMapView<bool> WalkabilityMap { get; private set; }

		/// <summary>
		/// Finds the shortest path between the two specified points.
		/// </summary>
		/// <remarks>
		/// Returns <see langword="null"/> if there is no path between the specified points. Will still return an
		/// appropriate path object if the start point is equal to the end point.
		/// </remarks>
		/// <param name="start">The starting point of the path.</param>
		/// <param name="end">The ending point of the path.</param>
		/// <param name="assumeEndpointsWalkable">
		/// Whether or not to assume the start and end points are walkable, regardless of what the
		/// <see cref="WalkabilityMap"/> reports. Defaults to <see langword="true"/>.
		/// </param>
		/// <returns>The shortest path between the two points, or <see langword="null"/> if no valid path exists.</returns>
		public InternalPath ShortestPath(Coord start, Coord end, bool assumeEndpointsWalkable = true)
		{
			// Don't waste initialization time if there is definately no path
			if (!assumeEndpointsWalkable && (!WalkabilityMap[start] || !WalkabilityMap[end]))
				return null; // There is no path

			// If the path is simply the start, don't bother with graph initialization and such
			if (start == end)
			{
				var retVal = new List<Coord> { start };
				return new InternalPath(retVal);
			}

			// Clear nodes to beginning state
			if (nodesWidth != WalkabilityMap.Width || nodesHeight != WalkabilityMap.Height)
			{
				int length = WalkabilityMap.Width * WalkabilityMap.Height;
				nodes = new GRAStarNode[length];
				openNodes = new FastPriorityQueue<GRAStarNode>(length);
				//for (int i = 0; i < length; i++)
				//	nodes[i] = new GRAStarNode(Coord.ToCoord(i, WalkabilityMap.Width), null);

				nodesWidth = WalkabilityMap.Width;
				nodesHeight = WalkabilityMap.Height;

				maxPathMultiplier = 1.0 + (1.0 / (length + 1));
			}
			else
			{
				foreach (var node in nodes)
					if (node != null)
						node.Closed = false;
			}

			var result = new List<Coord>();
			int index = start.ToIndex(WalkabilityMap.Width);

			if (nodes[index] == null)
				nodes[index] = new GRAStarNode(start, null);

			nodes[index].G = 0;
			nodes[index].F = (float)Heuristic(start, end); // Completely heuristic for first node
			openNodes.Enqueue(nodes[index], nodes[index].F);

			while (openNodes.Count != 0)
			{
				var current = openNodes.Dequeue();
				current.Closed = true;  // We are evaluating this node, no need for it to ever end up in open nodes queue again
				if (current.Position == end) // We found the end, cleanup and return the path
				{
					openNodes.Clear();

					do
					{
						result.Add(current.Position);
						current = current.Parent;
					} while (current.Position != start);

					result.Add(start);
					return new InternalPath(result);
				}

				foreach (var dir in ((AdjacencyRule)DistanceMeasurement).DirectionsOfNeighbors())
				{
					Coord neighborPos = current.Position + dir;

					// Not a valid map position, ignore
					if (neighborPos.X < 0 || neighborPos.Y < 0 || neighborPos.X >= WalkabilityMap.Width || neighborPos.Y >= WalkabilityMap.Height)
						continue;

					if (!checkWalkability(neighborPos, start, end, assumeEndpointsWalkable)) // Not part of walkable node "graph", ignore
						continue;

					int neighborIndex = neighborPos.ToIndex(WalkabilityMap.Width);
					var neighbor = nodes[neighborIndex];

					var isNeighborOpen = IsOpen(neighbor, openNodes);

					if (neighbor == null) // Can't be closed because never visited
						nodes[neighborIndex] = neighbor = new GRAStarNode(neighborPos, null);
					else if (nodes[neighborIndex].Closed) // This neighbor has already been evaluated at shortest possible path, don't re-add
						continue;

					float newDistance = current.G + (float)DistanceMeasurement.Calculate(current.Position, neighbor.Position) * (float)(Weights== null ? 1.0 : Weights[neighbor.Position]);
					if (isNeighborOpen && newDistance >= neighbor.G) // Not a better path
						continue;

					// We found a best path, so record and update
					neighbor.Parent = current;
					neighbor.G = newDistance; // (Known) distance to this node via shortest path
					// Heuristic distance to end (priority in queue). If it's already in the queue, update priority to new F
					neighbor.F = newDistance + (float)Heuristic(neighbor.Position, end);

					if (openNodes.Contains(neighbor))
						openNodes.UpdatePriority(neighbor, neighbor.F);
					else // Otherwise, add it with proper priority
					{
						openNodes.Enqueue(neighbor, neighbor.F);
					}
				}
			}

			openNodes.Clear();
			return null; // No path found
		}

		private static bool IsOpen(GRAStarNode node, FastPriorityQueue<GRAStarNode> openSet)
		{
			return node != null && openSet.Contains(node);
		}

		/// <summary>
		/// Finds the shortest path between the two specified points.
		/// </summary>
		/// <remarks>
		/// Returns <see langword="null"/> if there is no path between the specified points. Will still return an
		/// appropriate path object if the start point is equal to the end point.
		/// </remarks>
		/// <param name="startX">The x-coordinate of the starting point of the path.</param>
		/// <param name="startY">The y-coordinate of the starting point of the path.</param>
		/// <param name="endX">The x-coordinate of the ending point of the path.</param>
		/// <param name="endY">The y-coordinate of the ending point of the path.</param>
		/// <param name="assumeEndpointsWalkable">
		/// Whether or not to assume the start and end points are walkable, regardless of what the
		/// <see cref="WalkabilityMap"/> reports. Defaults to <see langword="true"/>.
		/// </param>
		/// <returns>The shortest path between the two points, or <see langword="null"/> if no valid path exists.</returns>
		public InternalPath ShortestPath(int startX, int startY, int endX, int endY, bool assumeEndpointsWalkable = true)
			=> ShortestPath(new Coord(startX, startY), new Coord(endX, endY), assumeEndpointsWalkable);

		private bool checkWalkability(Coord pos, Coord start, Coord end, bool assumeEndpointsWalkable)
		{
			if (!assumeEndpointsWalkable)
				return WalkabilityMap[pos];

			return WalkabilityMap[pos] || pos == start || pos == end;
		}
	}
}
