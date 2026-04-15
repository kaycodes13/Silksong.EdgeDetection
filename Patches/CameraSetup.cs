using BepInEx.Configuration;
using EdgeDetection.Components;
using System;

namespace EdgeDetection.Patches;

[HarmonyPatch(typeof(GameCameras), "Start")]
internal static class CameraSetup {
	static void Postfix(GameCameras __instance) {
		foreach(var pass in PassDefs) {
			var detector = __instance.mainCamera.gameObject.AddComponent<EdgeDetectionPass>();
			(detector.Id,
			detector.LineColor,
			detector.LineWidth,
			detector.HalfResolution,
			detector.Layers,
			detector.AlphaThreshold,
			detector.ClipFar,
			detector.ClipNear,
			detector.ExcludePass) = pass;

			if (Plugin.GetPassConfig(pass.Id, out var c, out var w, out var h)) {
				c.SettingChanged += (_, e) => detector.LineColor = GetValue<Color>(e);
				w.SettingChanged += (_, e) => detector.LineWidth = GetValue<byte>(e);
				h.SettingChanged += (_, e) => detector.HalfResolution = GetValue<bool>(e);
			}
		}

		static T GetValue<T>(EventArgs e)
			=> (T)((SettingChangedEventArgs)e).ChangedSetting.BoxedValue;
	}
}
