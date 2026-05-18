using UnityEngine;
using TMPro;

public class WinScreenUI : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    void Start()
    {
        float time = GameManager.Instance.runTime;
        timeText.text = "Your time: " + time.ToString("F2") + "s";
    }
}
