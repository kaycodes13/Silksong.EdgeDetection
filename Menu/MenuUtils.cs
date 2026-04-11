using BepInEx.Configuration;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using System;

namespace EdgeDetection.Menu;

internal static class MenuUtils {
	public static ListChoiceModel<bool> LocalizedBoolModel()
		=> new([false, true]) {
			DisplayFn = (idx, val)
				=> val ? Localized("BOOL_TRUE") : Localized("BOOL_FALSE")
		};

	public static void Sync<T>(this SelectableValueElement<T> elt, ConfigEntry<T> entry, Action<T>? onValueChanged = null) {
		elt.SynchronizeWith(entry);
		if (onValueChanged != null)
			elt.Model.OnValueChanged += onValueChanged;
	}
}
