using UnityEngine;

public class TournamentBot : MonoBehaviourExtended
{
    public string botName;
    public double initialElo = 1500;

    [Component] public AIConfig settings;

    private void Start()
    {
        Elo = initialElo;
    }

    public int StartingPosition { get; set; }
    public double Elo { get; set; }
    public int Wins { get; set; }
    public int Loses { get; set; }
    public int Draws { get; set; }

    public void PrintStats()
    {
        Debug.Log($"Name: {botName} Wins : {Wins} Draws: {Draws} Loses: {Loses} Elo: {Elo}");
    }
}
