using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    private void Start()
    {
        FindAnyObjectByType<Image>().sprite = ResourceManager.GetInstance().Load<Sprite>("Keyboard_F");
    }
}
 