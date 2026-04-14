using EdgeDetection.Components;
using EdgeDetection.Structs;
using GlobalEnums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace EdgeDetection.Patches;

[HarmonyPatch]
internal static class ApplyObjectMods {
	#region Patches

	[HarmonyPatch(typeof(GameCameras), "Start")]
	[HarmonyPostfix]
	static void SoKParticles(GameCameras __instance) {
		GameObject sokSceneParticles = __instance.sceneParticles
			.transform.Find("blown_sand_particles").gameObject;

		foreach (Transform t in Utils.WalkHierarchy(sokSceneParticles))
			t.gameObject.layer = ObjectMods.HIDE_LAYER_INT;
	}

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

			for (int i = 0; i < 2; i++) yield return null;
			if (!scene.isLoaded) yield break;

			foreach (Transform t in Utils.WalkHierarchy(scene.GetRootGameObjects())) {
				if (ObjectVisualizer.IsVisualizer(t))
					continue;
				ShowColliderHideParticles(t);
				genericMods.Apply(t);
			}
		}
	}

	[HarmonyPatch(typeof(ObjectPool), "Spawn", [
		typeof(GameObject), typeof(Transform),
		typeof(Vector3), typeof(Quaternion), typeof(bool)
	])]
	[HarmonyPostfix]
	static void OnObjectSpawned(GameObject __result) {
		foreach (Transform t in Utils.SelfAndWalkHierarchy(__result)) {
			ShowColliderHideParticles(t);
			genericMods.Apply(t);
		}
	}

	[HarmonyPatch(typeof(HeroController), "Awake")]
	[HarmonyPostfix]
	static void OnHeroAwake(HeroController __instance) {
		ObjectMods hornetMods = Utils.ReadJsonAsset<ObjectMods>("hornet_modifications.json");
		foreach (Transform t in Utils.WalkHierarchy(__instance.gameObject)) {
			ShowColliderHideParticles(t);
			hornetMods.Apply(t);
		}
		hornetMods.ClearAll();
	}

	#endregion

	#region Utils

	static readonly ObjectMods genericMods = Utils.ReadJsonAsset<ObjectMods>("generic_modifications.json");
	
	static readonly HashSet<PhysLayers> colliderLayers = [
		//PhysLayers.TERRAIN,
		//PhysLayers.SOFT_TERRAIN,
		//PhysLayers.INTERACTIVE_OBJECT,
		PhysLayers.DAMAGE_ALL,
		PhysLayers.ENEMY_ATTACK,
		PhysLayers.HERO_ATTACK,
		PhysLayers.PROJECTILES,
	];

	static void ShowColliderHideParticles(Transform t) {
		GameObject go = t.gameObject;
		PhysLayers layer = (PhysLayers)go.layer;
		Renderer renderer = go.GetComponent<Renderer>();
		bool hasCollider = go.GetComponent<Collider2D>();

		if (!hasCollider) {
			// particles should never be edge detected
			if (renderer is ParticleSystemRenderer)
				go.layer = ObjectMods.HIDE_LAYER_INT;

			// sometimes lever sprites aren't on the right layer
			else if (renderer && go.GetComponent<Lever>()) {
				foreach (Transform t2 in Utils.SelfAndWalkHierarchy(go))
					if (t2.GetComponent<Renderer>() && !t.GetComponent<Collider2D>())
						t2.gameObject.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
			}
		}
		// hazard/etc hitboxes we need to see
		else if (!renderer && (go.GetComponent<DamageHero>() || colliderLayers.Contains(layer)))
			go.AddComponentIfNotPresent<ColliderVisualizer>();
	}

	#endregion
}
