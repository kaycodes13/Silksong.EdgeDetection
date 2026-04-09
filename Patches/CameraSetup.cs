using EdgeDetection.Components;
using GlobalEnums;
using System.Collections.Generic;
using System.Linq;

namespace EdgeDetection.Patches;

[HarmonyPatch(typeof(GameCameras), "Start")]
internal static class CameraSetup {

	static readonly Dictionary<Color, PhysLayers[]> detectorDefs = new() {
		{ Color.cyan, [
			PhysLayers.TERRAIN,
			PhysLayers.INTERACTIVE_OBJECT,
			PhysLayers.BOUNCER,
			PhysLayers.DAMAGE_ALL, // this is actually Water Surface. :|
		] },
		{ Color.red, [
			PhysLayers.ENEMIES,
			PhysLayers.ENEMY_ATTACK,
			PhysLayers.HERO_ATTACK,
			PhysLayers.PROJECTILES
		] },
		{ Color.yellow, [PhysLayers.PLAYER] },
		// TODO: Geo/Shards/Item pickups as a separate layer, maybe? do they have a distinct layer?
		// upon investigation, pretty sure they're on the default layer. might have to just see if
		// there's a way to apply an object-specific shader to them, or do something
		// similar to hitbox visualization with them, or layer a slightly enlarged spriteflash-shaded copy behind them...
	};

	static void Postfix(GameCameras __instance) {
		foreach(var (colour, layers) in detectorDefs) {
			var detector = __instance.mainCamera.gameObject.AddComponent<EdgeDetectionPass>();
			detector.LineColor = colour;
			detector.LineWidth = 3;
			detector.Layers = layers;

			if (layers.Contains(PhysLayers.PLAYER))
				detector.AlphaThreshold = 0.6f; // cuts out hero light
		}
	}

}
