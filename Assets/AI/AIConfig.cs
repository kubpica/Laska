using UnityEngine;

public class AIConfig : MonoBehaviour
{
	public bool useThreading;
	public int searchTime;
	public bool useIterativeDeepening;
	public bool limitDeepeningDepth;
	public int searchDepth;
	public bool forcedSequencesAsOneMove;
	public bool searchAllTakes;
	public bool searchUnsafePositions;
	public bool antyZugzwang;
	public bool seekWinInZugzwangSearch;
	public bool evalColumnsValue;
	public float officerValue = 10.296f;
	public float officerCaptivesShare = 0.208f;
	public float soldierValue = 4.08477f; //4.160353f; //4.08477f; //6.94963f; //5.610073f; //4.456758f;
	public float soldierCaptivesShare = 0.523585f; //0.5231462f; //0.523585f; //0.2609079f; //0.3636922f; //0.3041386f;
	public float pointsPerOwnedColumn = 10000;
	public bool evalColumnsStrength;
	public float pointsPerExtraColumnStrength = 10000;
	public bool evalSpace;
	public bool orderMoves;
	public bool useTranspositionTable;
	public bool useTTForDirectEvals;
	public bool storeBestMoveForAllNodes;
	public bool storeMovesInfuencedByDraws;
	public bool failSoft;
	public bool dontUseAlphaBeta;
}
