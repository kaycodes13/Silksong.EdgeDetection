namespace EdgeDetection.Components;

/// <summary>
/// Base class for components which create an invisible duplicate
/// of their GameObject for the purposes of edge detection.
/// </summary>
internal abstract class ObjectVisualizer : MonoBehaviour {

	static GameObject dupeParent;

	#region Unity Messages

	void Start() {
		if (IsVisualizer(gameObject)) {
			Destroy(this);
			return;
		}

		if (!dupeParent) {
			dupeParent = new GameObject("Edge Detection Visualizers");
			DontDestroyOnLoad(dupeParent);
			dupeParent.transform.Reset();
		}
		if (!dupe) {
			dupe = new();
			dupe.transform.SetParentReset(dupeParent.transform);
			InitDupe();
		}
	}

	void OnEnable() {
		Start();
		if (dupe && gameObject.activeInHierarchy)
			dupe.SetActive(true);
	}

	void Update() {
		if (!dupe) Start();
		if (!dupe) return;

		if (!gameObject.activeInHierarchy) {
			dupe.SetActive(false);
			return;
		}
		dupe.SetActive(true);
		dupe.transform.SetPositionAndRotation(transform.position, transform.rotation);
		dupe.transform.localScale = transform.lossyScale;

		UpdateDupe();
	}

	void OnDisable() {
		if (dupe)
			dupe.SetActive(false);
	}

	void OnDestroy() {
		DestroyDupe();
		Destroy(dupe);
	}

	#endregion

	#region Utils

	/// <inheritdoc cref="IsVisualizer(Transform)"/>
	public static bool IsVisualizer(GameObject go)
		=> IsVisualizer(go.transform);

	/// <summary>
	/// True if the given object is an ObjectVisualizer's secondary object,
	/// or is the container for those objects.
	/// </summary>
	public static bool IsVisualizer(Transform t)
		=> dupeParent && (t == dupeParent.transform || t.parent == dupeParent.transform);

	#endregion

	#region Inherited Interface

	/// <summary>
	/// Secondary GameObject which should be used to do the visualization.
	/// </summary>
	protected GameObject dupe { get; private set; }

	/// <summary>
	/// Any necessary initialization for the <see cref="dupe"/> object.
	/// The object is already created when this runs.
	/// </summary>
	protected abstract void InitDupe();

	/// <summary>
	/// A routine run during LateUpdate that syncs <see cref="dupe"/> to its
	/// primary GameObject.
	/// </summary>
	/// <remarks>
	/// By default, the dupe is de/activated when this component is, and when
	/// its primary GameObject is. It's position, rotation, and scale are also
	/// synced with the primary GameObject.
	/// </remarks>
	protected abstract void UpdateDupe();

	/// <summary>
	/// Clean up routine run when the component is destroyed. Anything the
	/// Visualizer created that could memory leak (meshes, materials, textures...)
	/// should be disposed of here.
	/// </summary>
	/// <remarks>
	/// The <see cref="dupe"/> object itself is destroyed after this runs.
	/// </remarks>
	protected abstract void DestroyDupe();

	#endregion
}
