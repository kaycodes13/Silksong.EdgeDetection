using System.Collections.Generic;

namespace EdgeDetection.Components;

/// <summary>
/// Attach to an object to selectively hide it from specific <see cref="Camera"/>s.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class HideFromCamera : MonoBehaviour {
	public bool hideFromMain = false;
	public bool hideFromEdgeDetectors = false;
	public readonly HashSet<Camera> filteredCameras = [];

	Renderer rend;

	void Start() => rend = GetComponent<Renderer>();

	void OnDisable() => SetVisible(true);

	void OnWillRenderObject() {
		SetVisible(true);
		if (
			(hideFromMain && Camera.current == GameCameras.instance.mainCamera)
			|| (hideFromEdgeDetectors && Camera.current.GetComponent<EdgeDetector>())
			|| filteredCameras.Contains(Camera.current)
		) {
			SetVisible(false);
		}
	}

	void SetVisible(bool value) {
		if (!rend) Start();
		if (!rend) return;
		Color color = value ? Color.white : Color.clear;
		if (rend.sharedMaterial) rend.sharedMaterial.color = color;
		else if (rend.material) rend.material.color = color;
	}
}
