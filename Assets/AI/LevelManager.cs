using UnityEngine;

namespace Laska
{
    public class LevelManager : MonoBehaviourSingleton<LevelManager>
    {
        [GlobalComponent] private GameManager gameManager; 

        public AIConfig[] levels;

        public void SetLevel(int id)
        {
            if(id < 0)
            {
                gameManager.DisableAI();
                return;
            }
            id = Mathf.Min(id, levels.Length-1);

            var l = levels[id];
            gameManager.WhitePlayer.AI.cfg = l;
            gameManager.BlackPlayer.AI.cfg = l;
        }
    }
}

