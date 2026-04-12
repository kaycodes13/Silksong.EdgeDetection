using EdgeDetection.Components;

namespace EdgeDetection.Patches;

[HarmonyPatch(typeof(HeroController), "Awake")]
internal static class HideHeroLight {
	static void Postfix(HeroController __instance)
		=> __instance.transform.Find("HeroLight").gameObject
			.AddComponent<HideFromCamera>().hideFromEdgeDetectors = true;
}
