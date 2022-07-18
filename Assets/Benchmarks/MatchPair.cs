using UnityEngine;

public class MatchPair
{
    public TournamentBot CompetitorA { get; set; }
    public TournamentBot CompetitorB { get; set; }

    public string NameA => CompetitorA.botName + " (" + CompetitorA.Elo + ")";
    public string NameB => CompetitorB.botName + " (" + CompetitorB.Elo + ")";

    public MatchPair(TournamentBot sideA, TournamentBot sideB)
    {
        CompetitorA = sideA;
        CompetitorB = sideB;
    }

    public void WinA()
    {
        updateRating(1);
        CompetitorA.Wins++;
        CompetitorB.Loses++;
        Debug.LogError(NameA + " wins!");
    }

    public void WinB()
    {
        updateRating(0);
        CompetitorB.Wins++;
        CompetitorA.Loses++;
        Debug.LogError(NameB + " wins!");
    }

    public void Draw()
    {
        updateRating(0.5);
        CompetitorA.Draws++;
        CompetitorB.Draws++;
        Debug.LogError(NameA + " <- draw -> " + NameB);
    }

    private void updateRating(double didAWin)
    {
        var aElo = CompetitorA.Elo;
        var bElo = CompetitorB.Elo;
        EloRatingSystem.UpdateRating(ref aElo, ref bElo, didAWin);
        CompetitorA.Elo = aElo;
        CompetitorB.Elo = bElo;
    }
}