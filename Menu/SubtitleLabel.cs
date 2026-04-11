using Silksong.ModMenu.Elements;
using UnityEngine.UI;

namespace EdgeDetection.Menu;

internal class SubtitleLabel : TextLabel {
	public SubtitleLabel(LocalizedText text) : base(text) {
		SetFontSizes(FontSizes.Large);
		Text.fontStyle = FontStyle.Italic;
		ApplyDefaultColors = true;
	}
}
