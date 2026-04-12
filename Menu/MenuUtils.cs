using GlobalEnums;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System.Collections.Generic;
using System.Linq;
using TeamCherry.Localization;
using UnityEngine.UI;

namespace EdgeDetection.Menu;

internal static class MenuUtils {
	/// <summary>
	/// <see cref="LocalisedString"/> with the given <paramref name="key"/> for this mod.
	/// </summary>
	internal static LocalisedString Localized(string key) => new($"Mods.{Id}", key);

	/// <summary>
	/// Simple choice model for bools which localizes the values.
	/// </summary>
	internal static ListChoiceModel<bool> LocalizedBoolModel()
		=> new([false, true]) {
			DisplayFn = (idx, val)
				=> val ? Localized("BOOL_TRUE") : Localized("BOOL_FALSE")
		};

	/// <summary>
	/// Creates a GameObject on the UI layer and sets its parent.
	/// </summary>
	internal static GameObject UIGameObject(string name, GameObject? parent) {
		GameObject go = new(name) { layer = (int)PhysLayers.UI };
		if (parent)
			go.transform.SetParentReset(parent.transform);
		return go;
	}

	/// <summary>
	/// Creates a new material using the default UI shader.
	/// </summary>
	internal static Material UIMaterial(Color? color = null) {
		if (!uiShader)
			uiShader = Shader.Find("UI/Default");
		return new Material(uiShader) { color = color ?? Color.white };
	}
	static Shader? uiShader;

	extension (GameObject go) {
		internal RectTransform RectTransform => (RectTransform)go.transform;

		internal void SetAnchors(Vector2 anchor)
			=> go.RectTransform.anchorMax = go.RectTransform.anchorMin = anchor;

		internal void SetSizeDelta(Vector2 size) => go.RectTransform.sizeDelta = size;
	}

	extension (Transform t) {
		internal RectTransform AsRect => (RectTransform)t;
		internal void SetAnchors(Vector2 anchor)
			=> t.AsRect.anchorMax = t.AsRect.anchorMin = anchor;
	}

	extension (VerticalGroup v) {
		internal IEnumerable<Selectable> AllSelectables()
			=> v.AllElements().OfType<SelectableElement>().Select(x => x.SelectableComponent);
	}
}
