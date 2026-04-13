namespace EdgeDetection.Components;

/// <summary>
/// Marks a <see cref="Camera"/> as one used for edge detection and provides access to
/// the linked post-processing pass' settings.
/// </summary>
[RequireComponent(typeof(Camera))]
public class EdgeDetector : MonoBehaviour {
	public EdgeDetectionPass Settings { get; internal set; } = null!;
}
