using GlobalEnums;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System.Globalization;
using UnityEngine.UI;

namespace EdgeDetection.Menu;

/// <summary>
/// Selectable element that accepts <see cref="Color"/> input in 6-character hex strings.
/// Includes a preview swatch beside the hex code.
/// </summary>
internal class HexColorInput : TextInput<Color> {

	readonly GameObject swatch;

	public HexColorInput(LocalizedText label)
		: this(label, (LocalizedText)"") {}

	public HexColorInput(LocalizedText label, LocalizedText description)
		: base(label, HexRGBModel(), description)
	{
		Container.name = $"{label.Text}Input";
		InputField.characterLimit = 6;
		InputField.contentType = InputField.ContentType.Custom;
		InputField.onValidateInput = HexValidation;
		ApplyDefaultColors = true;

		swatch = new("Swatch") { layer = (int)PhysLayers.UI };
		swatch.transform.SetParent(InputField.transform.Find("Menu Option Text"), false);
		swatch.transform.Reset();

		var image = swatch.AddComponent<Image>();
		image.material = new Material(Shader.Find("UI/Default")) { color = Color.white };

		var outline = swatch.AddComponent<Outline>();
		outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1);
		outline.effectDistance = new(3, 3);

		var swatchT = (RectTransform)swatch.transform;
		swatchT.sizeDelta = new Vector2(40, 40);
		swatchT.anchorMax = swatchT.anchorMin = new Vector2(0, 0.5f);

		Model.OnValueChanged += color => image.material.color = color;
	}

	/// <summary>
	/// <see cref="InputField"/> validation for hex codes; only accepts chars a-fA-F0-7.
	/// </summary>
	public static char HexValidation(string input, int index, char addedChar)
		=> Parse($"{addedChar}", out _) ? addedChar : '0';

	/// <summary>
	/// Text model which parses 6-character hex strings to and from <see cref="Color"/>s.
	/// </summary>
	public static ParserTextModel<Color> HexRGBModel()
		=> new(HexParser, HexUnparser, InvalidColor);

	static bool HexParser(string x, out Color c) {
		c = InvalidColor;
		x = x.Replace("#", "").Trim();

		if (x.Length < 6)
			return false;
		if (Parse(x[0..2], out var r) && Parse(x[2..4], out var g) && Parse(x[4..6], out var b)) {
			c = new Color32(r, g, b, 255);
			return true;
		}
		return false;
	}

	static bool HexUnparser(Color c, out string x) {
		if (In01(c.r) && In01(c.g) && In01(c.b)) {
			Color32 c32 = c;
			x = $"{c32.r:X2}{c32.g:X2}{c32.b:X2}";
		}
		else
			x = $"######";
		return true;

		static bool In01(float f) => 0 <= f && f <= 1;
	}

	static bool Parse(string s, out byte b)
		=> byte.TryParse(s, NumberStyles.HexNumber, null, out b);

	static readonly Color InvalidColor = new(-1, -1, -1, -1);

}
