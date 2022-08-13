using UnityEngine;

[CreateAssetMenu(fileName = "Language", menuName = "ScriptableObjects/Language", order = 1)]
public class Language : ScriptableObject
{
    public string botOff = "Bot wyłączony";
    public string level = "Poziom";
    public string newGame = "Nowa gra";
    public string editPosition = "Edytuj pozycję";
    public string autoRotate = "Auto. obróć:";
    public string screen = "ekran";
    public string board = "planszę";
    public string rotate = "Obróć";
    public string automatically = "automatycznie";
    public string playAsGreen = "Graj zielonymi";
    public string playAsRed = "Graj czerwonymi";
    public string playerVsPlayer = "Gracz vs gracz";
    public string botVsBot = "Bot vs bot";
    public string mate = "Pat-mat!";
    public string greenWins = "Wygrana gracza zielonego";
    public string redWins = "Wygrana gracza czerwonego";
    public string drawByRepetition = "Remis przez powtórzenie!";
    public string drawBy50MoveRule = "Remis przez 50 ruchów bez bicia ani posunięcia szeregowym!";
    public string greenPlayerToMove = "Ruch gracza zielonego";
    public string redPlayerToMove = "Ruch gracza czerwonego";
    public string gameOver = "Koniec gry! :)";
    public string materialEval = "Ocena materiału:";
    public string column = "Kolumna";
    public string square = "Pole";
    public string soldier = "Szeregowy";
    public string soldierBiernik = "szeregowego";
    public string officer = "Oficer";
    public string officerBiernik = "oficera";
    public string takesMsg = "bierze do niewoli";
    public string movesMsg = "rusza z";
    public string on = "na";
    public string from = "z";
    public string to = "na";
    public string editor = "Edytor";
    public string deleting = "Usuwanie";
    public string exit = "Opuść";
    public string restoreDefault = "Przywróć domyślną";
    public string greenToMove = "Ruch zielonego";
    public string redToMove = "Ruch czerwonego";
    public string green = "Zielony";
    public string red = "Czerwony";
    public string piecesLimitReached = "Limit 100 bierek został osiągnięty!";
    public string reviewApp = "Oceń aplikację ;)";
    public string reviewFailed = "Nie udało się wczytać oceniania w apliacji, proszę odwiedź sklep Play.";
    public string loading = "Wczytywanie...";
    public string myOtherGames = "Moje inne gry";
    public string aiIsThinking = "Bot myśli.";
    public string forceMove = "Wymuś ruch";
    public string secondAbbreviated = "sek.";
    public string searchedDepth = "Przeszukana głębokość:";
}
