using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EdgeDetection.Components;
using EdgeDetection.Menu;
using EdgeDetection.Structs;
using GlobalEnums;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using System.Collections.Generic;
using System.Linq;
using static EdgeDetection.Menu.MenuUtils;

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

	private static Harmony Harmony { get; } = new(Id);

	void Awake() {
		Plugin = this;
		Log = Logger;

		// Find shaders
		Utils.ReadAsset($"shaders.bundle", stream => {
			AssetBundle bundle = AssetBundle.LoadFromStream(stream);
			SilhouetteShader = bundle.LoadAsset<Shader>("assets/drawsilhouette.shader");
			EdgeDetectionShader = bundle.LoadAsset<Shader>("assets/edgedetection.shader");
		});

		// Find passes & bind config for them
		PassDefs = Utils.ReadJsonAsset<PassDef[]>($"pass_definitions.json");
		int i = 0;
		foreach(var pass in PassDefs) {
			PassDefs[i++] = pass with {
				Colour = Config.Bind(pass.Id, "Colour", pass.Colour).Value,
				Width = Config.Bind(
						pass.Id, "Width", pass.Width,
						new ConfigDescription("", new AcceptableValueRange<byte>(
							EdgeDetectionPass.WIDTH_MIN,
							EdgeDetectionPass.WIDTH_MAX
						))
					).Value,
				HalfRes = Config.Bind(pass.Id, "Low Res", pass.HalfRes).Value,
			};
		}

		Harmony.PatchAll();
		Log.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}

	internal bool GetPassConfig(string id, out ConfigEntry<Color> colour, out ConfigEntry<byte> width, out ConfigEntry<bool> halfRes) {
		bool flag = Config.TryGetEntry(id, "Colour", out colour);
		flag &= Config.TryGetEntry(id, "Width", out width);
		flag &= Config.TryGetEntry(id, "Low Res", out halfRes);
		if (!flag)
			Log.LogWarning($"Failed to load configuration for {id} pass.");
		return flag;
	}

	public LocalizedText ModMenuName() => Localized("MOD_TITLE");

	public AbstractMenuScreen BuildCustomMenu() {
		VerticalGroup group = new();
		group.AddRange(EdgeDetectionPass.Passes.SelectMany(GenerateDetectorOptions));
		return new ScrollMenuScreen(ModMenuName(), group);
	}

	/// <summary>
	/// Menu options for an <see cref="EdgeDetectionPass"/>.
	/// </summary>
	IEnumerable<MenuElement> GenerateDetectorOptions(EdgeDetectionPass pass) {
		SubtitleLabel title = new(Localized($"{pass.Id}_NAME"));

		HexColorInput colour = new(Localized("LINE_COLOUR_LABEL"));

		WiderSliderElement<byte> width = new(
			Localized("LINE_WIDTH_LABEL"),
			new ByteSliderModel(EdgeDetectionPass.WIDTH_MIN, EdgeDetectionPass.WIDTH_MAX)
		);

		ChoiceElement<bool> halfRes = new(
			Localized("HALF_RES_LABEL"),
			LocalizedBoolModel(),
			Localized("HALF_RES_DESC")
		);

		if (GetPassConfig(pass.Id, out var c, out var w, out var h)) {
			colour.SynchronizeWith(c);
			width.SynchronizeWith(w);
			halfRes.SynchronizeWith(h);
		}

		return [title, colour, width, halfRes];
	}

}
