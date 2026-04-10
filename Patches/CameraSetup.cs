using EdgeDetection.Components;
using GlobalEnums;
using System.Collections.Generic;

namespace EdgeDetection.Patches;

[HarmonyPatch(typeof(GameCameras), "Start")]
internal static class CameraSetup {

	private record struct PassDefinition(string Id, PhysLayers[] Layers, Color Colour, uint Width = 3, bool HalfRes = false, float Threshold = 0.4f);

	// TODO should these go in a JSON file or is that overkill
	static readonly List<PassDefinition> detectorDefs = [
		new("TERRAIN", [
			PhysLayers.TERRAIN,
			PhysLayers.INTERACTIVE_OBJECT,
			PhysLayers.BOUNCER,
			PhysLayers.DAMAGE_ALL, // this is actually Water Surface. :|
		], Color.cyan),

		new("HAZARD", [
			PhysLayers.ENEMIES,
			PhysLayers.ENEMY_ATTACK,
			PhysLayers.HERO_ATTACK,
			PhysLayers.PROJECTILES,
			PhysLayers.TERRAIN_DETECTOR, // for boomerangs. might cause issues, idk.
		], Color.red),

		new("PLAYER", [
			PhysLayers.PLAYER,
		], Color.yellow, Threshold: 0.6f), // high threshold cuts hero light

		// TODO: Geo/Shards/Item pickups as a separate layer, maybe? do they have a distinct layer?
		// upon investigation, pretty sure they're on the default layer. might have to just see if
		// there's a way to apply an object-specific shader to them, or do something
		// similar to hitbox visualization with them, or layer a slightly enlarged spriteflash-shaded copy behind them...
	];

	static void Postfix(GameCameras __instance) {
		foreach(var pass in detectorDefs) {
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
