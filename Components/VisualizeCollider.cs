using GlobalEnums;
using System.Linq;

namespace EdgeDetection.Components;

internal class VisualizeCollider : MonoBehaviour {
	static Transform? visParent;
	GameObject visGo;
	Transform visT;

	Collider2D collider;
	Lever? lever;
	Vector3 origScale;

	void Start() {
		origScale = transform.lossyScale;
		lever = transform.GetComponent<Lever>();
		collider = transform.GetComponent<Collider2D>();
		if (
			!collider
			|| (collider.isTrigger && !VisualizableWhenTrigger(collider))
		) {
			return;
		}

		Mesh mesh = collider.CreateMesh(true, true, true);
		if (!mesh) return;

		mesh.vertices = [.. mesh.vertices.Select(v => v - transform.position)];
		mesh.RotateVertices(Quaternion.Inverse(transform.rotation));
		mesh.colors = [.. Enumerable.Repeat(Color.white, mesh.vertexCount)];

		if (!visParent) {
			visParent = new GameObject("Hitbox Visualizers").transform;
			visParent.Reset();
		}

		visGo = new GameObject($"{gameObject.name} Hitbox") { layer = gameObject.layer };
		visT = visGo.transform;
		visT.SetParent(visParent);
		visT.Reset();

		// If a collider damages, I don't care what layer it's actually on,
		// edge-detect it in the same style as everything else that's hazardous.
		if (transform.GetComponent<DamageEnemies>() || transform.GetComponent<DamageHero>())
			visT.gameObject.layer = (int)PhysLayers.ENEMIES;

		visGo.AddComponent<MeshFilter>().mesh = mesh;
		visGo.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
		visGo.AddComponent<HideFromCamera>().hideFromMain = true;
	}

	void Update() {
		if (!collider || !visGo)
			Start();
		else if (!collider.enabled || (lever && lever.hitBlocked))
			visGo.SetActive(false);
		else {
			visGo.SetActive(true);
			visT.SetPositionAndRotation(transform.position, transform.rotation);
			visT.localScale = Vector3.Scale(Vector3.one, transform.lossyScale.DivideElements(origScale));
		}
	}

	void OnDisable() {
		if (visGo)
			visGo.SetActive(false);
	}

	void OnDestroy() {
		if (visGo) {
			Destroy(visGo.GetComponent<MeshFilter>().mesh);
			Destroy(visGo);
		}
	}

	static readonly System.Type[] validTriggerTypes = [
		typeof(BouncePod), typeof(BounceBalloon),
		typeof(DamageHero), typeof(DamageEnemies)
	];
	static bool VisualizableWhenTrigger(Collider2D collider) =>
		validTriggerTypes.Any(x => collider.GetComponent(x));
}
