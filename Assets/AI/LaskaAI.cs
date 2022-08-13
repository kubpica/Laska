using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Laska
{
	public class LaskaAI : MonoBehaviourExtended
	{
		[GlobalComponent] private Board board;
		[GlobalComponent] private GameManager gameManager;
		[GlobalComponent] MoveMaker moveMaker;
		[Component] private MoveOrdering moveOrdering;
		[Component] private TranspositionTable transpositionTable;

		[Component] public AIConfig cfg;

		public const float ACTIVE_WIN = 1000000;
		public const float INACTIVE_WIN = -1000000;
		/// <summary>
		/// Contempt Factor - https://www.chessprogramming.org/Contempt_Factor
		/// </summary>
		public const float DRAW = -1; //-998999 to avoid draws

		private HashSet<ulong> _visitedNonTakePositions = new HashSet<ulong>(); // Zobrist keys
		private bool _isSearchingZugzwang;
		private HashSet<string> _cachedSafeSquares = new HashSet<string>();
		private bool _abortSearch;
		private string _threadingMove;
		private Exception _threadingException;
		private CancellationTokenSource _cancelSearchTimer;
		private CancellationTokenSource _cancelMakeMove;

		private int _numNodes;
		private int _numExtensions;
		private int _numCutoffs;
		private int _numTranspositions;

		public System.Diagnostics.Stopwatch SearchStopwatch { get; private set; }
		public int LastDepth { get; private set; } = -1;

		private void Start()
		{
			initDiagnostics();
		}

		public float EvaluatePosition(Player playerToMove)
		{
			var activeColumns = gameManager.ActivePlayer.GetOwnedColums();
			var inactiveColumns = gameManager.InactivePlayer.GetOwnedColums();

			// Check for mate (We can skip it as we check it in the negamax func)
			//int activeColumnsCount = activeColumns.Count();
			//if (activeColumnsCount == 0)
			//{
			//    return INACTIVE_WIN;
			//}

			//int inactiveColumnsCount = inactiveColumns.Count();
			//if (inactiveColumnsCount == 0)
			//{
			//    return ACTIVE_WIN;
			//}

			// Check for stalemate (We can skip it as we check it in the negamax func)
			//if (!playerToMove.HasNewPossibleMoves())
			//    return playerToMove == gameManager.ActivePlayer ? INACTIVE_WIN : ACTIVE_WIN;

			float activeScore = 0;
			
			if(cfg.pointsPerOwnedColumn != 0)
			{
				var activePieceDiff = activeColumns.Count() - inactiveColumns.Count();
				activeScore = activePieceDiff * cfg.pointsPerOwnedColumn;
			}

			if (cfg.evalColumnsValue)
			{
				foreach (var c in activeColumns)
					activeScore += c.Value * 2770.879059f; //2808.054061f;

				foreach (var c in inactiveColumns)
					activeScore -= c.Value * 2770.879059f; //2808.054061f;
			}

			if (cfg.evalColumnsStrength)
			{
				foreach (var c in activeColumns)
					activeScore += (c.Strength - 1) * cfg.pointsPerExtraColumnStrength;

				foreach (var c in inactiveColumns)
					activeScore -= (c.Strength - 1) * cfg.pointsPerExtraColumnStrength;
			}

			if (cfg.evalSpace && !cfg.antyZugzwang)
			{
				activeScore += getSpaceScore();
			}
			else if (activeScore > 20000) //activePieceDiff > 2
			{
				activeScore += calcDistanceScore();
			}

			return activeScore;
		}

		private int getSpaceScore()
		{
			int score = 0;
			_cachedSafeSquares.Clear();
			foreach (var c in gameManager.ActivePlayer.GetOwnedColums())
				score += calcAccessibleSquares(c);

			_cachedSafeSquares.Clear();
			foreach (var c in gameManager.InactivePlayer.GetOwnedColums())
				score -= calcAccessibleSquares(c);

			return score;
		}

		/// <summary>
		/// Visits accessible squares recursively and returns number of safe ones.
		/// </summary>
		private int calcAccessibleSquares(Column column)
		{
			var visitedSquares = new HashSet<Square>();
			visitAccessibleSquares(column.MovementDirections, column.Square, false);
			return visitedSquares.Count;

			void visitAccessibleSquares(List<string> movementDirections, Square square, bool useCache)
			{
				board.GetSquareIds(square.coordinate, out int file, out int rank);

				// Get squares accessible from current square
				foreach (var dir in movementDirections)
				{
					int dirX = dir[0] == '-' ? -1 : 1;
					int dirY = dir[1] == '-' ? -1 : 1;

					// Visit next square recursively
					try
					{
						var s = board.GetSquareAt(file + 1 * dirX, rank + 1 * dirY);
						if (s.IsEmpty && !visitedSquares.Contains(s) && isSquareSafe(s.coordinate, column, useCache))
						{
							visitedSquares.Add(s);
							visitAccessibleSquares(movementDirections, s, true);
						}
					}
					catch { }
				}
			}
		}

		private bool isPositionSafe(Player playerToMove, List<string> moves)
		{
			return !hasAnyUnsafePiece(playerToMove) && hasAnySafeMove(playerToMove, moves);
		}

		private bool hasAnyUnsafePiece(Player playerToMove)
		{
			foreach(var column in playerToMove.Columns)
			{
				if (!isSquareSafe(column.Position, column))
					return true;
			}
			return false;
		}

		private bool hasAnySafeMove(Player playerToMove, List<string> moves)
		{
			if (playerToMove.CanTake)
				return false;

			foreach (var move in moves)
			{
				var squares = move.Split('-');
				if (isSquareSafe(squares[1], board.GetColumnAt(squares[0])))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Square is safe for <c>columnAtRisk</c> when being on that square is not forcing oponent to take. 
		/// </summary>
		private bool isSquareSafe(string square, Column columnAtRisk, bool useCache = false)
		{
			if (useCache && _cachedSafeSquares.Contains(square))
				return true;

			// The column is near board border so it's safe
			if (square[1] == '1' || square[1] == '7' || square[0] == 'a' || square[0] == 'g')
				return true;

			board.GetSquareIds(square, out int file, out int rank);
			bool isSafe = isSquareDiagonalSafe(1) && isSquareDiagonalSafe(-1);
			if(isSafe && useCache)
			{
				_cachedSafeSquares.Add(square);
			}
			return isSafe;

			bool isSquareDiagonalSafe(int dir)
			{
				var upperColumn = board.GetColumnAt(file - 1 * dir, rank + 1);
				var lowerColumn = board.GetColumnAt(file + 1 * dir, rank - 1);
				if (upperColumn == columnAtRisk)
					upperColumn = null;
				else if (lowerColumn == columnAtRisk)
					lowerColumn = null;

				if ((upperColumn == null) != (lowerColumn == null)) // One of columns have space to take
				{
					Column attacker = upperColumn == null ? lowerColumn : upperColumn;
					if (attacker.Commander.Color == columnAtRisk.Commander.Color)
						return true;

					int attackerFile;
					int attackerRank;
					if (attacker == lowerColumn)
					{
						attackerFile = file + 1 * dir;
						attackerRank = rank - 1;
					}
					else
					{
						attackerFile = file - 1 * dir;
						attackerRank = rank + 1;
					}

					string takeDirection = attackerFile > file ? "-" : "+";
					takeDirection += attackerRank > rank ? "-" : "+";
					if (attacker.MovementDirections.Contains(takeDirection))
					{
						return false;
					}
				}
				// Either no attacking columns or no space to take - so it's safe
				return true;
			}
		}

		private int calcDistanceScore()
		{
			int score = 0;
			foreach (var ac in gameManager.ActivePlayer.pieces.Where(p => p.IsFree))
			{
				foreach (var ic in gameManager.InactivePlayer.pieces.Where(p => p.IsFree))
				{
					score += (int)Mathf.Pow(6 - board.CalcDistance(ac.Position, ic.Position), 2);
				}
			}
			return score;
		}

		private Column makeMove(string move, out Stack<Square> takenSquares, out Square previousSquare,
			out bool promotion, out bool unrepeatable)
		{
			var squares = move.Split('-');
			Column movedColumn = board.GetColumnAt(squares[0]);
			previousSquare = movedColumn.Square;
			movedColumn.ZobristAll(); // XOR-out column from old square

			Square targetSquare;
			if (squares.Length >= 3)
			{
				// Take
				unrepeatable = true;
				takenSquares = new Stack<Square>();
				targetSquare = board.GetSquareAt(squares[squares.Length-1]);

				for(int i = 1; i<squares.Length; i += 2)
				{
					var takenColumn = board.GetColumnAt(squares[i]);
					takenSquares.Push(takenColumn.Square);
					takenColumn.ZobristCommander(); // XOR-out taken piece
					movedColumn.Take(takenColumn);
				}
			}
			else
			{
				// Move
				unrepeatable = !movedColumn.Commander.IsOfficer;
				takenSquares = null;
				targetSquare = board.GetSquareAt(squares[1]);
			}
			promotion = movedColumn.Move(targetSquare);

			movedColumn.ZobristAll(); // XOR-in column on new square (with new pieces if take)
			board.ZobristSideToMove();

			return movedColumn;
		}

		private void unmakeMove(Column movedColumn, Stack<Square> takenSquares, Square previousSquare, bool demote)
		{
			board.ZobristSideToMove();
			movedColumn.ZobristAll(); // XOR-out column from current square

			if (takenSquares != null)
			{
				while (takenSquares.Count > 0)
				{
					var takenSquare = takenSquares.Pop();
					movedColumn.Untake(takenSquare);
					takenSquare.Column.ZobristCommander(); // XOR-in restored piece
				}
			}

			movedColumn.Move(previousSquare);

			if (demote)
				movedColumn.Demote();

			movedColumn.ZobristAll(); // XOR-in column on previous square (with previous pieces)
		}

		private float antyZugzwangSearch(float currentScore, float alpha, float beta, bool maximize,
			List<string> moves, int plyFromRoot, bool wasLastMoveUnrepeatable, out int repetitions)
		{
			if (cfg.orderMoves)
				moveOrdering.OrderMoves(moves, null);

			byte evalType = TranspositionTable.UpperBound;
			string bestMove = null;
			float bestScore = float.MinValue;
			repetitions = 0;

			for (int i = 0; i < moves.Count; i++)
			{
				var move = moves[i];
				Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare,
					out bool promotion, out bool unrepeatable);
				float score = -negamax(-beta, -alpha, 0, !maximize, plyFromRoot + 1, unrepeatable, ref repetitions);
				unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

				if (score > bestScore)
				{
					bestScore = score;
					bestMove = move;

					// Found a new best move so far. (PV-Node?)
					if (score > alpha)
					{
						// Exact score means that it's not influenced by cutoffs,
						// so even if alpha/beta changed it still would return the same score.
						evalType = TranspositionTable.Exact;

						// Move was *too* good (Cut-Node)
						if (score >= beta)
						{
							evalType = TranspositionTable.LowerBound;

							if (!cfg.failSoft)
								bestScore = beta;

							_numCutoffs++;
							break; // beta-cutoff
						}
						else
						{
							alpha = score;
						}

						// Found move that leads to better or equal position so it's not zugzwang.
						if (cfg.seekWinInZugzwangSearch ? score > currentScore : score >= currentScore)
						{
							// If we haven't checked all the moves, then there might be a better one
							// but we can still store it as Exact (if it's between [alpha, beta]),
							// bacause it's good enought to break the zugzwang search.

							break; // We were just making sure the position is not zugzwang.
						}
					}
				}
			}

			// We don't like any move (All-Node)
			if (evalType == TranspositionTable.UpperBound)
			{
				if (!cfg.failSoft)
					bestScore = alpha;

				if (!cfg.storeBestMoveForAllNodes)
					bestMove = null;
			}

			if (cfg.useTranspositionTable && !_abortSearch && (repetitions == 0 || wasLastMoveUnrepeatable))
				transpositionTable.StoreEvaluation(0, plyFromRoot, bestScore, evalType, bestMove);

			return bestScore;
		}

		/// <returns> Should search be extended?</returns>
		private bool quiescenceSearch(float alpha, float beta, bool maximize,
			Player playerToMove, List<string> moves, int plyFromRoot,
			out float eval, bool wasLastMoveUnrepeatable, out int repetitions)
		{
			eval = TranspositionTable.LookupFailed;
			repetitions = 0;

			if (cfg.searchAllTakes && playerToMove.CanTake)
				return true;

			if (cfg.searchUnsafePositions && !hasAnySafeMove(playerToMove, moves))
				return true;

			// Get static evaluation
			if (cfg.useTTForDirectEvals)
			{
				eval = transpositionTable.LookupDirectEvaluation(plyFromRoot);
			}
			if (eval == TranspositionTable.LookupFailed)
			{
				eval = applyPerspectiveToEval(EvaluatePosition(playerToMove), maximize);
				// Save static evaluation into transposition table
				if (cfg.useTTForDirectEvals && !_abortSearch)
				{
					transpositionTable.StoreDirectEvaluation(plyFromRoot, eval);
				}
			}

			// Anty zugzwang
			if (cfg.antyZugzwang && !_isSearchingZugzwang)
			{
				_isSearchingZugzwang = true;
				eval = antyZugzwangSearch(eval, alpha, beta, maximize, moves, plyFromRoot,
					wasLastMoveUnrepeatable, out repetitions);
				_isSearchingZugzwang = false;

				// When antyZugzwang is on, space score is skipped in the eval func so let's add it now
				if (cfg.evalSpace && !IsWinEval(eval))
				{
					eval += getSpaceScore();
				}
			}

			return false;
		}

		private float applyPerspectiveToEval(float eval, bool maximize)
		{
			return maximize ? eval : -eval;
		}

		/// <summary>
		/// Alternative notation of the minimax algorithm in which in every node we maximize,
		/// it's achieved by negating values and applying perspective to eval.
		/// (You can imagine this as if we were turning the eval bar upside down every move.
		/// Alpha would be our lower bound and beta the upper one, and our goal is to find
		/// the best move with a score that fits within that window.)
		/// </summary>
		/// <param name="alpha"> Value of the best move so far of the player to move in this node.</param>
		/// <param name="beta"> Value of the best move of the other player.</param>
		/// <param name="depth"> 
		/// Decreases with every node checked, at depth 0 we run search extensions and then <see cref="EvaluatePosition(Player)"/>.
		/// </param>
		/// <param name="maximize"> 
		/// Whos move it is at this node; true if it's "root player".
		/// (As our eval func returns positive values when the position is good for <see cref="GameManager.ActivePlayer"/>.)
		/// </param>
		/// <param name="plyFromRoot"> Starts with 0 at root node and increases with every node checked.</param>
		/// <param name="wasLastMoveUnrepeatable"> 
		/// Was the move that led to this position Take or Soldier move? False if Officer move.
		/// If true, only deeper positions can be repeated, so it's safe to store score from this node it in TT.
		/// </param>
		/// <param name="repetitionsLastNode"> How many draws by repetition were found starting from this/previous node.</param>
		/// <returns> 
		/// It depends on whether we are using fail-soft or fail-hard version
		/// and whether we fit in the [alpha, beta] window or not,
		/// but the general idea is to return value of the best move in a given position.
		/// </returns>
		private float negamax(float alpha, float beta, int depth, bool maximize, int plyFromRoot,
			bool wasLastMoveUnrepeatable, ref int repetitionsLastNode)
		{
			// If we use iterative deepening result from this iteration
			// will be discarded and move from previous one will be used.
			if (_abortSearch)
			{
				return 0;
			}

			_numNodes++;

			if (cfg.dontUseAlphaBeta)
			{
				alpha = float.MinValue;
				beta = float.MaxValue;
			}
			else
			{
				// Skip this position if a mating sequence has already been found earlier in
				// the search, which would be shorter than any mate we could find from here.
				// This is done by observing that alpha can't possibly be worse (and likewise
				// beta can't possibly be better) than being mated in the current position.
				alpha = Mathf.Max(alpha, INACTIVE_WIN + plyFromRoot);
				beta = Mathf.Min(beta, ACTIVE_WIN - plyFromRoot);
				if (alpha >= beta)
				{
					return alpha;
				}
			}

			// Detect draw by repetition.
			// Returns a draw score even if this position has only appeared once in the game history (for simplicity).
			if (_visitedNonTakePositions.Contains(board.ZobristKey))
			{
				repetitionsLastNode++;
				return applyPerspectiveToEval(DRAW, maximize);
			}

			// Try looking up the current position in the transposition table.
			// If the same position has already been searched to at least an equal depth
			// to the search we're doing now, we can just use the recorded evaluation.
			string ttMove = null;
			if (cfg.useTranspositionTable)
			{
				float ttVal = transpositionTable
					.LookupEvaluation(_isSearchingZugzwang ? -1 : Mathf.Max(0, depth), plyFromRoot, alpha, beta, out ttMove);
				if (ttVal != TranspositionTable.LookupFailed)
				{
					_numTranspositions++;
					return ttVal;
				}
			}

			Player playerToMove = maximize ? gameManager.ActivePlayer : gameManager.InactivePlayer;
			List<string> moves = playerToMove.GetPossibleMovesAndMultiTakes(true);
			bool canTake = playerToMove.CanTake;

			if (moves.Count == 0)
			{
				// Current ply added to reward finding mate quicker, or avoiding mate longer.
				return INACTIVE_WIN + plyFromRoot;
			}
			else if (moves.Count == 1)
			{
				if (cfg.forcedSequencesAsOneMove)
					depth++;
			}
			else if (depth <= 0)
			{
				if (!quiescenceSearch(alpha, beta, maximize, playerToMove, moves, plyFromRoot,
					out float eval, wasLastMoveUnrepeatable, out int repetitions))
				{
					// Final depth, return eval
					repetitionsLastNode += repetitions;
					return eval;
				}
				else
				{
					// Search extended
					_numExtensions++;
				}
			}

			if(cfg.orderMoves)
				moveOrdering.OrderMoves(moves, ttMove);

			if (!canTake)
				_visitedNonTakePositions.Add(board.ZobristKey);

			byte evalType = TranspositionTable.UpperBound;
			string bestMove = null;
			float bestScore = float.MinValue;
			int repetitionsThisNode = 0;

			foreach (var move in moves)
			{
				Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare,
					out bool promotion, out bool unrepeatable);
				float score = -negamax(-beta, -alpha, depth - 1, !maximize, plyFromRoot + 1, unrepeatable, ref repetitionsThisNode);
				unmakeMove(movedColumn, takenSquares, previousSquare, promotion);

				if (score > bestScore)
				{
					bestScore = score;
					bestMove = move;

					// Move was *too* good, so opponent won't allow this position to be reached
					// (by choosing a different move earlier on, so we "refute" opponents previous move). Skip remaining moves.
					if (score >= beta)
					{
						// Fail-high / Cut-Node
						// As we stop searching this node here we can't be sure that "bestMove" is actually the best,
						// but we will still store it in transposition table as it may be good guess for move ordering.
						// It's called Refutation Move - not necessarily the best, but good enough to refute opponents previous move.
						// (Also there could be more cutoffs deeper, so it's not guaranteed to be Exact even if it's the last move.)
						evalType = TranspositionTable.LowerBound;

						// In Fail-hard version we clamp returned/stored score to beta when we fail-high
						if (!cfg.failSoft)
							bestScore = beta;

						_numCutoffs++;
						break; // beta-cutoff
					}

					// Found a new best move so far (if false, we have better move earlier).
					if (score > alpha)
					{
						// Inside-window / PV-Node
						// It's confirmed to be PV-Node only if it's the last move in this position;
						// otherwise it still can turn out to be Cut-Node, but it's not All-Node for sure.
						evalType = TranspositionTable.Exact;

						alpha = score; // alpha acts like max in MiniMax
					}
				}
			}

			// We don't like any of the moves in this position.
			// We can do better by choosing a different move earlier on.
			if (evalType == TranspositionTable.UpperBound)
			{
				// Fail-low / All-Node
				// Even though we checked all the moves in this position, most likely there were some cutoffs deeper.
				// The opponent deeper may have even better moves than the ones we checked, so bestScore is our upper bound.

				// bestScore may be lower than alpha here, so in Fail-hard version we clamp it to alpha
				if (!cfg.failSoft)
					bestScore = alpha;

				// Some engines store "bestMove" for All-Nodes, while other don't save any move when all of them failed-low (e.g. Stockfish).
				// I found some old online discussion (https://groups.google.com/g/rec.games.chess.computer/c/p8GbiiLjp0o)
				// where people claim that storing "bestMove" for "All-Nodes" (AlphaFlag) resulted in faster search for them
				// but they are woried that it may not necessery mean stronger play because of increased search instability.
				if (!cfg.storeBestMoveForAllNodes)
					bestMove = null;
			}

			if (cfg.useTranspositionTable && !_abortSearch)
			{
				// We shoudn't store in TT scores that were influenced by repetiton draws, because scores stored in TT should only
				// depend on deeper positions and not previous ones (as sometimes we can reach the same position by different path).
				// Draws by repetition can depend on positions prior to this one so we shoudn't store scores based on them in TT.
				if (repetitionsThisNode == 0 || wasLastMoveUnrepeatable)
				{
					transpositionTable
						.StoreEvaluation(_isSearchingZugzwang ? -1 : Mathf.Max(1, depth), plyFromRoot, bestScore, evalType, bestMove);
				}
				// Is it worth to store the "bestMove" anyway? Maybe it would still improve move ordering even if influenced by draws?
				else if (cfg.storeMovesInfuencedByDraws)
				{
					transpositionTable.StoreEvaluation(depth, plyFromRoot, bestScore, TranspositionTable.Invalid, bestMove);
				}
			}

			if (!canTake)
				_visitedNonTakePositions.Remove(board.ZobristKey);

			repetitionsLastNode += repetitionsThisNode;

			// In Fail-hard version bestScore is alpha for All-Node, beta for Cut-Node and exact for PV-Node.
			// In Fail-soft version bestScore is not clamped to [alpha, beta] range.
			return bestScore;
		}

		public static bool IsWinEval(float eval) 
		{
			const int maxWinDepth = 1000;
			return Mathf.Abs(eval) > ACTIVE_WIN - maxWinDepth;
		}

		private void announceMate(float eval)
		{
			if (IsWinEval(eval))
			{
				var numPlyToMate = ACTIVE_WIN - Mathf.Abs(eval);
				int numMovesToMate = Mathf.CeilToInt(numPlyToMate / 2f);
				string sideWithMate = eval > 0 ? gameManager.ActivePlayer.GetName() : gameManager.InactivePlayer.GetName();
				Debug.LogError($"{sideWithMate} can win in {numMovesToMate} move{((numMovesToMate > 1) ? "s" : "")}");
			}
		}

		public string BestMoveMinimax()
		{
			PiecesManager.TempMoves = true;
			_abortSearch = false;
			float bestScoreThisIteration, bestScore = float.MinValue;
			string bestMoveThisIteration, bestMove = null;
			int bestDepth = 0;

			List<string> moves = gameManager.ActivePlayer.GetPossibleMovesAndMultiTakes();
			if (moves.Count == 1)
			{
				bestMove = moves[0];
			}
			else
			{
				initDiagnostics();
				if (cfg.useIterativeDeepening)
				{
					int targetDepth = cfg.limitDeepeningDepth ? cfg.searchDepth : int.MaxValue;
					for (int depth = 1; depth <= targetDepth; depth++)
					{
						search(depth);
						if (_abortSearch)
						{
							if(bestMove == null)
							{
								Debug.LogError("Move not found - not enough time. Using TT-move...");
								bestMove = transpositionTable.GetStoredMove();
								if(bestMove == null)
								{
									Debug.LogError("Move not found even in TT! Using first move...");
									bestMove = moves[0];
								}
							}
							break;
						}
						else
						{
							bestMove = bestMoveThisIteration;
							bestScore = bestScoreThisIteration;
							bestDepth = depth;

							// Exit search if found a mate
							if (IsWinEval(bestScore))
							{
								break;
							}
						}
					}
				}
				else
				{
					search(cfg.searchDepth);
					bestMove = bestMoveThisIteration;
					bestScore = bestScoreThisIteration;
					bestDepth = cfg.searchDepth;
				}
				logDiagnostics();
			}
			PiecesManager.TempMoves = false;

			if (moves.Count == 1)
			{
				Debug.Log(gameManager.ActivePlayer.GetName() + ": forcedMove " + bestMove);
				LastDepth = -1;
			}
			else
			{
				Debug.Log(gameManager.ActivePlayer.GetName() 
					+ ": bestMove/" + moves.Count + " " + bestMove + " (" + bestScore + "), Depth: " + bestDepth);
				announceMate(bestScore);
				LastDepth = bestDepth;
			}
			return bestMove;

			void search(int depth)
			{
				bestScoreThisIteration = float.MinValue;
				bestMoveThisIteration = null;
				string ttMove = null;
				if (cfg.useTranspositionTable)
				{
					float ttVal = transpositionTable
						.LookupEvaluation(depth, 0, float.MinValue, float.MaxValue, out ttMove);
					if (ttVal != TranspositionTable.LookupFailed)
					{
						bestMoveThisIteration = ttMove;
						bestScoreThisIteration = ttVal;
						return;
					}
				}

				int repetitions = 0;

				if (cfg.orderMoves)
					moveOrdering.OrderMoves(moves, ttMove);

				_visitedNonTakePositions.Clear();
				foreach (var p in board.GetPositionsSinceLastTake())
					_visitedNonTakePositions.Add(p);

				foreach (var move in moves)
				{
					Column movedColumn = makeMove(move, out Stack<Square> takenSquares, out Square previousSquare,
						out bool promotion, out bool unrepeatable);

					float score = -negamax(float.MinValue, -bestScoreThisIteration, depth - 1, false, 1, unrepeatable, ref repetitions);
					if (score > bestScoreThisIteration)
					{
						bestScoreThisIteration = score;
						bestMoveThisIteration = move;
					}

					unmakeMove(movedColumn, takenSquares, previousSquare, promotion);
				}

				if (cfg.useTranspositionTable && !_abortSearch && (repetitions == 0 || _visitedNonTakePositions.Count <= 1))
					transpositionTable.StoreEvaluation(depth, 0, bestScoreThisIteration, TranspositionTable.Exact, bestMoveThisIteration);
			}
		}

		private void initDiagnostics()
		{
			SearchStopwatch = System.Diagnostics.Stopwatch.StartNew();
			_numNodes = 0;
			_numExtensions = 0;
			_numCutoffs = 0;
			_numTranspositions = 0;
		}

		private void logDiagnostics()
		{
			Debug.Log($"Search time: {SearchStopwatch.ElapsedMilliseconds} ms." +
				$" Nodes: {_numNodes} Extensions: {_numExtensions} Cutoffs: {_numCutoffs} TThits: {_numTranspositions}");
		}

		public void MakeMove()
		{
			if (cfg.useThreading)
			{
				_cancelMakeMove = new CancellationTokenSource();
				Task.Factory.StartNew(() =>
				{
					try
					{
						_threadingMove = BestMoveMinimax();
					}
					catch (Exception e)
					{
						_threadingException = e;
					}
					_cancelSearchTimer?.Cancel();
				}, _cancelMakeMove.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

				_cancelSearchTimer = new CancellationTokenSource();
				Task.Delay(cfg.searchTime, _cancelSearchTimer.Token).ContinueWith(_ => EndSearch());
			}
			else 
			{
				var move = BestMoveMinimax();
				moveMaker.MakeMove(move);
			}
		}

		public void AbortMakeMove()
		{
			_cancelMakeMove?.Cancel();
			EndSearch();
		}

		public void EndSearch()
		{
			if (_cancelSearchTimer == null || !_cancelSearchTimer.IsCancellationRequested)
			{
				_abortSearch = true;
			}
		}

		private void Update()
		{
			if (_threadingMove != null)
			{
				moveMaker.MakeMove(_threadingMove);
				_threadingMove = null;
			}

			if(_threadingException != null)
			{
				Debug.LogException(_threadingException);
				_threadingException = null;
			}
		}
	}
}