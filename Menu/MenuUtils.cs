using BepInEx.Configuration;
using GlobalEnums;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using System;
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
	/// Synchronizes the menu element model's and config entry's values,
	/// and runs another function when the value changes.
	/// </summary>
	internal static void Sync<T>(this SelectableValueElement<T> elt, ConfigEntry<T> entry, Action<T> onValueChanged) {
		elt.Model.OnValueChanged += onValueChanged;
		elt.SynchronizeWith(entry);
	}

	extension(GameObject go) {
		internal RectTransform RectTransform => (RectTransform)go.transform;

		internal void SetAnchors(Vector2 anchor)
			=> go.RectTransform.anchorMax = go.RectTransform.anchorMin = anchor;

		internal void SetSizeDelta(Vector2 size) => go.RectTransform.sizeDelta = size;
	}

	internal static GameObject UIGameObject(string name, GameObject? parent) {
		GameObject go = new(name) {
			layer = (int)PhysLayers.UI
		};
		if (parent)
			go.transform.SetParentReset(parent.transform);
		return go;
	}

	internal static Material UIMaterial(Color? color = null) {
		if (!uiShader)
			uiShader = Shader.Find("UI/Default");
		return new Material(uiShader) {
			color = color ?? Color.white
		};
	}
	static Shader? uiShader;

	internal static IEnumerable<Selectable> AllSelectables(this VerticalGroup group)
		=> group.AllElements().OfType<SelectableElement>().Select(x => x.SelectableComponent);

}
