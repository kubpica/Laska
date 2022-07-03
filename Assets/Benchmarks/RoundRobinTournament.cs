using System.Collections.Generic;

public class RoundRobinTournament
{
    private BergsCircle _circle;

    public int Stage => _circle.Stage;

    public RoundRobinTournament(List<TournamentBot> competitors)
    {
        // Numbering players with starting positions 1, 2, ... n (where n is the number of players in the tournament)
        int i = 1;
        foreach (var c in competitors)
        {
            c.StartingPosition = i;
            i++;
        }

        _circle = new BergsCircle(competitors);
    }

    public void NextStage()
    {
        _circle.NextStage();
    }

    public List<MatchPair> GenerateMatches()
    {
        return _circle.GetPairs();
    }

    public List<MatchPair> GenerateNextMatches()
    {
        NextStage();
        return GenerateMatches();
    }
}