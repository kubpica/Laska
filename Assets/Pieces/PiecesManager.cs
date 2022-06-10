using UnityEngine;

namespace Laska
{
    public class PiecesManager : MonoBehaviourSingleton<PiecesManager>
    {
        /// <summary>
        /// Set to true when AI analyzes moves (without actually making them) so some optimizations can be applied.
        /// </summary>
        public static bool TempMoves;

        [Component] private Graveyard _graveyard;
        [Component] private PiecesSpawner _spawner;
        [GlobalComponent] private GameManager _gameManager;

        public GameObject ColumnHolder => gameObject;
        public Graveyard Graveyard => _graveyard;
        public PiecesSpawner Spawner => _spawner;
        public GameManager GameManager => _gameManager;
    }
}