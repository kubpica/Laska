using Laska;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BotsTournamentManager : MonoBehaviourExtended
{
    static System.Random rnd = new System.Random();
   
    public int stagesLimit = 0;

    public GameManager GameManager => GameManager.Instance;
    public MoveMaker MoveMaker => MoveMaker.Instance;

    private Player White => GameManager.GetPlayer('w');
    private Player Black => GameManager.GetPlayer('b');

    private List<TournamentBot> _competitors;
    private RoundRobinTournament _tournament;
    private List<MatchPair> _matches;

    private MatchPair _currentMatch;
    private int _matchRound = 0;
    private TournamentBot _firstRoundWinner;
    private TournamentBot _secondRoundWinner;
    private int _randomMoveCounter = 0;
    private string[] _randomMoves = new string[6];


    private void OnEnable()
    {
        SceneManager.sceneLoaded += onSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= onSceneLoaded;
    }

    private void Start()
    {
        _competitors = new List<TournamentBot>(GetComponentsInChildren<TournamentBot>());
        _tournament = new RoundRobinTournament(_competitors);

        _matches = _tournament.GenerateMatches();
        startMatch();
    }

    private void resetScene()
    {
        PiecesManager.TempMoves = false;
        SceneManager.LoadScene("Laska");
    }

    private void onSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Laska" && _currentMatch != null)
        {
            GameManager.onGameStarted.AddListener(startRound);
        }
    }

    private void endTournament()
    {
        Debug.LogError("Bots Tournament ended on stage " + stagesLimit + ". Results:");
        foreach (var c in _competitors)
        {
            c.PrintStats();
        }
    }

    private void startMatch()
    {
        bool isEnd = false;
        if (_matches.Count == 0)
            isEnd = !nextStage();

        if (isEnd)
        {
            endTournament();
            return;
        }

        _currentMatch = _matches[0];
        _matches.RemoveAt(0);

        Debug.LogError("Starting " + _currentMatch.NameA + " vs " + _currentMatch.NameB);

        resetScene();
    }

    private bool nextStage()
    {
        if (stagesLimit == _tournament.Stage)
            return false;

        _matches = _tournament.GenerateNextMatches();
        return true;
    }

    private void startRound()
    {
        GameManager.onPlayerDecision.AddListener(playMatch);
        GameManager.onGameEnded.AddListener(gameEnded);
        _matchRound++;
        Debug.LogError("Round " + _matchRound);
        _randomMoveCounter = 0;
        playMatch();
    }

    private void playMatch()
    {
        switch (_matchRound)
        {
            case 1:
                playFirstRound();
                break;
            case 2:
                playSecondRound();
                break;
        }
    }

    private void playFirstRound()
    {
        if (_randomMoveCounter < 6)
        {
            drawRandomMove();
            if(_randomMoveCounter < 5)
                return;
        }

        var a = _currentMatch.CompetitorA;
        var b = _currentMatch.CompetitorB;
        playRound(a, b);
    }

    private void playSecondRound()
    {
        if (_randomMoveCounter < 6)
        {
            redoRandomMove();
            if (_randomMoveCounter < 5)
                return;
        }

        var a = _currentMatch.CompetitorA;
        var b = _currentMatch.CompetitorB;
        playRound(b, a);
    }

    private void drawRandomMove() 
    {
        var player = GameManager.ActivePlayer;
        var moves = player.GetPossibleMovesAndMultiTakes();
        var move = moves[rnd.Next(moves.Count)];
        _randomMoves[_randomMoveCounter] = move;
        makeMove(move);
    }

    private void redoRandomMove()
    {
        var move = _randomMoves[_randomMoveCounter];
        makeMove(move);
    }

    private void makeMove(string move)
    {
        Debug.Log("Random move: " + move);
        MoveMaker.MakeMove(move);
        _randomMoveCounter++;
    }

    private void playRound(TournamentBot whiteBot, TournamentBot blackBot)
    {
        var white = White;
        var black = Black;

        white.AI.cfg = whiteBot.settings;
        black.AI.cfg = blackBot.settings;

        white.isAI = true;
        black.isAI = true;
    }

    private void gameEnded(char winner)
    {
        TournamentBot winnerBot;
        switch (winner) 
        {
            case 'w':
                winnerBot = _matchRound == 1 ? _currentMatch.CompetitorA : _currentMatch.CompetitorB; 
                break;
            case 'b':
                winnerBot = _matchRound == 2 ? _currentMatch.CompetitorA : _currentMatch.CompetitorB;
                break;
            case '-':
            default:
                winnerBot = null;
                break;
        }
        endRound(winnerBot);
    }

    private void endRound(TournamentBot winnerBot)
    {
        if (winnerBot == null)
            Debug.LogError($"Round ended: Draw");
        else
            Debug.LogError($"Round ended: " + winnerBot.botName + " wins!");

        if (_matchRound == 1)
        {
            _firstRoundWinner = winnerBot;
            resetScene();
        }
        else if (_matchRound == 2)
        {
            _secondRoundWinner = winnerBot;
            endMatch();
        }
    }

    private float calcPoints(TournamentBot bot)
    {
        float points = 0;

        if (_firstRoundWinner == bot)
            points++;
        else if (_firstRoundWinner == null)
            points += 0.5f;

        if (_secondRoundWinner == bot)
            points++;
        else if (_secondRoundWinner == null)
            points += 0.5f;

        return points;
    }

    private void endMatch()
    {
        float aPoints = calcPoints(_currentMatch.CompetitorA);
        float bPoints = calcPoints(_currentMatch.CompetitorB);
        Debug.LogError(_currentMatch.NameA + " " + aPoints + ":" + bPoints + " " + _currentMatch.NameB);

        if (aPoints > bPoints)
        {
            // A wins
            _currentMatch.WinA();
        }
        else if (bPoints > aPoints)
        {
            // B wins
            _currentMatch.WinB();
        }
        else
        {
            // Draw
            _currentMatch.Draw();
        }

        clearMatch();
        startMatch();
    }

    private void clearMatch()
    {
        _currentMatch = null;
        _matchRound = 0;
        _firstRoundWinner = null;
        _secondRoundWinner = null;
        _randomMoveCounter = 0;
    }
}
