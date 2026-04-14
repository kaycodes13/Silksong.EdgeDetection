namespace EdgeDetection.Components;

/// <summary>
/// Attempts to remove collider visualization from an object and then immediately explodes.
/// </summary>
internal class RemoveColliderVisualizer : MonoBehaviour {
	const int MAX_TRIES = 10;
	int tries = 0;

	void Awake() => TryRemove();
	void Start() => TryRemove();
	void Update() => TryRemove();

	void TryRemove() {
		tries++;
		if (TryGetComponent<ColliderVisualizer>(out var vis)) {
			Destroy(vis);
			Destroy(this);
		}
		if (tries > MAX_TRIES)
			Destroy(this);
	}
}
