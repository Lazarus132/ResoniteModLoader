using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

public sealed class ButtonTextColorDriver : Component
{
    public readonly SyncRef<Button> Button;
    public readonly SyncRef<Text> Label;

    public readonly Sync<colorX> NormalColor;
    public readonly Sync<colorX> HoverColor;
    public readonly Sync<colorX> PressedColor;

    private bool _subscribed;

    protected override void OnStart()
    {
        base.OnStart();

        if (_subscribed)
            return;

        if (Button.Target == null ||
            Label.Target == null)
            return;

        Button.Target.HoverEnter.Target += HoverEnter;
        Button.Target.HoverLeave.Target += HoverLeave;
        Button.Target.Pressed.Target += Pressed;
        Button.Target.Released.Target += Released;

        Label.Target.Color.Value =
            NormalColor.Value;

        _subscribed = true;
    }

    protected override void OnDispose()
    {
        if (_subscribed &&
            Button.Target != null)
        {
            Button.Target.HoverEnter.Target -= HoverEnter;
            Button.Target.HoverLeave.Target -= HoverLeave;
            Button.Target.Pressed.Target -= Pressed;
            Button.Target.Released.Target -= Released;
        }

        base.OnDispose();
    }

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    private void HoverEnter(
        IButton button,
        ButtonEventData data)
    {
        if (Label.Target != null)
            Label.Target.Color.Value =
                HoverColor.Value;
    }

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    private void HoverLeave(
        IButton button,
        ButtonEventData data)
    {
        if (Label.Target != null)
            Label.Target.Color.Value =
                NormalColor.Value;
    }

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    private void Pressed(
        IButton button,
        ButtonEventData data)
    {
        if (Label.Target != null)
            Label.Target.Color.Value =
                PressedColor.Value;
    }

    [SyncMethod(typeof(ButtonEventHandler), new string[] { })]
    private void Released(
        IButton button,
        ButtonEventData data)
    {
        if (Label.Target == null ||
            Button.Target == null)
            return;

        Label.Target.Color.Value =
            Button.Target.IsHovering.Value
                ? HoverColor.Value
                : NormalColor.Value;
    }
}