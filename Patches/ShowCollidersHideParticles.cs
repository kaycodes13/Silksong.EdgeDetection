using EdgeDetection.Components;
using GlobalEnums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace EdgeDetection.Patches;

[HarmonyPatch(typeof(GameManager), "Start")]
internal static class ShowCollidersHideParticles {

	static readonly HashSet<PhysLayers> hitboxLayers = [
		PhysLayers.TERRAIN,
		PhysLayers.SOFT_TERRAIN,
		PhysLayers.INTERACTIVE_OBJECT,
		PhysLayers.BOUNCER,
		PhysLayers.DAMAGE_ALL,
		PhysLayers.ENEMY_ATTACK,
		PhysLayers.HERO_ATTACK,
		PhysLayers.PROJECTILES,
	];

	static void Postfix() {
		SceneManager.activeSceneChanged +=
			(_, scene) => Plugin.StartCoroutine(Handler(scene));
		SceneManager.sceneLoaded +=
			(scene, _) => Plugin.StartCoroutine(Handler(scene));
	}

	static IEnumerator Handler(Scene scene) {
		if (GameManager.instance.IsNonGameplayScene())
			yield break;

		// some objects don't have their colliders/particlesystemrenderers on scene load
		for (int i = 0; i < 2; i++)
			yield return new WaitFrameAndPaused();
		if (!scene.isLoaded)
			yield break;

		foreach (GameObject root in scene.GetRootGameObjects()) {
			Modify(root);
			foreach (Transform t in Utils.Descendants(root))
				Modify(t.gameObject);
		}
	}

	static void Modify(GameObject go) {
		bool
			hasCollider = go.GetComponent<Collider2D>(),
			hasRenderer = go.GetComponent<Renderer>();
		PhysLayers
			layer = (PhysLayers)go.layer;

		// some particle systems are present on scene load, not spawned later
		if (go.GetComponent<ParticleSystemRenderer>())
			go.AddComponentIfNotPresent<HideFromCamera>().hideFromEdgeDetectors = true;
		// terrain/etc hitboxes we need to see
		else if (hasCollider && !hasRenderer && hitboxLayers.Contains(layer)) {
			go.AddComponentIfNotPresent<VisualizeCollider>();
		}
		// sometimes lever sprites aren't on the right layer
		else if (
			!hasCollider && hasRenderer && !hitboxLayers.Contains(layer) && IsPartOfLever(go)
		) {
			go.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
		}
	}

	static bool IsPartOfLever(GameObject go) {
		if (go.GetComponent<Lever>())
			return true;

		bool flag = false;
		while (!flag && go.transform.parent) {
			go = go.transform.parent.gameObject;
			flag = go.GetComponent<Lever>();
		}
		return flag;
	}
}
