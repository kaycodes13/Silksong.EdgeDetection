using GlobalEnums;
using System.Linq;

namespace EdgeDetection.Components;

/// <summary>
/// Visualizes sprites for edge detector cameras.
/// Use when a GameObject should be caught by a particular edge detection pass, but can't
/// have its layer changed due to it having a collider.
/// </summary>
internal class SpriteVisualizer : ObjectVisualizer {

	public PhysLayers layer = PhysLayers.DEFAULT;

	SpriteRenderer sprite, dupeSprite;
	tk2dSprite tk2d, dupeTk2d;
	MeshRenderer dupeMesh;
	Material dupeMat;

	protected override void InitDupe() {
		Dupe.name = $"{gameObject.name} Sprite";

		if (TryGetComponent(out sprite)) {
			dupeSprite = Dupe.AddComponent<SpriteRenderer>();
			dupeSprite.materials = [.. sprite.materials.Select(x => new Material(x.shader))];
		}
		else if (TryGetComponent(out tk2d)) {
			Dupe.AddComponent<MeshFilter>();
			dupeMesh = Dupe.AddComponent<MeshRenderer>();
			dupeTk2d = Dupe.AddComponent<tk2dSprite>();
		}

		if (sprite || tk2d)
			Dupe.AddComponent<HideFromCamera>().hideFromMain = true;
	}

	protected override void UpdateDupe() {
		Dupe.layer = (int)layer;

		if (sprite) {
			dupeSprite.sprite = sprite.sprite;
			dupeSprite.flipX = sprite.flipX;
			dupeSprite.flipY = sprite.flipY;
		}
		else if (tk2d && tk2d.CurrentSprite != dupeTk2d.CurrentSprite) {
			if (dupeMat)
				Destroy(dupeMat);
			dupeTk2d.scale = tk2d.scale;
			dupeTk2d.SetSprite(tk2d.collection, tk2d.spriteId);
			dupeMesh.material = dupeMat = new Material(dupeMesh.material);
		}
	}

	protected override void DestroyDupe() => Destroy(dupeMat);
}
