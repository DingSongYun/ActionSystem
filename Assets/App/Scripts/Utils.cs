using UnityEngine;
using System.Collections;

public class Utils : MonoBehaviour {

}

public class Asset {
	private static string PATH_ROOT = Application.dataPath + "/App/ExternalResources/";

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
}
