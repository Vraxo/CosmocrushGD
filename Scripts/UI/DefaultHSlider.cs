using Godot;

namespace CosmocrushGD;

public partial class DefaultHSlider : HSlider
{
    public DefaultHSlider()
    {
        ValueChanged += OnValueChanged;
    }

    private void OnValueChanged(double value)
    {
        GlobalAudioPlayer.Instance.PlaySound(GlobalAudioPlayer.Instance.UiSound);
    }
}