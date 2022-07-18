using UnityEngine;

[CreateAssetMenu(fileName = "Theme", menuName = "ScriptableObjects/Theme", order = 1)]
public class Theme : ScriptableObject
{
    public string soldierMianownik;
    public string soldierBiernik;
    public string officerMianownik;
    public string officerBiernik;
    public string column;
    public string takesMsg;
    public string movesMsg;
    public string editPosition;
    public string positionEval;
    public string gameOver;
    public bool d4StinkyCheese;
}
