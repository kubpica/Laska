using System.Collections.Generic;
using UnityEngine;

namespace Laska
{
	public class MoveOrdering : MonoBehaviourExtended
	{
		[GlobalComponent] private Board board;
		private int[] moveScores = new int[44];

		public void OrderMoves(List<string> moves)
		{
			bool canTake = moves[0].Length > 5;

			for (int i = 0; i < moves.Count; i++)
			{
				string move = moves[i];
				
				int score = 0;

				if (canTake)
				{
					// Prefer longer takes
					score += move.Length * 100;

					//TODO Prefer more valuable takes (taking officers, releasing captives etc)
				}

				// Prefer moving stronger columns
				score += board.GetColumnAt(move.Substring(0, 2)).Strength * 10;

				//TODO Prefer moves close to other pieces

				// Prefer moves closer to the center
				score += 3 - distanceToCenter(move.Substring(move.Length - 2));

				moveScores[i] = score;
			}

			sort(moves);
		}

		private int distanceToCenter(string square)
		{
			board.GetSquareIds(square, out int fileId, out int rankId);
			return Mathf.Max(Mathf.Abs(3 - fileId), Mathf.Abs(3 - rankId));
		}

		private void sort(List<string> moves)
		{
			// Sort the moves list based on scores
			for (int i = 0; i < moves.Count - 1; i++)
			{
				for (int j = i + 1; j > 0; j--)
				{
					int swapIndex = j - 1;
					if (moveScores[swapIndex] < moveScores[j])
					{
						(moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
						(moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
					}
				}
			}
		}
	}
}