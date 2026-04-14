using GlobalEnums;
using System.Linq;

namespace EdgeDetection.Components;

internal class ReLayerSprite : MonoBehaviour {

	public PhysLayers layer = PhysLayers.DEFAULT;

	static Transform? dupeParent;
	GameObject dupe;
	SpriteRenderer? sprite, dupeSprite;
	tk2dSprite? tk2d, dupeTk2d;
	MeshRenderer? dupeMesh;

	void Start() => CreateDupe();
	void OnEnable() => CreateDupe();

	void OnDisable() => Destroy(dupe);
	void OnDestroy() => Destroy(dupe);

	void LateUpdate() {
		if (!dupe)
			return;

		dupe.layer = (int)layer;
		dupe.transform.SetPositionAndRotation(transform.position, transform.rotation);
		dupe.transform.localScale = transform.lossyScale;

		if (sprite) {
			dupeSprite!.sprite = sprite.sprite;
			dupeSprite.flipX = sprite.flipX;
			dupeSprite.flipY = sprite.flipY;
		}
		else if (tk2d && dupeTk2d && tk2d.CurrentSprite != dupeTk2d.CurrentSprite) {
			Destroy(dupeMesh!.material);
			dupeTk2d.scale = tk2d.scale;
			dupeTk2d.SetSprite(tk2d.collection, tk2d.spriteId);
			dupeMesh!.material = new Material(dupeMesh.material);
		}
	}


	void CreateDupe() {
		if (dupe)
			return;

		if (!dupeParent) {
			dupeParent = new GameObject("Duped Sprites").transform;
			dupeParent.Reset();
		}

		dupe = new($"{gameObject.name} Sprite Dupe");
		dupe.transform.SetParentReset(dupeParent);

		if (TryGetComponent<SpriteRenderer>(out sprite)) {
			dupeSprite = dupe.AddComponent<SpriteRenderer>();
			dupeSprite.materials = [.. sprite.materials.Select(x => new Material(x.shader))];
		} else if (TryGetComponent<tk2dSprite>(out tk2d)) {
			dupe.AddComponent<MeshFilter>();
			dupeMesh = dupe.AddComponent<MeshRenderer>();
			dupeTk2d = dupe.AddComponent<tk2dSprite>();
		}

		if (sprite || tk2d) {
			dupe.AddComponent<HideFromCamera>().hideFromMain = true;
			foreach (Transform t in Utils.Descendants(gameObject))
				t.gameObject.AddComponent<RemoveColliderVisualizer>();
		}
	}

}
