using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class ActionEditorManager : MonoBehaviour {

	private static ActionEditorManager sInstance = null;
	public static ActionEditorManager I {
		get {
			if (sInstance == null) {
				sInstance = FindObjectOfType <ActionEditorManager> ();
			}

			return sInstance;
		}
	}

	public Transform anchorChara;
}
