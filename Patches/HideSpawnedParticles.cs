using EdgeDetection.Components;

namespace EdgeDetection.Patches;

[HarmonyPatch(
	typeof(ObjectPool), "Spawn",
	[typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion), typeof(bool)]
)]
internal static class HideSpawnedParticles {
	static void Postfix(GameObject __result) {
		if (__result.GetComponent<ParticleSystemRenderer>())
			__result.AddComponentIfNotPresent<HideFromCamera>().hideFromEdgeDetectors = true;

		foreach (Transform t in Utils.Descendants(__result)) {
			if (t.GetComponent<ParticleSystemRenderer>())
				t.gameObject.AddComponentIfNotPresent<HideFromCamera>().hideFromEdgeDetectors = true;
		}
	}
}
