using BepInEx;
using BepInEx.Logging;
using EdgeDetection.Components;
using EdgeDetection.Menu;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TeamCherry.Localization;
namespace EdgeDetection;

// TODO: custom menu + i18n (ask for localization help?)

[BepInAutoPlugin(id: "io.github.kaycodes13.edgedetection")]
[BepInDependency("org.silksong-modding.modmenu", "0.5.2")]
[BepInDependency("org.silksong-modding.i18n", "1.0.2")]
public partial class EdgeDetectionPlugin : BaseUnityPlugin, IModMenuCustomMenu {

	internal static Assembly asm = Assembly.GetExecutingAssembly();
	internal static EdgeDetectionPlugin Plugin { get; private set; }
	internal static ManualLogSource Log { get; private set; }
	private static Harmony Harmony { get; } = new(Id);

	internal static Shader SilhouetteShader { get; private set; }
	internal static Shader EdgeDetectionShader { get; private set; }

	void Awake() {
		Plugin = this;
		Log = Logger;
		using (Stream stream = asm.GetManifestResourceStream($"{nameof(EdgeDetection)}.Assets.shaders.bundle")) {
			AssetBundle bundle = AssetBundle.LoadFromStream(stream);
			SilhouetteShader = bundle.LoadAsset<Shader>("assets/drawsilhouette.shader");
			EdgeDetectionShader = bundle.LoadAsset<Shader>("assets/edgedetection.shader");
		}
		Harmony.PatchAll();
		Log.LogInfo($"Plugin {Name} ({Id}) has loaded!");
	}

	internal static LocalisedString Localized(string key) => new($"Mods.{Id}", key);

	public LocalizedText ModMenuName() => Localized("MOD_TITLE");

	public AbstractMenuScreen BuildCustomMenu() {
		VerticalGroup group = new();
		group.AddRange(EdgeDetectionPass.Passes.SelectMany(GenerateDetectorOptions));
		return new ScrollMenuScreen(ModMenuName(), group);
	}

	IEnumerable<MenuElement> GenerateDetectorOptions(EdgeDetectionPass pass) {
		var colourConfig = Config.Bind(pass.Id, "Colour", pass.LineColor);
		var widthConfig = Config.Bind(pass.Id, "Width", (int)pass.LineWidth);
		var halfResConfig = Config.Bind(pass.Id, "Low Res", pass.HalfResolution);

		pass.LineColor = colourConfig.Value;
		pass.LineWidth = (uint)widthConfig.Value;
		pass.HalfResolution = halfResConfig.Value;

		var title = new TextLabel(Localized($"{pass.Id}_NAME"));
		title.SetFontSizes(FontSizes.Large);
		title.Text.fontStyle = FontStyle.Italic;

		var colour = new HexColorInput(Localized("LINE_COLOUR_LABEL"));
		colour.Sync(colourConfig, x => pass.LineColor = x);
		colour.Container.name += $" {pass.Id}";

		var width = new SliderElement<int>(Localized("LINE_WIDTH_LABEL"), SliderModels.ForInts(0, 16));
		width.Sync(widthConfig, x => pass.LineWidth = (uint)x);
		width.Container.name += $" {pass.Id}";

		var halfRes = new ChoiceElement<bool>(
			Localized("HALF_RES_LABEL"),
			MenuUtils.LocalizedBoolModel(),
			Localized("HALF_RES_DESC")
		);
		halfRes.Sync(halfResConfig, x => pass.HalfResolution = x);
		halfRes.Container.name += $" {pass.Id}";

		return [title, colour, width, halfRes];
	}

}
