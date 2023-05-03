using UnityEngine;
using UnityEngine.UI;

public class ButtonColor : MonoBehaviour
{
    public static ButtonColor Instance;

    // コンストラクタをprivate化
    private ButtonColor() {}

    public Button startRecordingButton;
    public Button stopRecordingButton;
    public Color activeColor;
    public Color deactivateColor;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
        }
    }

    public void ActivateStartButton() {
        SetButtonColor(startRecordingButton, activeColor);
    }

    public void DeactivateStartButton() {
        SetButtonColor(startRecordingButton, deactivateColor);
    }

    public void ActivateStopButton() {
        SetButtonColor(stopRecordingButton, activeColor);
    }

    public void DeactivateStopButton() {
        SetButtonColor(stopRecordingButton, deactivateColor);
    }

    private void SetButtonColor(Button button, Color color) {
        ColorBlock colorBlock = button.colors;
        colorBlock.selectedColor = color;
        button.colors = colorBlock;
    }
}
