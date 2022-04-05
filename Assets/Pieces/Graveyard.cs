using System.Collections.Generic;

namespace Laska
{
    public class Graveyard : MonoBehaviourSingleton<Graveyard>
    {
        public Stack<Column> Columns = new Stack<Column>();
    }
}