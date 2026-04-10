using BepInEx;
using BepInEx.Logging;
using EdgeDetection.Components;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		VerticalGroup group = new() { VerticalSpacing = SpacingConstants.VSPACE_SMALL };

		foreach (var pass in EdgeDetectionPass.Passes)
			group.AddRange(GenerateDetectorOptions(pass));

		return new BasicMenuScreen(ModMenuName(), group);
	}

	IEnumerable<MenuElement> GenerateDetectorOptions(EdgeDetectionPass pass) {
		var colourConfig = Config.Bind(pass.Id, "Colour", pass.LineColor);
		var widthConfig = Config.Bind(pass.Id, "Width", (int)pass.LineWidth);
		var halfResConfig = Config.Bind(pass.Id, "Low Res", pass.HalfResolution);

		pass.LineColor = colourConfig.Value;
		pass.LineWidth = (uint)widthConfig.Value;
		pass.HalfResolution = halfResConfig.Value;

		var title = new TextLabel(Localized($"{pass.Id}_NAME"));
		title.Text.fontStyle = FontStyle.Italic;

		var colourModel = new ParserTextModel<Color>(HexColorParser, HexColorUnparser, new Color(-1, -1, -1));
		var colour = new TextInput<Color>(Localized("LINE_COLOUR_LABEL"), colourModel);
		colour.SynchronizeWith(colourConfig);
		colour.Model.OnValueChanged += x => pass.LineColor = x;
		colour.SetFontSizes(FontSizes.Small);

		var width = new SliderElement<int>(Localized("LINE_WIDTH_LABEL"), SliderModels.ForInts(1, 16));
		width.SynchronizeWith(widthConfig);
		width.Model.OnValueChanged += x => pass.LineWidth = (uint)x;
		width.SetFontSizes(FontSizes.Small);

		var halfRes = new ChoiceElement<bool>(
			Localized("HALF_RES_LABEL"),
			LocalizedBoolModel(),
			Localized("HALF_RES_DESC")
		);
		halfRes.SynchronizeWith(halfResConfig);
		halfRes.Model.OnValueChanged += x => pass.HalfResolution = x;
		halfRes.SetFontSizes(FontSizes.Small);

		return [title, colour, width, halfRes];
	}

	static ListChoiceModel<bool> LocalizedBoolModel()
		=> new([false, true]) {
			DisplayFn = (idx, val)
				=> val ? Localized("BOOL_TRUE") : Localized("BOOL_FALSE")
		};

	static bool HexColorParser(string x, out Color c) {
		c = new Color(-1, -1, -1);
		x = x.Replace("#", "").Trim();
		if (x.Length < 6)
			return false;
		if (
			byte.TryParse(x[0..2], NumberStyles.HexNumber, null, out var r)
			&& byte.TryParse(x[2..4], NumberStyles.HexNumber, null, out var g)
			&& byte.TryParse(x[4..6], NumberStyles.HexNumber, null, out var b)
		) {
			c = new Color32(r, g, b, 255);
			return true;
		}
		return false;
	}

	static bool HexColorUnparser(Color c, out string x) {
		if (c.r < 0 || c.r > 1 || c.g < 0 || c.g > 1 || c.b < 0 || c.b > 1)
			x = $"######";
		else {
			Color32 c32 = c;
			x = $"{c32.r:X2}{c32.g:X2}{c32.b:X2}";
		}
		return true;
	}
}
