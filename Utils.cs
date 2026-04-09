using System.Collections.Generic;

namespace EdgeDetection;

internal static class Utils {

	/// <summary>
	/// Enumerates all the descendants of a GameObject.
	/// </summary>
	public static IEnumerable<Transform> Descendants(GameObject go) {
		foreach (Transform t in go.transform) {
			yield return t;
			foreach (Transform descendant in Descendants(t.gameObject))
				yield return descendant;
		}
	}

}

