using BepInEx;
using BepInEx.Logging;
using System.IO;
using System.Reflection;
namespace EdgeDetection;

// TODO: custom menu + i18n (ask for localization help?)

[BepInAutoPlugin(id: "io.github.kaycodes13.edgedetection")]
public partial class EdgeDetectionPlugin : BaseUnityPlugin {

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

}
