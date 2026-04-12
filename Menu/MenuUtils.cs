using BepInEx.Configuration;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using System;
using TeamCherry.Localization;

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
		if (onValueChanged != null)
			elt.Model.OnValueChanged += onValueChanged;
	}
}
