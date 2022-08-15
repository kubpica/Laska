using UnityEngine;

namespace Laska
{
    public class LevelManager : MonoBehaviourSingleton<LevelManager>
    {
        [GlobalComponent] private GameManager game;

        private static int s_level = 0;
        public const int BOT_OFF = 9;
        public const int BOT_1SEC_LEVEL = 8;

        public AIConfig[] levels;

        public int CurrentLevel => s_level;

        public void SetLevel(int level)
        {
            bool enable = false;
            if (s_level == LevelManager.BOT_OFF && level != LevelManager.BOT_OFF)
                enable = true;

            s_level = level;
            if (level == LevelManager.BOT_OFF)
            {
                ApplyLevel(-1);
            }
            else
            {
                ApplyLevel(level);
            }

            if (enable)
                game.EnableAI();
        }

        public void ApplyLevel(int id)
        {
            if(id < 0)
            {
                game.DisableAI();
                return;
            }
            id = Mathf.Min(id, levels.Length-1);

            if (game.IsAIThinking)
                game.ActivePlayer.AI.EndSearch();

            var l = levels[id];
            game.WhitePlayer.AI.cfg = l;
            game.BlackPlayer.AI.cfg = l;
        }

        public void Start()
        {
            if (s_level == BOT_OFF && GameManager.GetAIMode() != GameManager.AIMode.PlayerVsPlayer)
                s_level = 0;
            SetLevel(s_level);
        }
    }
}

