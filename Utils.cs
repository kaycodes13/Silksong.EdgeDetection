using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EdgeDetection;

/// <summary>
/// Miscellaneous utility functions and extensions.
/// </summary>
internal static class Utils {
	#region Assets

	/// <summary>
	/// Static reference to this assembly.
	/// </summary>
	internal static readonly Assembly asm = Assembly.GetExecutingAssembly();

	/// <summary>
	/// Streams the embedded resource at <paramref name="path"/>,
	/// invokes <paramref name="action"/>, disposes of the stream.
	/// </summary>
	internal static void ReadAsset(string path, Action<Stream> action) {
		using Stream stream = asm.GetManifestResourceStream($"{nameof(EdgeDetection)}.Assets.{path}");
		action.Invoke(stream);
	}

	/// <summary>
	/// Deserializes the embedded json file at <paramref name="path"/>
	/// to data of type <typeparamref name="T"/>.
	/// </summary>
	internal static T ReadJsonAsset<T>(string path) {
		T value;
		using (StreamReader reader = new(asm.GetManifestResourceStream($"{nameof(EdgeDetection)}.Assets.{path}"))) {
			value = JsonConvert.DeserializeObject<T>(reader.ReadToEnd())!;
		}
		return value;
	}

	#endregion

	#region Iterators

	/// <summary>
	/// Enumerates the Transforms of all GameObjects in <paramref name="roots"/>
	/// and all their descendants.
	/// </summary>
	internal static IEnumerable<Transform> WalkHierarchy(IEnumerable<GameObject> roots) {
		foreach (Transform t in roots.SelectMany(x => SelfAndWalkHierarchy(x)))
			yield return t;
	}

	/// <summary>
	/// Enumerates the Transforms of a GameObject and all its descendants.
	/// </summary>
	internal static IEnumerable<Transform> SelfAndWalkHierarchy(GameObject go) {
		yield return go.transform;
		foreach (Transform descendant in WalkHierarchy(go))
			yield return descendant;
	}

	/// <summary>
	/// Enumerates the Transforms of all the descendants of a GameObject.
	/// </summary>
	internal static IEnumerable<Transform> WalkHierarchy(GameObject go) {
		foreach (Transform t in go.transform) {
			yield return t;
			foreach (Transform descendant in WalkHierarchy(t.gameObject))
				yield return descendant;
		}
	}

	#endregion

	#region Extensions

	extension(Mesh mesh) {
		/// <summary>
		/// Rotates a <see cref="Mesh"/>'s vertices about <see cref="Vector3.zero"/>.
		/// </summary>
		internal void RotateVertices(Quaternion rotation)
			=> mesh.RotateVertices(rotation, Vector3.zero);

		/// <summary>
		/// Rotates a <see cref="Mesh"/>'s vertices about <paramref name="center"/>.
		/// </summary>
		internal void RotateVertices(Quaternion rotation, Vector3 center) {
			mesh.vertices = [..
				mesh.vertices.Select(v => rotation * (v - center) + center)
			];
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
		}
	}

	#endregion
}

