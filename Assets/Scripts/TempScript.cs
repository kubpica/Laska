using Laska;
using UnityEngine;

public class TempScript : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Insert))
        {
            LevelManager.Instance.SetLevel(8);
            var gameManager = GameManager.Instance;
            gameManager.PresetAIMode(GameManager.AIMode.AIVsAI);
            gameManager.EnableAI();
            CameraController.Instance.Camera.transform.position = new Vector3(13.81347f, 40.2f, 30.4f);
            MenusManager.Instance.ingame.enabled = false;
        }
    }
}
