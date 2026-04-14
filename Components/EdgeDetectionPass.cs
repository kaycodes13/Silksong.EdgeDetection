using GlobalEnums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EdgeDetection.Components;

/// <summary>
/// A customizable edge detection post-processing step for a <see cref="Camera"/>.
/// Should be targeted to specific layers.
/// </summary>
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraShakeManager))]
public class EdgeDetectionPass : MonoBehaviour, IComparable<EdgeDetectionPass> {

	#region API

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

	#endregion

	/// <summary>
	/// Internal ID for the pass.
	/// Used in localizing its menu options and naming its camera.
	/// </summary>
	public string Id { get; set; } = "";

	/// <summary>
	/// Collection of all existing passes.
	/// </summary>
	internal static readonly HashSet<EdgeDetectionPass> Passes = [];

	/// <summary>Minimum outline width.</summary>
	internal const byte WIDTH_MIN = 0;
	/// <summary>Maximum outline width.</summary>
	internal const byte WIDTH_MAX = 16;

	Material silhouetteMaterial, edgeDetectionMaterial;
	Camera mainCam, detectorCam;
	CameraShakeManager mainShaker, detectorShaker;
	RenderTexture camTarget;

	static readonly int
		thresholdID = Shader.PropertyToID("_AlphaThreshold"),
		lineColorID = Shader.PropertyToID("_LineColor"),
		sceneTexID = Shader.PropertyToID("_SceneTex"),
		finalID = Shader.PropertyToID("_FinalPass");

	void Start() {
		Passes.Add(this);
		mainCam = GetComponent<Camera>();

		GameObject camGo = new($"{Id} Edge Detection Camera");
		camGo.transform.SetParentReset(transform);

		detectorCam = camGo.AddComponentIfNotPresent<Camera>();
		detectorCam.enabled = false;

		mainShaker = GetComponent<CameraShakeManager>();
		detectorShaker = camGo.AddComponentIfNotPresent<CameraShakeManager>();
		mainShaker.CopyTo(camGo);

		camGo.AddComponent<EdgeDetector>().Settings = this;

		silhouetteMaterial = new Material(SilhouetteShader);
		edgeDetectionMaterial = new Material(EdgeDetectionShader);
	}

	void OnDisable() {
		if (camTarget && camTarget.IsCreated())
			camTarget.Release();
	}

	void OnDestroy() {
		DestroyImmediate(detectorCam.gameObject);
		DestroyImmediate(silhouetteMaterial);
		DestroyImmediate(edgeDetectionMaterial);
		if (camTarget) {
			if (camTarget.IsCreated())
				camTarget.Release();
			DestroyImmediate(camTarget);
		}
		Passes.Remove(this);
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination) {
		if (LineWidth < 1) {
			Graphics.Blit(source, destination);
			return;
		}

		int div = HalfResolution ? 2 : 1,
			width = source.width / div,
			height = source.height / div;

		// Set up temp cam, cull all layers except the ones we're rendering
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

		// Grab the scene
		if (camTarget && (camTarget.width != width || camTarget.height != height)) {
			detectorCam.targetTexture = null;
			camTarget.Release();
			DestroyImmediate(camTarget);
		}
		if (!camTarget) {
			camTarget = new(width, height, 32, RenderTextureFormat.ARGB32, 0);
			camTarget.Create();
		}
		detectorCam.targetTexture = camTarget;
		detectorCam.Render();
#if DEBUG
		if (DebugOutput) SaveToFile("0_orig", camTarget);
#endif

		RenderTexture[] temp = GetEmptyTemporaryTextures(count: 2, width, height);

		// Render silhouette mask
		silhouetteMaterial.SetFloat(thresholdID, AlphaThreshold);
		Graphics.Blit(camTarget, temp[0], silhouetteMaterial);
#if DEBUG
		if (DebugOutput) SaveToFile("1_mask", temp[0]);
#endif

		// Accumulate edge detection up to the width parameter
		edgeDetectionMaterial.SetInteger(finalID, 0);
		for (int i = 0; i < LineWidth; i++) {
			Graphics.Blit(temp[0], temp[1], edgeDetectionMaterial);
			(temp[0], temp[1]) = (temp[1], temp[0]);
#if DEBUG
			if (DebugOutput) SaveToFile($"2_edge_{i:00}", temp[0]);
#endif
		}

		// Composite the edge detection with the original camera view
		edgeDetectionMaterial.SetInteger(finalID, 1);
		edgeDetectionMaterial.SetColor(lineColorID, LineColor);
		edgeDetectionMaterial.SetTexture(sceneTexID, source);
		Graphics.Blit(temp[0], destination, edgeDetectionMaterial);
#if DEBUG
		if (DebugOutput) SaveToFile($"3_dest", destination);
#endif

		// Free memory
		for (int i = 0; i < temp.Length; i++)
			RenderTexture.ReleaseTemporary(temp[i]);
#if DEBUG
		DebugOutput = false;
#endif
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

	public int CompareTo(EdgeDetectionPass other)
		=> Id.CompareTo(other.Id);

	public override int GetHashCode()
		=> Id.GetHashCode();

#if DEBUG
	public bool DebugOutput { get; set; } = false;

	static void SaveToFile(string name, RenderTexture tex) {
		Texture2D tex2D = new(tex.width, tex.height, TextureFormat.RGBAFloat, false, true);

		var oldRt = RenderTexture.active;
		RenderTexture.active = tex;
		tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		tex2D.Apply();
		RenderTexture.active = oldRt;

		File.WriteAllBytes(
			$"{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name)}.png",
			tex2D.EncodeToPNG()
		);

		if (Application.isPlaying)
			Destroy(tex2D);
		else
			DestroyImmediate(tex2D);
	}
#endif
}
