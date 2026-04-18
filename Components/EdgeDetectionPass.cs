using GlobalEnums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EdgeDetection.Components;

/// <summary>
/// A customizable edge detection post-processing step for a <see cref="Camera"/>.
/// Should be targeted to specific layers.
/// </summary>
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraShakeManager))]
public class EdgeDetectionPass : MonoBehaviour {

	#region API

	/// <summary>
	/// Internal ID for the pass. Also used in localizing its menu options and naming its camera.
	/// The value must be unique.
	/// </summary>
	public string Id {
		get => field;
		set {
			if (field != value) {
				if (Passes.ContainsKey(value))
					throw new InvalidOperationException($"The {nameof(Id)} must be unique.");
				Passes.Remove(field);
				Passes.TryAdd(value, this);
				field = value;
			}
		}
	} = "";

	/// <summary>
	/// The colour that the outline will render as.
	/// The alpha channel is ignored; outlines are always at full opacity.
	/// </summary>
	public Color LineColor { get; set; } = Color.white;

	/// <summary>
	/// The width, in pixels, of the outline.
	/// If <see cref="HalfResolution"/> is true, lines will look double this size.
	/// </summary>
	public byte LineWidth {
		get => field;
		set => field = Math.Clamp(value, WIDTH_MIN, WIDTH_MAX);
	} = 1;

	/// <summary>
	/// The layers that will be outlined.
	/// DO NOT modify the array directly after setting it, if you want to change the
	/// contents you have to set an entirely new array here.
	/// </summary>
	public PhysLayers[] Layers {
		get => field;
		set {
			field = value;
			layerMask = LayerMask.GetMask(
				[.. field.Select(x => LayerMask.LayerToName((int)x))]
			);
		}
	} = [PhysLayers.PLAYER];
	int layerMask = LayerMask.GetMask(LayerMask.LayerToName((int)PhysLayers.PLAYER));

	/// <summary>
	/// If true, the outline will be rendered at half the resolution and then upscaled.
	/// This makes the outline look more pixelated, but improves performance.
	/// </summary>
	public bool HalfResolution { get; set; } = false;

	/// <summary>
	/// Minimum alpha value of a pixel for it to be included in the edge detection mask.
	/// </summary>
	public float AlphaThreshold {
		get => field;
		set => field = Mathf.Clamp01(value);
	} = 0.4f;

	/// <summary>
	/// How far behind Hornet the detector camera's clipping plane will reach.
	/// </summary>
	public float ClipFar {
		get => field;
		set => field = Mathf.Max(0, value);
	} = 1.8f;

	/// <summary>
	/// How far in front of Hornet the detector camera's clipping plane will reach.
	/// </summary>
	public float ClipNear {
		get => field;
		set => field = Mathf.Max(0, value);
	} = 1.8f;

	/// <summary>
	/// If set to the ID of a different pass, that other pass' silhouette will be
	/// cut out of the final edge detection for this pass.
	/// </summary>
	public string ExcludePass {
		get => field;
		set => field = value ?? "";
	} = "";

	#endregion

	/// <summary>
	/// Collection of all existing passes.
	/// </summary>
	internal static readonly Dictionary<string, EdgeDetectionPass> Passes = [];

	/// <summary>Minimum outline width.</summary>
	internal const byte WIDTH_MIN = 0;
	/// <summary>Maximum outline width.</summary>
	internal const byte WIDTH_MAX = 16;

	Material edgeMat;
	Camera mainCam, detectorCam;
	CameraShakeManager mainShaker, detectorShaker;

	static readonly int
		thresholdID = Shader.PropertyToID("_AlphaThreshold"),
		lineColorID = Shader.PropertyToID("_LineColor"),
		sceneTexID = Shader.PropertyToID("_SceneTex"),
		subtractTexID = Shader.PropertyToID("_SubtractTex");

	const int
		SILHOUETTE_PASS = 0,
		DETECT_PASS = 1,
		COMPOSITE_PASS = 2;

