using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;

namespace EdgeDetection.Menu;

/// <summary>
/// <inheritdoc path="//summary"/>
/// Its width matches the text and choice elements.
/// </summary>
internal class WiderSliderElement<T> : SliderElement<T> {
	public WiderSliderElement(LocalizedText label, SliderModel<T> model) : base(label, model) {
		Vector2
			middleright = new(1, 0.5f),
			middleleft = new(0, 0.5f),
			uppercenter = new(0.5f, 1),
			lowercenter = new(0.5f, 0),
			center = Vector2.one * 0.5f;
		RectTransform
			Slider = RectTransform.Find("Slider").AsRect,
				MenuOptionLabel = Slider.Find("Menu Option Label").AsRect,
				CursorHotspot = Slider.Find("CursorHotspot").AsRect,
					CursorLeft = CursorHotspot.Find("CursorLeft").AsRect,
					CursorRight = CursorHotspot.Find("CursorRight").AsRect;

		// getting a bit magic number-y with this, but. i'm tired.

		RectTransform.sizeDelta = RectTransform.sizeDelta with { x = 720 };
		RectTransform.SetAnchors(center);
		RectTransform.anchoredPosition = RectTransform.anchoredPosition with { x = 0 };

		Slider.SetAnchors(middleright);
		Slider.anchoredPosition = Vector2.zero;

		MenuOptionLabel.anchorMin = Vector2.zero;
		MenuOptionLabel.anchorMax = Vector2.one;
		MenuOptionLabel.pivot = middleleft;
		MenuOptionLabel.anchoredPosition = new Vector2(-(2 * RectTransform.offsetMax.x) - 91, 0);

		CursorHotspot.anchorMax = uppercenter;
		CursorHotspot.anchorMin = lowercenter;
		CursorHotspot.anchoredPosition = new(RectTransform.offsetMin.x, 0);
		CursorHotspot.sizeDelta = new Vector2(1000 + RectTransform.offsetMax.x, 0);

		CursorLeft.SetAnchors(middleleft);
		CursorLeft.anchoredPosition = Vector2.zero;

		CursorRight.SetAnchors(middleright);
		CursorRight.anchoredPosition = Vector2.zero;

		ValueText.alignByGeometry = true;
	}
}
