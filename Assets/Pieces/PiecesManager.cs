using UnityEngine;

namespace Laska
{
    public class PiecesManager : MonoBehaviourSingleton<PiecesManager>
    {
        [Component] private Graveyard _graveyard;
        [Component] private PiecesSpawner _spawner;

        public GameObject ColumnHolder => gameObject;
        public Graveyard Graveyard => _graveyard;
        public PiecesSpawner Spawner => _spawner;
    }
}