	void Start() {
		Passes.TryAdd(Id, this);
		mainCam = GetComponent<Camera>();

		GameObject camGo = new($"{Id} Edge Detection Camera");
		camGo.transform.SetParentReset(transform);

		detectorCam = camGo.AddComponentIfNotPresent<Camera>();
		detectorCam.enabled = false;

		mainShaker = GetComponent<CameraShakeManager>();
		detectorShaker = camGo.AddComponentIfNotPresent<CameraShakeManager>();
		mainShaker.CopyTo(camGo);

		camGo.AddComponent<EdgeDetector>().Settings = this;

		edgeMat = new Material(EdgeDetectionShader);
	}

	void OnDestroy() {
		DestroyImmediate(detectorCam.gameObject);
		DestroyImmediate(edgeMat);
		Passes.Remove(Id);
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (LineWidth < 1) {
			Graphics.Blit(source, destination);
			return;
		}

		int div = HalfResolution ? 2 : 1,
			width = source.width / div,
			height = source.height / div;

		RenderTexture[] temp = GetEmptyTemporaryTextures(count: 3, width, height);

		if (TryGetExcludePass(out var otherPass)) {
			otherPass.RenderSilhouette(temp[2]);
			edgeMat.SetTexture(subtractTexID, temp[2]);
		} else {
			edgeMat.SetTexture(subtractTexID, Texture2D.blackTexture);
		}
		edgeMat.SetColor(lineColorID, LineColor);
		edgeMat.SetTexture(sceneTexID, source);

		RenderSilhouette(temp[0]);

		for (int i = 0; i < LineWidth; i++) {
			Graphics.Blit(temp[0], temp[1], edgeMat, DETECT_PASS);
			(temp[0], temp[1]) = (temp[1], temp[0]);
		}

		Graphics.Blit(temp[0], destination, edgeMat, COMPOSITE_PASS);

		for (int i = 0; i < temp.Length; i++)
			RenderTexture.ReleaseTemporary(temp[i]);
	}

	internal void RenderSilhouette(RenderTexture output) {
		detectorCam.CopyFrom(mainCam);
		detectorCam.clearFlags = CameraClearFlags.SolidColor;
		detectorCam.backgroundColor = Color.clear;
		detectorCam.cullingMask = layerMask;

		// probably not necessary, but I do NOT want camera shakes to ever stop being detected.
		if (detectorShaker.cameraTypeReference != mainShaker.cameraTypeReference)
			mainShaker.CopyTo(detectorCam.gameObject);

		if (HeroController.instance) {
			float z = HeroController.instance.transform.position.z - detectorCam.transform.position.z;
			detectorCam.farClipPlane = z + ClipFar;
			detectorCam.nearClipPlane = z - ClipNear;
		} else {
			detectorCam.farClipPlane = 42;
			detectorCam.nearClipPlane = 38;
		}

		RenderTexture tx = GetEmptyTemporaryTextures(count: 1, output.width, output.height)[0];
		detectorCam.targetTexture = tx;
		detectorCam.Render();

		edgeMat.SetFloat(thresholdID, AlphaThreshold);
		Graphics.Blit(tx, output, edgeMat, SILHOUETTE_PASS);

		detectorCam.targetTexture = null;
		RenderTexture.ReleaseTemporary(tx);
	}

	bool TryGetExcludePass(out EdgeDetectionPass other) {
		other = null!;
		return
			ExcludePass != "" && ExcludePass != Id
			&& Passes.TryGetValue(ExcludePass, out other);
	}

	static RenderTexture[] GetEmptyTemporaryTextures(int count, int width, int height) {
		RenderTexture[] texs = new RenderTexture[count];
		RenderTexture prev = RenderTexture.active;
		for (int i = 0; i < count; i++) {
			texs[i] = RenderTexture.GetTemporary(width, height, 32, RenderTextureFormat.ARGB32);
			RenderTexture.active = texs[i];
			GL.Clear(true, true, Color.clear);
		}
		RenderTexture.active = prev;
		return texs;
	}

}
