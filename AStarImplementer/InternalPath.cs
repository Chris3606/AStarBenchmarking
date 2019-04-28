using System.Collections.Generic;
using GoRogue;

namespace AStarImplementer
{
	/// <summary>
	/// Encapsulates a path as returned by pathfinding algorithms like AStar.
	/// </summary>
	/// <remarks>
	/// Provides various functions to iterate through/access steps of the path, as well as
	/// constant-time reversing functionality.
	/// </remarks>
	public class InternalPath
	{
		private IReadOnlyList<Coord> _steps;
		private bool inOriginalOrder;

		/// <summary>
		/// Creates a copy of the path, optionally reversing the path as it does so.
		/// </summary>
		/// <remarks>Reversing is an O(1) operation, since it does not modify the list.</remarks>
		/// <param name="pathToCopy">The path to copy.</param>
		/// <param name="reverse">Whether or not to reverse the path. Defaults to <see langword="false"/>.</param>
		public InternalPath(InternalPath pathToCopy, bool reverse = false)
		{
			_steps = pathToCopy._steps;
			inOriginalOrder = (reverse ? !pathToCopy.inOriginalOrder : pathToCopy.inOriginalOrder);
		}

		// Create based on internal list
		internal InternalPath(IReadOnlyList<Coord> steps)
		{
			_steps = steps;
			inOriginalOrder = true;
		}

		/// <summary>
		/// Ending point of the path.
		/// </summary>
		public Coord End
		{
			get
			{
				if (inOriginalOrder)
					return _steps[0];

				return _steps[_steps.Count - 1];
			}
		}

		/// <summary>
		/// The length of the path, NOT including the starting point.
		/// </summary>
		public int Length { get => _steps.Count - 1; }

		/// <summary>
		/// The length of the path, INCLUDING the starting point.
		/// </summary>
		public int LengthWithStart { get => _steps.Count; }

		/// <summary>
		/// Starting point of the path.
		/// </summary>
		public Coord Start
		{
			get
			{
				if (inOriginalOrder)
					return _steps[_steps.Count - 1];

				return _steps[0];
			}
		}

		/// <summary>
		/// The coordinates that constitute the path (in order), NOT including the starting point.
		/// These are the coordinates something might walk along to follow a path.
		/// </summary>
		public IEnumerable<Coord> Steps
		{
			get
			{
				if (inOriginalOrder)
				{
					for (int i = _steps.Count - 2; i >= 0; i--)
						yield return _steps[i];
				}
				else
				{
					for (int i = 1; i < _steps.Count; i++)
						yield return _steps[i];
				}
			}
		}

		/// <summary>
		/// The coordinates that constitute the path (in order), INCLUDING the starting point.
		/// </summary>
		public IEnumerable<Coord> StepsWithStart
		{
			get
			{
				if (inOriginalOrder)
				{
					for (int i = _steps.Count - 1; i >= 0; i--)
						yield return _steps[i];
				}
				else
				{
					for (int i = 0; i < _steps.Count; i++)
						yield return _steps[i];
				}
			}
		}

		/// <summary>
		/// Gets the nth step along the path, where 0 is the step AFTER the starting point.
		/// </summary>
		/// <param name="stepNum">The (array-like index) of the step to get.</param>
		/// <returns>The coordinate consituting the step specified.</returns>
		public Coord GetStep(int stepNum)
		{
			if (inOriginalOrder)
				return _steps[(_steps.Count - 2) - stepNum];

			return _steps[stepNum + 1];
		}

		/// <summary>
		/// Gets the nth step along the path, where 0 IS the starting point.
		/// </summary>
		/// <param name="stepNum">The (array-like index) of the step to get.</param>
		/// <returns>The coordinate consituting the step specified.</returns>
		public Coord GetStepWithStart(int stepNum)
		{
			if (inOriginalOrder)
				return _steps[(_steps.Count - 1) - stepNum];

			return _steps[stepNum];
		}

		/// <summary>
		/// Reverses the path, in constant time.
		/// </summary>
		public void Reverse() => inOriginalOrder = !inOriginalOrder;

		/// <summary>
		/// Returns a string representation of all the steps in the path, including the start point,
		/// eg. [(1, 2), (3, 4), (5, 6)].
		/// </summary>
		/// <returns>A string representation of all steps in the path, including the start.</returns>
		public override string ToString() => StepsWithStart.ExtendToString();
	}
}
