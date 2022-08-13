using UnityEngine;

namespace Laska
{
    public class ThemeManager : MonoBehaviourSingleton<ThemeManager>
    {
        public Theme theme;
        public Material cheeseMaterial;

        private Language Language => LanguageManager.Language;

        public string Soldier => themeOrLanguage(theme.soldierMianownik, Language.soldier);
        public string SoldierBiernik => themeOrLanguage(theme.soldierBiernik, Language.soldierBiernik);
        public string Officer => themeOrLanguage(theme.officerMianownik, Language.officer);
        public string OfficerBiernik => themeOrLanguage(theme.officerBiernik, Language.officerBiernik);
        public string Column => themeOrLanguage(theme.column, Language.column);
        public string TakesMsg => themeOrLanguage(theme.takesMsg, Language.takesMsg);
        public string MovesMsg => themeOrLanguage(theme.movesMsg, Language.movesMsg);
        public string EditPosition => themeOrLanguage(theme.editPosition, Language.editPosition);
        public string PositionEval => themeOrLanguage(theme.positionEval, Language.materialEval);
        public string GameOver => themeOrLanguage(theme.gameOver, Language.gameOver);

        public bool IsStinkyCheese => theme.d4StinkyCheese;

        private string themeOrLanguage(string theme, string lang)
        {
            return string.IsNullOrEmpty(theme) ? lang : theme;
        }

        public void ApplyStinkyCheese()
        {
            if (IsStinkyCheese)
            {
                var d4Renderer = Board.Instance.GetSquareAt("d4").GetComponent<MeshRenderer>();
                d4Renderer.material = cheeseMaterial;
            }
        }
    }
}