using TeamCherry.Localization;

namespace EdgeDetection.Menu;

internal static class MenuUtils {
	/// <summary>
	/// <see cref="LocalisedString"/> with the given <paramref name="key"/> for this mod.
	/// </summary>
	internal static LocalisedString Localized(string key) => new($"Mods.{Id}", key);

	extension (Transform t) {
		internal RectTransform AsRect => (RectTransform)t;
		internal void SetAnchors(Vector2 anchor)
			=> t.AsRect.anchorMax = t.AsRect.anchorMin = anchor;
	}
}
