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

	/// <summary>
	/// Static reference to this assembly.
	/// </summary>
	internal static readonly Assembly asm = Assembly.GetExecutingAssembly();

	/// <summary>
	/// Streams the embedded resource at <paramref name="path"/>,
	/// invokes <paramref name="action"/>, disposes of the stream.
	/// </summary>
	internal static void ReadResource(string path, Action<Stream> action) {
		using Stream stream = asm.GetManifestResourceStream(path);
		action.Invoke(stream);
	}

	/// <summary>
	/// Deserializes the embedded json file at <paramref name="path"/>
	/// to data of type <typeparamref name="T"/>.
	/// </summary>
	internal static T ReadJson<T>(string path) {
		T value;
		using (StreamReader reader = new(asm.GetManifestResourceStream(path))) {
			value = JsonConvert.DeserializeObject<T>(reader.ReadToEnd())!;
		}
		return value;
	}

	/// <summary>
	/// Enumerates all the descendants of a GameObject.
	/// </summary>
	internal static IEnumerable<Transform> Descendants(GameObject go) {
		foreach (Transform t in go.transform) {
			yield return t;
			foreach (Transform descendant in Descendants(t.gameObject))
				yield return descendant;
		}
	}

	/// <summary>
	/// Rotates a <see cref="Mesh"/>'s vertices about <see cref="Vector3.zero"/>.
	/// </summary>
	internal static void RotateVertices(this Mesh mesh, Quaternion rotation)
		=> mesh.RotateVertices(rotation, Vector3.zero);

	/// <summary>
	/// Rotates a <see cref="Mesh"/>'s vertices about <paramref name="center"/>.
	/// </summary>
	internal static void RotateVertices(this Mesh mesh, Quaternion rotation, Vector3 center) {
		mesh.vertices = [..
			mesh.vertices.Select(v => rotation * (v - center) + center)
		];
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

}

