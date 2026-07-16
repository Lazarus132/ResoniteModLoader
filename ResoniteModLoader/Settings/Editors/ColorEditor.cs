using System;
using System.Collections.Generic;
using System.Globalization;
using FrooxEngine;
using FrooxEngine.UIX;
using Elements.Core;

namespace ResoniteModLoader;

public sealed class ColorEditor
    : IValueEditor,
      IButtonValueEditor
{
    private const string SerializedPrefix =
        "[color]#";

    private enum ColorChannel
    {
        Red,
        Green,
        Blue,
        Alpha
    }

    private string _serializedValue =
        string.Empty;

    private byte _red;
    private byte _green;
    private byte _blue;
    private byte _alpha;

    private ColorChannel _selectedChannel =
        ColorChannel.Red;

    private Slider<float>? _valueSlider;
    private Image? _sliderBackground;
    private Image? _sliderFill;
    private Image? _editorBackground;

    private Button? _redButton;
    private Button? _greenButton;
    private Button? _blueButton;
    private Button? _alphaButton;

    private Button? _decreaseButton;
    private Button? _increaseButton;

    private readonly List<Button> _editorButtons =
        new();

    public string Prefix =>
        "color";

    public string SerializedValue
    {
        get
        {
            CommitSliderValue();
            return _serializedValue;
        }
    }

    public IEnumerable<Button> EditorButtons =>
        _editorButtons;

    public void SetSerializedValue(
        string value)
    {
        _serializedValue =
            string.IsNullOrWhiteSpace(value)
                ? "[color]#FFFFFFFF"
                : value;

        ParseSerializedValue();

        if (_valueSlider != null)
        {
            _valueSlider.Value.Value =
                GetSelectedChannelValue();
        }

        RefreshEditor();
    }

    public Component Build(
        UIBuilder ui)
    {
        /*
        * HorizontalElementWithLabel() hat den UIBuilder an dieser
        * Stelle bereits in den rechten Split-Bereich verschachtelt.
        *
        * Deshalb KEINEN eigenen Slot und KEINEN nackten
        * RectTransform erzeugen. HorizontalLayout() erzeugt selbst
        * ein korrektes Layout-Element innerhalb dieses Bereichs.
        */

        ui.Style.MinHeight = 32f;
        ui.Style.PreferredHeight = 32f;
        ui.Style.FlexibleHeight = 0f;

        ui.Style.MinWidth = 0f;
        ui.Style.PreferredWidth = 0f;
        ui.Style.FlexibleWidth = 1f;

        HorizontalLayout layout =
            ui.HorizontalLayout(
                4f,
                0f,
                Alignment.MiddleCenter);

        _editorBackground =
            layout.Slot.GetComponent<Image>();

        if (_editorBackground == null)
        {
            _editorBackground =
                layout.Slot.AttachComponent<Image>();

            _editorBackground.Sprite.Target =
                ui.Style.ButtonSprite;

            _editorBackground.Tint.Value =
                new colorX(
                    0f,
                    0f,
                    0f,
                    0f);
        }

        /*
        * Kanalauswahl.
        */

        ui.Style.MinWidth = 68f;
        ui.Style.PreferredWidth = 68f;
        ui.Style.FlexibleWidth = 0f;

        _redButton =
            ui.Button(
                (LocaleString)"R 255");

        _greenButton =
            ui.Button(
                (LocaleString)"G 255");

        _blueButton =
            ui.Button(
                (LocaleString)"B 255");

        _alphaButton =
            ui.Button(
                (LocaleString)"A 255");

        /*
        * Exakte Verringerung.
        */

        ui.Style.MinWidth = 32f;
        ui.Style.PreferredWidth = 32f;
        ui.Style.FlexibleWidth = 0f;

        _decreaseButton =
            ui.Button(
                (LocaleString)"−");

        /*
        * Nativer UIX-Slider.
        */

        ui.Style.MinWidth = 100f;
        ui.Style.PreferredWidth = 220f;
        ui.Style.FlexibleWidth = 1f;

        Image line;
        Image fillLine;
        Image handle;

        _valueSlider =
            ui.Slider(
                32f,
                out line,
                out fillLine,
                out handle);

        _valueSlider.Min.Value = 0f;
        _valueSlider.Max.Value = 255f;
        _valueSlider.Value.Value =
            GetSelectedChannelValue();

        _valueSlider.Value.OnValueChange +=
            _ =>
            {
                UpdateFromSlider();
            };

        _sliderBackground =
            line;

        _sliderFill =
            fillLine;

        /*
        * Exakte Erhöhung.
        */

        ui.Style.MinWidth = 32f;
        ui.Style.PreferredWidth = 32f;
        ui.Style.FlexibleWidth = 0f;

        _increaseButton =
            ui.Button(
                (LocaleString)"+");

        /*
        * HorizontalLayout() hat genau eine Verschachtelung geöffnet.
        */

        ui.NestOut();

        RegisterEditorButton(
            _redButton);

        RegisterEditorButton(
            _greenButton);

        RegisterEditorButton(
            _blueButton);

        RegisterEditorButton(
            _alphaButton);

        RegisterEditorButton(
            _decreaseButton);

        RegisterEditorButton(
            _increaseButton);

        UpdateButtonLabels();

        /*
        * Style für nachfolgende Settings wieder neutralisieren.
        */

        ui.Style.MinWidth = 0f;
        ui.Style.PreferredWidth = 0f;
        ui.Style.FlexibleWidth = 1f;

        ui.Style.MinHeight = 32f;
        ui.Style.PreferredHeight = 32f;
        ui.Style.FlexibleHeight = 0f;

        return layout;
    }

    public void HandleButton(
        IButton button)
    {
        /*
         * Always store the currently visible slider position before
         * changing channel or applying an exact increment.
         */
        CommitSliderValue();

        if (ReferenceEquals(
                button,
                _redButton))
        {
            SelectChannel(
                ColorChannel.Red);

            return;
        }

        if (ReferenceEquals(
                button,
                _greenButton))
        {
            SelectChannel(
                ColorChannel.Green);

            return;
        }

        if (ReferenceEquals(
                button,
                _blueButton))
        {
            SelectChannel(
                ColorChannel.Blue);

            return;
        }

        if (ReferenceEquals(
                button,
                _alphaButton))
        {
            SelectChannel(
                ColorChannel.Alpha);

            return;
        }

        if (ReferenceEquals(
                button,
                _decreaseButton))
        {
            ChangeSelectedChannel(
                -1);

            return;
        }

        if (ReferenceEquals(
                button,
                _increaseButton))
        {
            ChangeSelectedChannel(
                1);
        }
    }

    private void RegisterEditorButton(
        Button? button)
    {
        if (button != null)
        {
            _editorButtons.Add(
                button);
        }
    }

    private void SelectChannel(
        ColorChannel channel)
    {
        _selectedChannel =
            channel;

        if (_valueSlider != null)
        {
            _valueSlider.Value.Value =
                GetSelectedChannelValue();
        }

        RefreshEditor();
    }

    private void ChangeSelectedChannel(
        int amount)
    {
        int currentValue =
            GetSelectedChannelValue();

        int changedValue =
            Math.Clamp(
                currentValue + amount,
                0,
                255);

        SetSelectedChannelValue(
            (byte)changedValue);

        if (_valueSlider != null)
        {
            _valueSlider.Value.Value =
                changedValue;
        }

        RefreshEditor();
    }

    private void CommitSliderValue()
    {
        if (_valueSlider == null)
            return;

        float sliderValue =
            _valueSlider.Value.Value;

        int roundedValue =
            (int)MathF.Round(
                sliderValue);

        roundedValue =
            Math.Clamp(
                roundedValue,
                0,
                255);

        SetSelectedChannelValue(
            (byte)roundedValue);

        /*
         * Snap the native continuous slider back onto an exact byte.
         */
        _valueSlider.Value.Value =
            roundedValue;

        RefreshEditor();
    }

    private int GetSelectedChannelValue()
    {
        return _selectedChannel switch
        {
            ColorChannel.Red =>
                _red,

            ColorChannel.Green =>
                _green,

            ColorChannel.Blue =>
                _blue,

            ColorChannel.Alpha =>
                _alpha,

            _ =>
                0
        };
    }

    private void SetSelectedChannelValue(
        byte value)
    {
        switch (_selectedChannel)
        {
            case ColorChannel.Red:
                _red = value;
                break;

            case ColorChannel.Green:
                _green = value;
                break;

            case ColorChannel.Blue:
                _blue = value;
                break;

            case ColorChannel.Alpha:
                _alpha = value;
                break;
        }
    }

    private void ParseSerializedValue()
    {
        string value =
            _serializedValue.Trim();

        if (!value.StartsWith(
                SerializedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string hex =
            value.Substring(
                SerializedPrefix.Length);

        if (hex.Length != 8)
        {
            return;
        }

        try
        {
            _red =
                ParseByte(
                    hex,
                    0);

            _green =
                ParseByte(
                    hex,
                    2);

            _blue =
                ParseByte(
                    hex,
                    4);

            _alpha =
                ParseByte(
                    hex,
                    6);

            UpdateSerializedValue();
        }
        catch (FormatException)
        {
            SetDefaultColor();
        }
        catch (OverflowException)
        {
            SetDefaultColor();
        }
    }

    private static byte ParseByte(
        string hex,
        int startIndex)
    {
        return byte.Parse(
            hex.Substring(
                startIndex,
                2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);
    }

    private void SetDefaultColor()
    {
        _red = 255;
        _green = 255;
        _blue = 255;
        _alpha = 255;

        RefreshEditor();
    }

    private void UpdateSerializedValue()
    {
        _serializedValue =
            $"{SerializedPrefix}" +
            $"{_red:X2}" +
            $"{_green:X2}" +
            $"{_blue:X2}" +
            $"{_alpha:X2}";
    }

    private void RefreshEditor()
    {
        UpdateSerializedValue();
        UpdateButtonLabels();
        UpdateSliderColors();
    }

    private void UpdateButtonLabels()
    {
        SetButtonLabel(
            _redButton,
            FormatChannelLabel(
                "R",
                _red,
                ColorChannel.Red));

        SetButtonLabel(
            _greenButton,
            FormatChannelLabel(
                "G",
                _green,
                ColorChannel.Green));

        SetButtonLabel(
            _blueButton,
            FormatChannelLabel(
                "B",
                _blue,
                ColorChannel.Blue));

        SetButtonLabel(
            _alphaButton,
            FormatChannelLabel(
                "A",
                _alpha,
                ColorChannel.Alpha));
    }

    private string FormatChannelLabel(
        string name,
        byte value,
        ColorChannel channel)
    {
        string selectionMarker =
            _selectedChannel == channel
                ? "●"
                : "";

        return
            $"{selectionMarker}{name} {value}";
    }

    private static void SetButtonLabel(
        Button? button,
        string text)
    {
        if (button?.Label != null)
        {
            button.Label.Content.Value =
                text;
        }
    }

    private void UpdateSliderColors()
    {
        if (_sliderBackground == null ||
            _sliderFill == null)
        {
            return;
        }

        colorX fillColor;
        colorX backgroundColor;

        switch (_selectedChannel)
        {
            case ColorChannel.Red:

                fillColor =
                    new colorX(
                        1f,
                        0f,
                        0f,
                        1f);

                backgroundColor =
                    new colorX(
                        0.25f,
                        0f,
                        0f,
                        1f);

                break;

            case ColorChannel.Green:

                fillColor =
                    new colorX(
                        0f,
                        1f,
                        0f,
                        1f);

                backgroundColor =
                    new colorX(
                        0f,
                        0.25f,
                        0f,
                        1f);

                break;

            case ColorChannel.Blue:

                fillColor =
                    new colorX(
                        0f,
                        0f,
                        1f,
                        1f);

                backgroundColor =
                    new colorX(
                        0f,
                        0f,
                        0.25f,
                        1f);

                break;

            default:

                fillColor =
                    new colorX(
                        _red / 255f,
                        _green / 255f,
                        _blue / 255f,
                        1f);

                backgroundColor =
                    new colorX(
                        _red / 1020f,
                        _green / 1020f,
                        _blue / 1020f,
                        0.4f);

                break;
        }

        _sliderFill.Tint.Value =
            fillColor;

        _sliderBackground.Tint.Value =
            backgroundColor;

        if (_editorBackground != null)
        {
            _editorBackground.Tint.Value =
                new colorX(
                    _red / 255f,
                    _green / 255f,
                    _blue / 255f,
                    _alpha / 255f);
        }
    }

    private void UpdateFromSlider()
    {
        if (_valueSlider == null)
            return;

        int value =
            Math.Clamp(
                (int)MathF.Round(
                    _valueSlider.Value.Value),
                0,
                255);

        SetSelectedChannelValue(
            (byte)value);

        RefreshEditor();
    }
}