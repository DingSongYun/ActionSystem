using UnityEngine;
using System.Collections;

public class Utils {
	public static Transform SearchTransform( Transform fromTrans, string name )
	{
		Transform[] transforms = fromTrans.GetComponentsInChildren <Transform> (true);

		Transform ret = null;

		if (transforms.Length <= 0)
			return ret;

		foreach (var trans in transforms) {
			if (trans.name.Equals (name)) {
				ret = trans;
				break;
			}
		}

		return ret;
	}
}

public class Asset {
	private static string PLUGIN_ROOT = "ActionSystem";
	private static string PATH_ROOT = Application.dataPath + "/ActionSystem/ExternalResources/";

	public class Character {
		public static string ASSET_DIR = Asset.PATH_ROOT + "Character/Prefabs";
		public static string EXTENSION = "prefab";
	}

	public class Weapon {
		public static string ASSET_DIR = Asset.PATH_ROOT + "Weapon/Prefabs";
		public static string EXTENSION = "prefab";
	}

	public class Action {
		public static string ASSET_DIR = Asset.PATH_ROOT + "Action";
		public static string EXTENSION = "asset";
	}

	public class Effect {
		public static string ASSET_DIR = Asset.PATH_ROOT + "Effect";
		public static string EXTENSION = "prefab";
	}

	public class Animation {
		public static string ASSET_DIR = Asset.PATH_ROOT + "Animation";
		public static string EXTENSION = "anim";
	}

	public static string CheckAssetPath (string path) {
		if (string.IsNullOrEmpty (path))
			return path;

		if (!path.StartsWith ("Assets")) {
			return path.Substring (path.IndexOf ("Assets"));
		}

		return path;
	}
}
