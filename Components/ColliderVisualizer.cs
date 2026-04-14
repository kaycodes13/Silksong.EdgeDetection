using GlobalEnums;
using System.Linq;

namespace EdgeDetection.Components;

/// <summary>
/// Visualizes colliders for edge detector cameras.
/// </summary>
internal class ColliderVisualizer : ObjectVisualizer {
	Collider2D collider;
	Lever? lever;
	Vector3 origScale;
	Mesh mesh;

	protected override void InitDupe() {
		dupe.name = $"{gameObject.name} Collider";

		origScale = transform.lossyScale;
		lever = transform.GetComponent<Lever>();
		collider = transform.GetComponent<Collider2D>();

		if (!collider || (collider.isTrigger && !IsTriggerVisualizable(collider)))
			return;

		Destroy(mesh);
		mesh = collider.CreateMesh(true, true, true);
		if (!mesh) return;

		mesh.vertices = [.. mesh.vertices.Select(v => v - transform.position)];
		mesh.RotateVertices(Quaternion.Inverse(transform.rotation));
		mesh.colors = [.. Enumerable.Repeat(Color.white, mesh.vertexCount)];

		if (transform.GetComponent<DamageHero>())
			dupe.layer = (int)PhysLayers.ENEMIES;
		else
			dupe.layer = gameObject.layer;

		dupe.AddComponent<MeshFilter>().mesh = mesh;
		dupe.AddComponent<MeshRenderer>().material = new Material(shader);
		dupe.AddComponent<HideFromCamera>().hideFromMain = true;
	}

	protected override void UpdateDupe() {
		if (!mesh)
			InitDupe();
		else if (!collider.enabled || (lever && lever.hitBlocked))
			dupe.SetActive(false);
		else {
			// the mesh matches origScale's size when its scaled to (1,1,1).
			// scales the mesh proportionally based on the difference between the
			// collider's original scale and its current scale.
			dupe.transform.localScale = Vector3.Scale(
				Vector3.one,
				transform.lossyScale.DivideElements(origScale)
			);
		}
	}

	protected override void DestroyDupe() => Destroy(mesh);


	static readonly Shader shader = Shader.Find("Sprites/Default");

	static readonly System.Type[] validTriggerTypes
		= [typeof(BouncePod), typeof(BounceBalloon), typeof(DamageHero)];

	static bool IsTriggerVisualizable(Collider2D collider)
		=> validTriggerTypes.Any(x => collider.GetComponent(x))
			|| (
				collider.TryGetComponent<PlayMakerFSM>(out var fsm)
				&& fsm.FsmName == "hornet_multi_wounder"
			);
}
