using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ActionEditPanel : EditorWindow {

	private const string ACTION_EDITOR_SCENE = "Assets/App/Scenes/ActionEditor.unity";

	private string m_ActionPath = string.Empty;
	private string m_ActionFileName = string.Empty;
	private string m_CharaPath = string.Empty;
	private string m_WeaponPath = string.Empty;

	private GameObject m_CharaSource;
	private Character m_Character;

	private GameObject m_WeaponSource;

	private ActionTable m_ActionTable;

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
				if (!string.IsNullOrEmpty (m_ActionPath)) {
					m_ActionFileName = System.IO.Path.GetFileNameWithoutExtension (m_ActionPath);
					m_ActionTable = (ActionTable)GameObject.Instantiate (
						AssetDatabase.LoadAssetAtPath (m_ActionPath.Substring (m_ActionPath.IndexOf ("Assets")), typeof(ActionTable)));
				}
			}

			// Save Action
			if (GUILayout.Button ("Save Action")) {
				m_ActionPath = EditorUtility.SaveFilePanel ("Save Action...", Application.dataPath + "/App/ExternalResources/Action", m_ActionFileName, "asset");

				if (m_ActionPath != null && m_ActionTable != null) {
					m_ActionTable.OnSave (m_ActionPath);

					m_ActionFileName = System.IO.Path.GetFileNameWithoutExtension (m_ActionFileName);
				}
			}

			// Clear Action
			if (GUILayout.Button ("Clear Action")) {
				m_ActionPath = string.Empty;
				m_ActionFileName = string.Empty;
				m_ActionTable = null;
			}
			EditorGUILayout.EndHorizontal ();

			if (m_ActionTable == null)
				m_ActionTable = new ActionTable ();
		}

		{ // Column 2
			EditorGUILayout.BeginVertical ("Box");
			// Setup Scene
			GUILayout.Label ("Setup Scene");


			EditorGUILayout.BeginHorizontal ();
			// Set Character
			if (GUILayout.Button ("Load Chara", GUILayout.Width (120))) {
				m_CharaPath = EditorUtility.OpenFilePanel ("Load Character...", Asset.Character.ASSET_DIR, Asset.Character.EXTENSION); 
				CreateCharacter ();
			}
				
			GUILayout.TextField (string.IsNullOrEmpty (m_CharaPath) ? "" : m_CharaPath.Substring (m_CharaPath.IndexOf ("Character")));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			// Set Character
			if (GUILayout.Button ("Load Weapon", GUILayout.Width (120))) {
				m_WeaponPath = EditorUtility.OpenFilePanel ("Load Weapon...", Asset.Weapon.ASSET_DIR, Asset.Weapon.EXTENSION); 
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
			
		{ // Column 3
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Play Action")) {
			}
				
			if (GUILayout.Button ("Action +")) {
				m_ActionTable.ActionObjects.Add (new ActionObject ());
			}
			EditorGUILayout.EndHorizontal ();
			
		}


		m_ActionTable.OnEditorDraw (m_Character, 22f * 6);

		Repaint();
	}

	private void SetupScene () {
		


	}

	private void CreateCharacter () {
		CleanWeapon ();
		CleanCharacter ();

		m_CharaSource = AssetDatabase.LoadAssetAtPath (m_CharaPath.Substring (m_CharaPath.IndexOf ("Assets")), typeof(GameObject)) as GameObject;
		m_CharaSource = GameObject.Instantiate (m_CharaSource) as GameObject;

		m_Character = m_CharaSource.AddComponent <Character> ();

		Transform charaTran = m_CharaSource.transform;
		charaTran.parent = ActionEditorManager.I.anchorChara;
		charaTran.localPosition = Vector3.zero;
		charaTran.localScale = Vector3.one;
		charaTran.localRotation = Quaternion.identity;

	}

	private void CleanCharacter () {
		if (m_Character != null)
			DestroyImmediate (m_Character.gameObject);

		m_Character = null;
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
