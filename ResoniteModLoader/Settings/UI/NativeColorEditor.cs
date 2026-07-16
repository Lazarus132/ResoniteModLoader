using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace ResoniteModLoader;

public sealed class NativeColorEditor : Component
{
    private enum Channel
    {
        Red,
        Green,
        Blue,
        Alpha
    }

#pragma warning disable CS8618
#pragma warning disable CA1051

    public readonly Sync<colorX> Value;

    public readonly Sync<int> SelectedChannel;

    private readonly SyncRef<Slider<float>> _slider;

    private readonly SyncRef<Button> _redButton;
    private readonly SyncRef<Button> _greenButton;
    private readonly SyncRef<Button> _blueButton;
    private readonly SyncRef<Button> _alphaButton;

    private readonly SyncRef<Button> _decreaseButton;
    private readonly SyncRef<Button> _increaseButton;

    private readonly SyncRef<Image> _sliderBackground;
    private readonly SyncRef<Image> _sliderFill;
    private readonly SyncRef<Image> _preview;
    private readonly SyncRef<Image> _background;
    private bool _refreshing;
    private bool _subscribed;

#pragma warning restore CS8618, CA1051

    public void Build(
        UIBuilder ui)
    {
        ui.Style.MinHeight = 24f;
        ui.Style.PreferredHeight = 24f;
        ui.Style.FlexibleHeight = 0f;

        ui.Style.MinWidth = 0f;
        ui.Style.PreferredWidth = 0f;
        ui.Style.FlexibleWidth = 1f;

        HorizontalLayout layout =
            ui.HorizontalLayout(
                spacing: 4f,
                padding: 0f,
                childAlignment:
                    Alignment.MiddleCenter);

        Image background =
            layout.Slot.GetComponent<Image>()
            ?? layout.Slot.AttachComponent<Image>();

        background.Sprite.Target =
            ui.Style.ButtonSprite;

        _background.Target =
            background;

        ui.Style.MinWidth = 56f;
        ui.Style.PreferredWidth = 56f;
        ui.Style.FlexibleWidth = 0f;

        Button red =
            ui.Button((LocaleString)"R 255");

        Button green =
            ui.Button((LocaleString)"G 255");

        Button blue =
            ui.Button((LocaleString)"B 255");

        Button alpha =
            ui.Button((LocaleString)"A 255");

        _redButton.Target = red;
        _greenButton.Target = green;
        _blueButton.Target = blue;
        _alphaButton.Target = alpha;

        red.Pressed.Target =
            SelectRed;

        green.Pressed.Target =
            SelectGreen;

        blue.Pressed.Target =
            SelectBlue;

        alpha.Pressed.Target =
            SelectAlpha;

        ui.Style.MinWidth = 32f;
        ui.Style.PreferredWidth = 32f;
        ui.Style.FlexibleWidth = 0f;

        Button decrease =
            ui.Button((LocaleString)"−");

        _decreaseButton.Target =
            decrease;

        decrease.Pressed.Target =
            Decrease;

        ui.Style.MinWidth = 100f;
        ui.Style.PreferredWidth = 220f;
        ui.Style.FlexibleWidth = 1f;

        Slider<float> slider =
            ui.Slider(
                32f,
                out Image line,
                out Image fillLine,
                out Image handle);

        slider.Min.Value = 0f;
        slider.Max.Value = 255f;

        _slider.Target =
            slider;

        _sliderBackground.Target =
            line;

        _sliderFill.Target =
            fillLine;

        ui.Style.MinWidth = 32f;
        ui.Style.PreferredWidth = 32f;
        ui.Style.FlexibleWidth = 0f;

        Button increase =
            ui.Button((LocaleString)"+");

        _increaseButton.Target =
            increase;

        increase.Pressed.Target =
            Increase;

        ui.NestOut();
    }

    protected override void OnStart()
    {
        base.OnStart();

        SubscribeEvents();
        Refresh();
    }

