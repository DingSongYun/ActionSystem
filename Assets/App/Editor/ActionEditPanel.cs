using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ActionEditPanel : EditorWindow {

	private const string ACTION_EDITOR_SCENE = "Assets/App/Scenes/ActionEditor.unity";

	private string m_ActionPath = string.Empty;
	private string m_CharaPath = string.Empty;
	private string m_WeaponPath = string.Empty;

	private GameObject m_CharaSource;

	private GameObject m_WeaponSource;

	[MenuItem ("Tools/Action Edit Panel")]
	public static void OpenEditPanel () {
		EditorWindow.GetWindow <ActionEditPanel> (false, "Action Edit");
	}

	private void OnGUI () {
		{ // Column 1
			EditorGUILayout.BeginHorizontal ();
			// Load Sceme
			if (GUILayout.Button ("Load Scene")) {
				EditorApplication.OpenScene (ACTION_EDITOR_SCENE);
			}

			// Load Action
			if (GUILayout.Button ("Load Action")) {
				m_ActionPath = EditorUtility.OpenFilePanel ("Open Action...", Application.dataPath + "/App/ExternalResources/Action", "asset"); 
			}

			// Save Action
			if (GUILayout.Button ("Save Action")) {
			
			}

			// Clear Action
			if (GUILayout.Button ("Clear Action")) {
			}
			EditorGUILayout.EndHorizontal ();
		}

		{ // Column 2
			EditorGUILayout.BeginVertical ("Box");
			// Setup Scene
			GUILayout.Label ("Setup Scene");


			EditorGUILayout.BeginHorizontal ();
			// Set Character
			if (GUILayout.Button ("Load Chara", GUILayout.Width (120))) {
				m_CharaPath = EditorUtility.OpenFilePanel ("Load Character...", Application.dataPath + "/App/ExternalResources/Character/Prefabs", "prefab"); 
				CreateCharacter ();
			}
				
			GUILayout.TextField (string.IsNullOrEmpty (m_CharaPath) ? "" : m_CharaPath.Substring (m_CharaPath.IndexOf ("Character")));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			// Set Character
			if (GUILayout.Button ("Load Weapon", GUILayout.Width (120))) {
				m_WeaponPath = EditorUtility.OpenFilePanel ("Load Weapon...", Application.dataPath + "/App/ExternalResources/Weapon/Prefabs", "prefab"); 
				CreateWeapon ();
			}

			GUILayout.TextField (string.IsNullOrEmpty (m_WeaponPath) ? "" : m_WeaponPath.Substring (m_WeaponPath.IndexOf ("Weapon")));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			// Clean Scene
			if (GUILayout.Button ("Clean Scene", GUILayout.Width (120))) {
				CleanScene ();
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.EndVertical ();
		}
	}

	private void SetupScene () {
		


	}

	private void CreateCharacter () {
		CleanWeapon ();
		CleanCharacter ();

		m_CharaSource = AssetDatabase.LoadAssetAtPath (m_CharaPath.Substring (m_CharaPath.IndexOf ("Assets")), typeof(GameObject)) as GameObject;
		m_CharaSource = GameObject.Instantiate (m_CharaSource) as GameObject;

		Transform charaTran = m_CharaSource.transform;
		charaTran.parent = ActionEditorManager.I.anchorChara;
		charaTran.localPosition = Vector3.zero;
		charaTran.localScale = Vector3.one;
		charaTran.localRotation = Quaternion.identity;

	}

	private void CleanCharacter () {
		if (m_CharaSource != null)
			DestroyImmediate (m_CharaSource);

		m_CharaSource = null;
	}

	private void CreateWeapon () {
		if (m_CharaSource == null)
			return;

		CleanWeapon ();

		m_WeaponSource = AssetDatabase.LoadAssetAtPath (m_WeaponPath.Substring (m_WeaponPath.IndexOf ("Assets")), typeof(GameObject)) as GameObject;
		m_WeaponSource = GameObject.Instantiate (m_WeaponSource) as GameObject;

		Transform wepTran = m_WeaponSource.transform;
		wepTran.parent = SearchTransform (m_CharaSource.transform, "WepPos_R");
		wepTran.localPosition = Vector3.zero;
		wepTran.localScale = Vector3.one;
		wepTran.localRotation = Quaternion.identity;
	}

	private void CleanWeapon () {
		if (m_WeaponSource != null)
			DestroyImmediate (m_WeaponSource);

		m_WeaponSource = null;
	}

	private void CleanScene () {
		CleanWeapon ();
		CleanCharacter ();
	}

	public Transform SearchTransform( Transform fromTrans, string name )
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
