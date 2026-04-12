using EdgeDetection.Components;

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
		}
	}
}
