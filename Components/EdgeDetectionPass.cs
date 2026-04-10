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
public class EdgeDetectionPass : MonoBehaviour {

	#region API

	internal static readonly List<EdgeDetectionPass> Passes = [];

	internal string Id { get; set; } = "";

	/// <summary>
	/// The colour that the outline will render as.
	/// The alpha channel is ignored; outlines are always at full opacity.
	/// </summary>
	public Color LineColor { get; set; } = Color.white;

	/// <summary>
	/// The width, in pixels, of the outline.
	/// If <see cref="HalfResolution"/> is true, lines will look double this size.
	/// </summary>
	public uint LineWidth {
		get => _width;
		set => _width = Math.Clamp(value, 1u, 16u);
	}
	uint _width = 1;

	/// <summary>
	/// The layers that will be outlined.
	/// DO NOT modify the array directly after setting it, if you want to change the
	/// contents you have to set an entirely new array here.
	/// </summary>
	public PhysLayers[] Layers {
		get => _layers;
		set {
			_layers = value;
			layerMask = LayerMask.GetMask(
				[.. value.Select(x => LayerMask.LayerToName((int)x))]
			);
		}
	}
	PhysLayers[] _layers = [PhysLayers.PLAYER];
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
		get => _thresh;
		set => _thresh = Mathf.Clamp01(value);
	}
	float _thresh = 0.4f;

	#endregion

	Material silhouetteMaterial, edgeDetectionMaterial;
	Camera mainCam, detectorCam;
	RenderTexture camTarget;

	static readonly int
		thresholdID = Shader.PropertyToID("_AlphaThreshold"),
		lineColorID = Shader.PropertyToID("_LineColor"),
		sceneTexID = Shader.PropertyToID("_SceneTex"),
		finalID = Shader.PropertyToID("_FinalPass");

	// TODO: test and adjust these as necessary
	public float clipFar = 40, clipNear = 35;

	void Start() {
		Passes.Add(this);
		mainCam = GetComponent<Camera>();

		detectorCam = new GameObject($"Edge Detection Camera").AddComponent<Camera>();
		DontDestroyOnLoad(detectorCam);
		detectorCam.enabled = false;
		detectorCam.gameObject.AddComponent<EdgeDetector>().Settings = this;

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
		detectorCam.farClipPlane = clipFar;
		detectorCam.nearClipPlane = clipNear;

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

		// Create temporary textures, make sure they're empty
		RenderTexture
			temp1 = RenderTexture.GetTemporary(width, height, 32, RenderTextureFormat.ARGB32),
			temp2 = RenderTexture.GetTemporary(width, height, 32, RenderTextureFormat.ARGB32);

		RenderTexture prev = RenderTexture.active;
		RenderTexture.active = temp1;
		GL.Clear(true, true, Color.clear);
		RenderTexture.active = temp2;
		GL.Clear(true, true, Color.clear);
		RenderTexture.active = prev;

		// Render silhouette mask
		silhouetteMaterial.SetFloat(thresholdID, AlphaThreshold);
		Graphics.Blit(camTarget, temp1, silhouetteMaterial);
#if DEBUG
		if (DebugOutput) SaveToFile("1_mask", temp1);
#endif

		// Accumulate edge detection up to the width parameter
		edgeDetectionMaterial.SetInteger(finalID, 0);
		for (int i = 0; i < LineWidth; i++) {
			Graphics.Blit(temp1, temp2, edgeDetectionMaterial);
			(temp1, temp2) = (temp2, temp1);
#if DEBUG
			if (DebugOutput) SaveToFile($"2_edge_{i:00}", temp1);
#endif
		}

		// Composite the edge detection with the original camera view
		edgeDetectionMaterial.SetInteger(finalID, 1);
		edgeDetectionMaterial.SetColor(lineColorID, LineColor);
		edgeDetectionMaterial.SetTexture(sceneTexID, source);
		Graphics.Blit(temp1, destination, edgeDetectionMaterial);
#if DEBUG
		if (DebugOutput) SaveToFile($"3_dest", destination);
#endif

		// Free memory
		RenderTexture.ReleaseTemporary(temp1);
		RenderTexture.ReleaseTemporary(temp2);
#if DEBUG
		DebugOutput = false;
#endif
	}


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
