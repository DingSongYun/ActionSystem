using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ActionEditPanel : EditorWindow {

	private const string ACTION_EDITOR_SCENE = "Assets/ActionSystem/Scenes/ActionEditor.unity";

	private string m_ActionPath = string.Empty;
	private string m_ActionFileName = string.Empty;
	private string m_CharaPath = string.Empty;
	private string m_WeaponPath = string.Empty;

	private GameObject m_CharaSource;
	private Character m_Character;

	private GameObject m_WeaponSource;

	private ActionTable m_ActionTable;

	private bool m_PauseActing = false;

	private float m_SpeedScale = 1;

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

			EditorGUILayout.EndHorizontal ();
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

			// Init Scene
			if (GUILayout.Button ("Init Scene", GUILayout.Width (120))) {
				InitScene ();
			}

			// Clean Scene
			if (GUILayout.Button ("Clean Scene", GUILayout.Width (120))) {
				CleanScene ();
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.EndVertical ();
		}

		{ // Column 3
			EditorGUILayout.BeginVertical ("Box");
			// Set up Action
			GUILayout.Label ("Setup Action");

			{
				EditorGUILayout.BeginHorizontal ();
				// Load Action
				if (GUILayout.Button ("Load Action", GUILayout.Width (120))) {
					m_ActionPath = EditorUtility.OpenFilePanel ("Open Action...", Asset.Action.ASSET_DIR, "asset"); 
					if (!string.IsNullOrEmpty (m_ActionPath)) {
						m_ActionFileName = System.IO.Path.GetFileNameWithoutExtension (m_ActionPath);
						m_ActionTable = (ActionTable)GameObject.Instantiate (
							AssetDatabase.LoadAssetAtPath (m_ActionPath.Substring (m_ActionPath.IndexOf ("Assets")), typeof(ActionTable)));

						//initialize frame anim
						m_ActionTable.ActionObjects.ForEach((actionObj) => {
							actionObj.LoadAnimation(actionObj.m_AnmPath);
						});
					}
				}

				GUILayout.TextField (string.IsNullOrEmpty (m_ActionPath) ? "" : m_ActionPath.Substring (m_ActionPath.IndexOf ("Action")));
				EditorGUILayout.EndHorizontal ();
			}

			{
				EditorGUILayout.BeginHorizontal ();

				GUILayout.FlexibleSpace ();

				{ // Time Scale
					GUILayout.Label ("Action Speed:", GUILayout.Width (80f));
					m_SpeedScale = EditorGUILayout.FloatField (m_SpeedScale, GUILayout.Width (60f));
					GUILayout.Space (20f);
					if (!m_PauseActing)
						Time.timeScale = m_SpeedScale;
				}

				if (m_Character != null && m_Character.IsActing && !m_PauseActing) {
					if (GUILayout.Button ("Pause Action")) {
						if (m_Character.IsActing) {
							m_PauseActing = true;
							Time.timeScale = 0f;
						}
					}
				} else {
					if (GUILayout.Button ("Play Action", GUILayout.Width (120))) {
						if (!m_Character.IsActing) {
							for (int i = 0; i < ActionEditorManager.I.anchorEffect.childCount; i++) {
								Transform child = ActionEditorManager.I.anchorEffect.GetChild (i);

								if (child != null) {
									DestroyImmediate (child.gameObject);
								}
							}

							m_Character.PlayAction (m_ActionTable);
						} else if (m_PauseActing) {
							m_PauseActing = false;
							Time.timeScale = m_SpeedScale;
						}
					}
				}

				if (GUILayout.Button ("Action +", GUILayout.Width (120))) {
					m_ActionTable.ActionObjects.Add (new ActionObject ());
				}

				// Save Action
				if (GUILayout.Button ("Save Action", GUILayout.Width (120))) {
					m_ActionPath = EditorUtility.SaveFilePanel ("Save Action...", Asset.Action.ASSET_DIR, m_ActionFileName, "asset");

					if (m_ActionPath != null && m_ActionTable != null) {
						m_ActionTable.OnSave (m_ActionPath);

						m_ActionFileName = System.IO.Path.GetFileNameWithoutExtension (m_ActionFileName);
					}
				}

				// Clear Action
				if (GUILayout.Button ("Clear Action", GUILayout.Width (120))) {
					m_ActionPath = string.Empty;
					m_ActionFileName = string.Empty;
					m_ActionTable = null;
				}

				if (m_ActionTable == null)
					m_ActionTable = new ActionTable ();
				EditorGUILayout.EndHorizontal ();
			}
			
			EditorGUILayout.EndVertical ();

		}

		m_ActionTable.OnEditorDraw (m_Character, 22f * 8 + 5f);

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
		wepTran.parent = Utils.SearchTransform (m_CharaSource.transform, "WepPos_R");
		wepTran.localPosition = Vector3.zero;
		wepTran.localScale = Vector3.one;
		wepTran.localRotation = Quaternion.identity;
	}

	private void CleanWeapon () {
		if (m_WeaponSource != null)
			DestroyImmediate (m_WeaponSource);

		m_WeaponSource = null;
	}

	private void InitScene () {
		CleanScene ();

		CreateCharacter ();
		CreateWeapon ();
	}

	private void CleanScene () {
		CleanWeapon ();
		CleanCharacter ();

		for (int i = 0; i < ActionEditorManager.I.anchorChara.childCount; i++) {
			Transform child = ActionEditorManager.I.anchorChara.GetChild (i);

			if (child != null) {
				DestroyImmediate (child.gameObject);
			}
		}

		for (int i = 0; i < ActionEditorManager.I.anchorEffect.childCount; i++) {
			Transform child = ActionEditorManager.I.anchorEffect.GetChild (i);

			if (child != null) {
				DestroyImmediate (child.gameObject);
			}
		}
	}
}
