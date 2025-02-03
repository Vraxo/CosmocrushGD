using Godot;

namespace CosmocrushGD;

public partial class DefaultButton : Button
{
    public DefaultButton()
    {
        Pressed += OnPressed;
    }

    private void OnPressed()
    {
        GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
    }
}