using BepInEx;
using BepInEx.Logging;
using EdgeDetection.Components;
using EdgeDetection.Menu;
using GlobalEnums;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using System.Collections.Generic;
using System.Linq;

namespace EdgeDetection;

// TODO: i18n (ask for localization help?)

[BepInAutoPlugin(id: "io.github.kaycodes13.edgedetection")]
[BepInDependency("org.silksong-modding.modmenu", "0.5.2")]
[BepInDependency("org.silksong-modding.i18n", "1.0.2")]
public partial class EdgeDetectionPlugin : BaseUnityPlugin, IModMenuCustomMenu {

	/// <summary>
	/// Instance of the plugin.
	/// </summary>
	internal static EdgeDetectionPlugin Plugin { get; private set; }

	/// <summary>
	/// Use to log debug info/errors/etc to the console.
	/// </summary>
	internal static ManualLogSource Log { get; private set; }

	/// <summary>
	/// Renders objects as a black/white mask.
	/// </summary>
	internal static Shader SilhouetteShader { get; private set; }

	/// <summary>
	/// Renders laplacian edge detection on black/white masks, then can be
	/// used to composite the edges only onto another texture.
	/// </summary>
	internal static Shader EdgeDetectionShader { get; private set; }

	/// <summary>
	/// Definitions for all edge detection passes which should be performed, in order.
	/// </summary>
	internal static PassDef[] PassDefs { get; private set; }

	/// <summary>
	/// Deserialization struct which describes an <see cref="EdgeDetectionPass"/>.
	/// </summary>
	internal record struct PassDef(
		string Id,
		Color Colour,
		byte Width,
		bool HalfRes,
		PhysLayers[] Layers,
		float Threshold);

	private static Harmony Harmony { get; } = new(Id);

	void Awake() {
		Plugin = this;
		Log = Logger;

		string path = $"{nameof(EdgeDetection)}.Assets";

		// Find shaders
		Utils.ReadResource($"{path}.shaders.bundle", stream => {
			AssetBundle bundle = AssetBundle.LoadFromStream(stream);
			SilhouetteShader = bundle.LoadAsset<Shader>("assets/drawsilhouette.shader");
			EdgeDetectionShader = bundle.LoadAsset<Shader>("assets/edgedetection.shader");
		});

		// Find passes & bind config for them
		PassDefs = Utils.ReadJson<PassDef[]>($"{path}.pass_definitions.json");
		int i = 0;
		foreach(var pass in PassDefs) {
			PassDefs[i++] = pass with {
				Colour = Config.Bind(pass.Id, "Colour", pass.Colour).Value,
				Width = Config.Bind(pass.Id, "Width", pass.Width).Value,
				HalfRes = Config.Bind(pass.Id, "Low Res", pass.HalfRes).Value,
			};
		}

		Harmony.PatchAll();
		Log.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}


	public LocalizedText ModMenuName() => MenuUtils.Localized("MOD_TITLE");

	public AbstractMenuScreen BuildCustomMenu() {
		VerticalGroup group = new();
		group.AddRange(EdgeDetectionPass.Passes.SelectMany(GenerateDetectorOptions));
		return new ScrollMenuScreen(ModMenuName(), group);
	}

	/// <summary>
	/// Menu options for an <see cref="EdgeDetectionPass"/>.
	/// </summary>
	IEnumerable<MenuElement> GenerateDetectorOptions(EdgeDetectionPass pass) {
		Config.TryGetEntry<Color>(pass.Id, "Colour", out var colourConfig);
		Config.TryGetEntry<byte>(pass.Id, "Width", out var widthConfig);
		Config.TryGetEntry<bool>(pass.Id, "Low Res", out var halfResConfig);

		SubtitleLabel title = new(MenuUtils.Localized($"{pass.Id}_NAME"));

		HexColorInput colour = new(MenuUtils.Localized("LINE_COLOUR_LABEL"));
		colour.Sync(colourConfig, x => pass.LineColor = x);

		SliderElement<byte> width = new(
			MenuUtils.Localized("LINE_WIDTH_LABEL"),
			new ByteSliderModel(0, 16)
		);
		width.Sync(widthConfig, x => pass.LineWidth = x);

		var halfRes = new ChoiceElement<bool>(
			MenuUtils.Localized("HALF_RES_LABEL"),
			MenuUtils.LocalizedBoolModel(),
			MenuUtils.Localized("HALF_RES_DESC")
		);
		halfRes.Sync(halfResConfig, x => pass.HalfResolution = x);

		return [title, colour, width, halfRes];
	}

}
