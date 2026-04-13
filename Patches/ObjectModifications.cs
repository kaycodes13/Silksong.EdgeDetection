using EdgeDetection.Components;
using GlobalEnums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace EdgeDetection.Patches;

[HarmonyPatch]
internal static class ObjectModifications {

	[HarmonyPatch(typeof(GameManager), "Start")]
	[HarmonyPostfix]
	static void OnSceneLoad() {
		SceneManager.activeSceneChanged -= SceneChangeHandler;
		SceneManager.activeSceneChanged += SceneChangeHandler;

		SceneManager.sceneLoaded -= SceneLoadHandler;
		SceneManager.sceneLoaded += SceneLoadHandler;
		
		static void SceneChangeHandler(Scene _, Scene scene)
			=> Plugin.StartCoroutine(Coro(scene));

		static void SceneLoadHandler(Scene scene, LoadSceneMode mode) {
			if (mode == LoadSceneMode.Additive)
				Plugin.StartCoroutine(Coro(scene));
		}

		static IEnumerator Coro(Scene scene) {
			if (!GameManager.SilentInstance || GameManager.instance.IsNonGameplayScene())
				yield break;

			// some objects don't have their colliders/particle renderers on scene load
			for (int i = 0; i < 2; i++)
				yield return new WaitFrameAndPaused();
			if (!scene.isLoaded)
				yield break;

			foreach (GameObject root in scene.GetRootGameObjects()) {
				ShowColliderHideParticles(root);
				foreach (Transform t in Utils.Descendants(root))
					ShowColliderHideParticles(t.gameObject);
			}
		}
	}

	[HarmonyPatch(
		typeof(ObjectPool), "Spawn",
		[typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion), typeof(bool)]
	)]
	[HarmonyPostfix]
	static void OnObjectSpawned(GameObject __result) {
		ShowColliderHideParticles(__result);
		foreach (Transform t in Utils.Descendants(__result))
			ShowColliderHideParticles(t.gameObject);
	}

	[HarmonyPatch(typeof(HeroController), "Awake")]
	[HarmonyPostfix]
	static void OnHeroAwake(HeroController __instance) {
		__instance.transform.Find("HeroLight").gameObject
			.AddComponent<HideFromCamera>().hideFromEdgeDetectors = true;

		foreach(Transform t in Utils.Descendants(__instance.gameObject))
			ShowColliderHideParticles(t.gameObject);
	}


	static void ShowColliderHideParticles(GameObject go) {
		PhysLayers
			layer = (PhysLayers)go.layer;
		bool
			hasCollider = go.GetComponent<Collider2D>(),
			hasRenderer = go.GetComponent<Renderer>(),
			onColliderLayer = colliderLayers.Contains(layer);

		// some particle systems are present on scene load, not spawned later
		if (go.GetComponent<ParticleSystemRenderer>())
			go.AddComponentIfNotPresent<HideFromCamera>().hideFromEdgeDetectors = true;

		// terrain/etc hitboxes we need to see
		else if (hasCollider && !hasRenderer && onColliderLayer)
			go.AddComponentIfNotPresent<VisualizeCollider>();

		// sometimes lever sprites aren't on the right layer
		else if (!hasCollider && hasRenderer && !onColliderLayer && IsPartOfLever(go))
			go.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
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

	static readonly HashSet<PhysLayers> colliderLayers = [
		PhysLayers.TERRAIN,
		PhysLayers.SOFT_TERRAIN,
		PhysLayers.INTERACTIVE_OBJECT,
		PhysLayers.BOUNCER,
		PhysLayers.DAMAGE_ALL,
		PhysLayers.ENEMY_ATTACK,
		PhysLayers.HERO_ATTACK,
		PhysLayers.PROJECTILES,
	];

}
