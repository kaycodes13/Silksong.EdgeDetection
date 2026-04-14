using EdgeDetection.Components;
using GlobalEnums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

			// some objects don't have their colliders/renderers immediately
			for (int i = 0; i < 2; i++)
				yield return null;
			if (!scene.isLoaded)
				yield break;

			foreach (GameObject root in scene.GetRootGameObjects())
			foreach (Transform t in Utils.SelfAndDescendants(root)) {
				if (ObjectVisualizer.IsVisualizer(t))
					continue;
				ShowColliderHideParticles(t.gameObject);
				ApplyObjectMods(t.gameObject, genericMods);
			}
		}
	}

	[HarmonyPatch(
		typeof(ObjectPool), "Spawn",
		[typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion), typeof(bool)]
	)]
	[HarmonyPostfix]
	static void OnObjectSpawned(GameObject __result) {
		foreach (Transform t in Utils.SelfAndDescendants(__result)) {
			ShowColliderHideParticles(t.gameObject);
			ApplyObjectMods(t.gameObject, genericMods);
		}
	}

	[HarmonyPatch(typeof(HeroController), "Awake")]
	[HarmonyPostfix]
	static void OnHeroAwake(HeroController __instance) {
		ObjectMods hornetMods = Utils.ReadJsonAsset<ObjectMods>("hornet_modifications.json");
		foreach (Transform t in Utils.Descendants(__instance.gameObject)) {
			ShowColliderHideParticles(t.gameObject);
			ApplyObjectMods(t.gameObject, hornetMods);
		}
		hornetMods.Clear();
	}

	static void ShowColliderHideParticles(GameObject go) {
		PhysLayers
			layer = (PhysLayers)go.layer;
		bool
			hasCollider = go.GetComponent<Collider2D>(),
			hasRenderer = go.GetComponent<Renderer>(),
			onColliderLayer = colliderLayers.Contains(layer);

		if (go.GetComponent<ParticleSystemRenderer>())
			go.AddComponentIfNotPresent<HideFromCamera>().hideFromEdgeDetectors = true;

		// terrain/etc hitboxes we need to see
		else if (hasCollider && !hasRenderer && onColliderLayer)
			go.AddComponentIfNotPresent<ColliderVisualizer>();

		// sometimes lever sprites aren't on the right layer
		else if (!hasCollider && hasRenderer && !onColliderLayer && IsPartOfLever(go))
			go.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
		
		static bool IsPartOfLever(GameObject go) {
			if (go.GetComponent<Lever>()) return true;
			while (go.transform.parent) {
				go = go.transform.parent.gameObject;
				if (go.GetComponent<Lever>()) return true;
			}
			return false;
		}
	}

	const string REGEX_UNITYCLONE = @"\s?\([a-zA-Z0-9]+\)";

	static void ApplyObjectMods(GameObject go, ObjectMods mods) {
		string name = Regex.Replace(go.name, REGEX_UNITYCLONE, "").Trim();

		if (mods.HideRenderer.Contains(name))
			go.layer = (int)PhysLayers.DEFAULT;

		else if (mods.HideCollider.Contains(name) && go.GetComponent<Collider2D>())
			go.AddComponent<RemoveColliderVisualizer>();

		else if (mods.ChangeLayers.TryGetValue(name, out var changeLayer)) {
			foreach (Transform t in Utils.SelfAndDescendants(go))
				if (t.GetComponent<Renderer>())
					t.gameObject.layer = (int)changeLayer;
		}

		else if (mods.DupeSprite.TryGetValue(name, out var dupeLayer))
			go.AddComponentIfNotPresent<SpriteVisualizer>().layer = dupeLayer;

		else if (mods.HideSubColliders.TryGetValue(name, out var layer)) {
			go.layer = (int)layer;
			foreach (Transform t in Utils.Descendants(go))
				t.gameObject.AddComponent<RemoveColliderVisualizer>();
		}
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

	static readonly ObjectMods genericMods = Utils.ReadJsonAsset<ObjectMods>("generic_modifications.json");

	/// <summary>
	/// Deserialization struct for specifying which GameObjects should receive one of
	/// a set of simple modifications intended to make edge detection look better.
	/// </summary>
	/// <param name="HideRenderer">Objects which will have their layer set to DEFAULT.</param>
	/// <param name="HideCollider">Objects which will not have their collider visualized.</param>
	/// <param name="DupeSprite">
	///		Key = object to have its sprite invisibly duped,
	///		value = layer to put the dupe on
	/// </param>
	/// <param name="HideSubColliders">
	///		Key = object whose descendants won't have collider visualization,
	///		value = layer to set the object to.
	///	</param>
	[Serializable]
	record struct ObjectMods(
		HashSet<string> HideRenderer,
		HashSet<string> HideCollider,
		Dictionary<string, PhysLayers> ChangeLayers,
		Dictionary<string, PhysLayers> DupeSprite,
		Dictionary<string, PhysLayers> HideSubColliders
	) {
		public readonly void Clear() {
			HideRenderer.Clear();
			HideCollider.Clear();
			ChangeLayers.Clear();
			DupeSprite.Clear();
			HideSubColliders.Clear();
		}
	};

}
