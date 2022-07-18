using UnityEngine;

namespace Laska
{
    public class LanguageManager : MonoBehaviour
    {
        public static Language Language;
        public static bool IsLanguageSelected;

        public Language defaultLanguage;

        public void Awake()
        {
            if (Language == null)
                Language = defaultLanguage;
        }
    }
}
