using EdgeDetection.Components;
using GlobalEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EdgeDetection.Structs;

/// <summary>
/// Deserialization struct for specifying which GameObjects should receive one of
/// a set of standard modifications intended to make edge detection look better,
/// and for applying those modifications at runtime.
/// </summary>
/// <param name="HideViaLayer">
///		The object's layer changes to <see cref="HIDE_LAYER"/>.
/// </param>
/// <param name="ChangeLayer">
///		The object's layer changed to the one specified.
/// </param>
/// <param name="ChangeAllLayers">
///		All renderer-having collider-free objects in this entire hierarchy
///		change to the layer specified.
/// </param>
/// <param name="HideFromDetectors">
///		The object gets a <see cref="HideFromCamera"/> set to hide from edge detectors.
/// </param>
/// <param name="HideCollider">
///		The object gets a <see cref="RemoveColliderVisualizer"/>.
/// </param>
/// <param name="HideSubColliders">
///		All descendants get a <see cref="RemoveColliderVisualizer"/>.
/// </param>
/// <param name="VisualizeSprite">
///		The object gets a <see cref="SpriteVisualizer"/> set to the layer specified.
/// </param>
[Serializable]
record struct ObjectMods(
	HashSet<string> HideViaLayer,
	Dictionary<string, PhysLayers> ChangeLayer,
	Dictionary<string, PhysLayers> ChangeAllLayers,
	HashSet<string> HideFromDetectors,
	HashSet<string> HideCollider,
	HashSet<string> HideSubColliders,
	Dictionary<string, PhysLayers> VisualizeSprite
) {
	public const PhysLayers HIDE_LAYER = PhysLayers.PARTICLE;
	public const int HIDE_LAYER_INT = (int)HIDE_LAYER;


	public readonly void Apply(Transform t) => Apply(t.gameObject);

	public readonly void Apply(GameObject go) {
		string name = CleanName(go);

		if (ChangeLayer?.TryGetValue(name, out PhysLayers a) ?? false)
			go.layer = (int)a;

		if (ChangeAllLayers?.TryGetValue(name, out PhysLayers b) ?? false) {
			foreach (Transform t in Utils.SelfAndWalkHierarchy(go))
				if (t.GetComponent<Renderer>() && !t.GetComponent<Collider2D>())
					t.gameObject.layer = (int)b;
		}

		if (HideViaLayer?.Contains(name) ?? false)
			go.layer = HIDE_LAYER_INT;

		if (HideFromDetectors?.Contains(name) ?? false)
			go.AddComponentIfNotPresent<HideFromCamera>().hideFromEdgeDetectors = true;

		if (HideCollider?.Contains(name) ?? false)
			go.AddComponent<RemoveColliderVisualizer>();

		if (HideSubColliders?.Contains(name) ?? false) {
			foreach (Transform t in Utils.WalkHierarchy(go))
				t.gameObject.AddComponent<RemoveColliderVisualizer>();
		}

		if (VisualizeSprite?.TryGetValue(name, out PhysLayers c) ?? false)
			go.AddComponentIfNotPresent<SpriteVisualizer>().layer = c;
	}

	/// <summary>
	/// Removes the '(Clone)' or '(4)' from the end of an object's name.
	/// </summary>
	public static string CleanName(GameObject go)
		=> Regex.Replace(go.name, @"\s?\([a-zA-Z0-9]+\)", "").Trim();

	/// <summary>
	/// Returns a new <see cref="ObjectMods"/> containing the union of
	/// each property of <paramref name="one"/> and <paramref name="two"/>.
	/// </summary>
	public static ObjectMods Union(ObjectMods one, ObjectMods two) {
		return new ObjectMods {
			HideViaLayer = MergeSet(one.HideViaLayer, two.HideViaLayer),
			HideFromDetectors = MergeSet(one.HideFromDetectors, two.HideFromDetectors),
			HideCollider = MergeSet(one.HideCollider, two.HideCollider),
			HideSubColliders = MergeSet(one.HideSubColliders, two.HideSubColliders),

			ChangeLayer = MergeDict(one.ChangeLayer, two.ChangeLayer),
			ChangeAllLayers = MergeDict(one.ChangeAllLayers, two.ChangeAllLayers),
			VisualizeSprite = MergeDict(one.VisualizeSprite, two.VisualizeSprite),
		};

		static HashSet<T> MergeSet<T>(HashSet<T>? a, HashSet<T>? b)
			=> [.. (a ?? []).Union(b ?? [])];

		static Dictionary<K,V> MergeDict<K,V>(Dictionary<K,V>? a, Dictionary<K,V>? b) {
			Dictionary<K,V> res = [];
			foreach (var (k, v) in a ?? []) res.TryAdd(k, v);
			foreach (var (k, v) in b ?? []) res.TryAdd(k, v);
			return res;
		}
	}

}
