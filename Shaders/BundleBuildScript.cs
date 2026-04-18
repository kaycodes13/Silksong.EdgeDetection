using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class CreateAssetBundle {
	[MenuItem("Assets/Create Shader Bundle")]
    private static void BuildAllAssetBundles() {
		try {
			string directory = $"{Application.dataPath}/../AssetBundles";
			Directory.CreateDirectory(directory);
			
			AssetBundleBuild ab = new();
			ab.assetBundleName = "shader";
			ab.assetNames = Directory.EnumerateFiles("Assets/", "*.shader", SearchOption.AllDirectories).ToArray();
			
			BuildAssetBundlesParameters buildInput = new();
			buildInput.options = BuildAssetBundleOptions.AssetBundleStripUnityVersion;
			buildInput.bundleDefinitions = new AssetBundleBuild[]{ab};
			
			BuildTarget[] targets = new BuildTarget[] {
				BuildTarget.StandaloneWindows64, BuildTarget.StandaloneOSX, BuildTarget.StandaloneLinux64
			};
			
			foreach (var target in targets) {
				string path = Path.Combine(directory, target.ToString());
				Directory.CreateDirectory(path);
				buildInput.outputPath = path;
				BuildPipeline.BuildAssetBundles(buildInput);
			}
		}
		catch (Exception e) {
			Debug.LogError(e);
		}
	}
}
