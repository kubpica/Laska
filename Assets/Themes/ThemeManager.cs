using UnityEngine;

public class ThemeManager : MonoBehaviourSingleton<ThemeManager>
{
    public Theme theme;

    public MeshRenderer d4Renderer;
    public Material cheeseMaterial;

    public string SoldierMianownik => theme.soldierMianownik;
    public string SoldierBiernik => theme.soldierBiernik;
    public string OfficerMianownik => theme.officerMianownik;
    public string OfficerBiernik => theme.officerBiernik;
    public string TakesMsg => theme.takesMsg;
    public string MovesMsg => theme.movesMsg;

    public void Start()
    {
        if (theme.d4StinkyCheese)
            d4Renderer.material = cheeseMaterial;
    }
}
