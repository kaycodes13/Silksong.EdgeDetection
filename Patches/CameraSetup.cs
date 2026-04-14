using BepInEx.Configuration;
using EdgeDetection.Components;
using GlobalEnums;
using System;

namespace EdgeDetection.Patches;

[HarmonyPatch(typeof(GameCameras), "Start")]
internal static class CameraSetup {
	static void Postfix(GameCameras __instance) {
		foreach(var pass in PassDefs) {
			var detector = __instance.mainCamera.gameObject.AddComponent<EdgeDetectionPass>();
			detector.Id = pass.Id;
			detector.LineColor = pass.Colour;
			detector.LineWidth = pass.Width;
			detector.Layers = pass.Layers;
			detector.HalfResolution = pass.HalfRes;
			detector.AlphaThreshold = pass.Threshold;

			if (Plugin.GetPassConfig(pass.Id, out var c, out var w, out var h)) {
				c.SettingChanged += (_, e) => detector.LineColor = GetValue<Color>(e);
				w.SettingChanged += (_, e) => detector.LineWidth = GetValue<byte>(e);
				h.SettingChanged += (_, e) => detector.HalfResolution = GetValue<bool>(e);
			}
		}

		static T GetValue<T>(EventArgs e)
			=> (T)((SettingChangedEventArgs)e).ChangedSetting.BoxedValue;

		GameObject sokSceneParticles =
			__instance.sceneParticles.transform.Find("blown_sand_particles").gameObject;

		foreach (Transform t in Utils.Descendants(sokSceneParticles))
			t.gameObject.layer = (int)PhysLayers.DEFAULT;
	}
}
