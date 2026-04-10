using System.Collections.Generic;
using System.Linq;

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

	public static void RotateVertices(this Mesh mesh, Quaternion rotation)
		=> mesh.RotateVertices(rotation, Vector3.zero);

	public static void RotateVertices(this Mesh mesh, Quaternion rotation, Vector3 center) {
		mesh.vertices = [.. mesh.vertices.Select(v => rotation * (v - center) + center)];
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

}