    private void SubscribeEvents()
    {
        if (_subscribed)
            return;

        Slider<float>? slider =
            _slider.Target;

        if (slider == null)
        {
            UniLog.Error(
                "[NativeColorEditor] Slider reference is missing.");

            return;
        }

        slider.Value.OnValueChange +=
            OnSliderChanged;

        Value.OnValueChange +=
            OnColorChanged;

        _subscribed =
            true;

        UniLog.Log(
            "[NativeColorEditor] Events subscribed.");
    }

    protected override void OnDispose()
    {
        if (_subscribed)
        {
            Slider<float>? slider =
                _slider.Target;

            if (slider != null)
            {
                slider.Value.OnValueChange -=
                    OnSliderChanged;
            }

            Value.OnValueChange -=
                OnColorChanged;

            _subscribed =
                false;
        }

        base.OnDispose();
    }

    private Channel CurrentChannel =>
        (Channel)Math.Clamp(
            SelectedChannel.Value,
            0,
            3);

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    private void SelectRed(
        IButton button,
        ButtonEventData eventData)
    {
        SelectChannel(Channel.Red);
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    private void SelectGreen(
        IButton button,
        ButtonEventData eventData)
    {
        SelectChannel(Channel.Green);
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    private void SelectBlue(
        IButton button,
        ButtonEventData eventData)
    {
        SelectChannel(Channel.Blue);
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    private void SelectAlpha(
        IButton button,
        ButtonEventData eventData)
    {
        SelectChannel(Channel.Alpha);
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    private void Decrease(
        IButton button,
        ButtonEventData eventData)
    {
        ChangeChannel(-1);
    }

    [SyncMethod(
        typeof(ButtonEventHandler),
        new string[] { })]
    private void Increase(
        IButton button,
        ButtonEventData eventData)
    {
        ChangeChannel(1);
    }

    private void SelectChannel(
        Channel channel)
    {
        SelectedChannel.Value =
            (int)channel;

        Refresh();
    }

    private void ChangeChannel(
        int amount)
    {
        int current =
            GetChannelByte(
                Value.Value,
                CurrentChannel);

        int changed =
            Math.Clamp(
                current + amount,
                0,
                255);

        Value.Value =
            SetChannelByte(
                Value.Value,
                CurrentChannel,
                changed);
    }

    private void OnSliderChanged(
        IField<float> field)
    {
        if (_refreshing)
            return;

        colorX before =
            Value.Value;

        int value =
            Math.Clamp(
                (int)MathF.Round(
                    field.Value),
                0,
                255);

        colorX after =
            SetChannelByte(
                before,
                CurrentChannel,
                value);

        UniLog.Log(
            $"[NativeColorEditor] Slider " +
            $"{CurrentChannel}: {value}; " +
            $"before={before}; after={after}");

        Value.Value =
            after;

        UniLog.Log(
            $"Stored Value: {Value.Value}");

        // UI direkt aktualisieren
        SetLabel(
            _redButton.Target,
            FormatLabel("R", FloatToByte(after.r), Channel.Red));

        SetLabel(
            _greenButton.Target,
            FormatLabel("G", FloatToByte(after.g), Channel.Green));

        SetLabel(
            _blueButton.Target,
            FormatLabel("B", FloatToByte(after.b), Channel.Blue));

        SetLabel(
            _alphaButton.Target,
            FormatLabel("A", FloatToByte(after.a), Channel.Alpha));

        UpdateSliderColors(after);
    }

    private void OnColorChanged(
        IField<colorX> field)
    {
        if (_refreshing)
            return;

        Refresh();
    }

    private void Refresh()
    {
        if (_refreshing)
            return;

        _refreshing =
            true;

        try
        {
            colorX color =
                Value.Value;

            int red =
                FloatToByte(
                    color.r);

            int green =
                FloatToByte(
                    color.g);

            int blue =
                FloatToByte(
                    color.b);

            int alpha =
                FloatToByte(
                    color.a);

            SetLabel(
                _redButton.Target,
                FormatLabel(
                    "R",
                    red,
                    Channel.Red));

            SetLabel(
                _greenButton.Target,
                FormatLabel(
                    "G",
                    green,
                    Channel.Green));

            SetLabel(
                _blueButton.Target,
                FormatLabel(
                    "B",
                    blue,
                    Channel.Blue));

            SetLabel(
                _alphaButton.Target,
                FormatLabel(
                    "A",
                    alpha,
                    Channel.Alpha));

            Slider<float>? slider =
                _slider.Target;

            if (slider != null)
            {
                int sliderValue =
                    GetChannelByte(
                        color,
                        CurrentChannel);

                if ((int)MathF.Round(slider.Value.Value) != sliderValue)
                {
                    slider.Value.Value =
                        sliderValue;
                }
            }

            Image? background =
                _background.Target;

            if (background != null)
            {
                background.Tint.Value =
                    new colorX(
                        color.r,
                        color.g,
                        color.b,
                        MathX.Max(
                            color.a * 0.25f,
                            0.08f));
            }

            UpdateSliderColors(
                color);
        }
        finally
        {
            _refreshing =
                false;
        }
    }

    private void UpdateSliderColors(
        colorX color)
    {
        Image? background =
            _sliderBackground.Target;

        Image? fill =
            _sliderFill.Target;

        if (background == null ||
            fill == null)
        {
            return;
        }

        switch (CurrentChannel)
        {
            case Channel.Red:

                fill.Tint.Value =
                    new colorX(
                        1f,
                        0f,
                        0f,
                        1f);

                background.Tint.Value =
                    new colorX(
                        0.25f,
                        0f,
                        0f,
                        1f);

                break;

            case Channel.Green:

                fill.Tint.Value =
                    new colorX(
                        0f,
                        1f,
                        0f,
                        1f);

                background.Tint.Value =
                    new colorX(
                        0f,
                        0.25f,
                        0f,
                        1f);

                break;

            case Channel.Blue:

                fill.Tint.Value =
                    new colorX(
                        0f,
                        0f,
                        1f,
                        1f);

                background.Tint.Value =
                    new colorX(
                        0f,
                        0f,
                        0.25f,
                        1f);

                break;

            default:

                fill.Tint.Value =
                    new colorX(
                        color.r,
                        color.g,
                        color.b,
                        1f);

                background.Tint.Value =
                    new colorX(
                        color.r * 0.25f,
                        color.g * 0.25f,
                        color.b * 0.25f,
                        1f);

                break;
        }
    }

    private string FormatLabel(
        string name,
        int value,
        Channel channel)
    {
        string marker =
            CurrentChannel == channel
                ? "●"
                : string.Empty;

        return
            $"{marker}{name} {value}";
    }

    private static void SetLabel(
        Button? button,
        string value)
    {
        if (button?.Label != null)
        {
            button.Label.Content.Value =
                value;
        }
    }

    private static int GetChannelByte(
        colorX color,
        Channel channel)
    {
        return channel switch
        {
            Channel.Red =>
                FloatToByte(color.r),

            Channel.Green =>
                FloatToByte(color.g),

            Channel.Blue =>
                FloatToByte(color.b),

            _ =>
                FloatToByte(color.a)
        };
    }

    private static colorX SetChannelByte(
        colorX color,
        Channel channel,
        int value)
    {
        float normalized =
            Math.Clamp(
                value,
                0,
                255) / 255f;

        return channel switch
        {
            Channel.Red =>
                color.SetR(normalized),

            Channel.Green =>
                color.SetG(normalized),

            Channel.Blue =>
                color.SetB(normalized),

            _ =>
                color.SetA(normalized)
        };
    }

    private static int FloatToByte(
        float value)
    {
        return Math.Clamp(
            (int)MathF.Round(
                Math.Clamp(
                    value,
                    0f,
                    1f) * 255f),
            0,
            255);
    }
}