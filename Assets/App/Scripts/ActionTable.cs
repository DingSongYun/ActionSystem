#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ActionTable : ScriptableObject {


	[SerializeField]
	private List <ActionObject> m_ActionObjects = new List<ActionObject> ();

	public List <ActionObject> ActionObjects { get { return m_ActionObjects; } }

	#if UNITY_EDITOR
	public void OnEditorDraw (Character previewChara, float startY) {
		if (m_ActionObjects == null || m_ActionObjects.Count <= 0)
			return;

		foreach (var actObj in m_ActionObjects) {
			ActionObject.DrawResult result = actObj.OnEditorDraw (previewChara, startY);

			switch (result.state) {
			case ActionObject.DrawResult.State.Idel:
				break;
			case ActionObject.DrawResult.State.Delete:
				m_ActionObjects.Remove (actObj);
				break;
			case ActionObject.DrawResult.State.MoveUp:
				break;
			case ActionObject.DrawResult.State.MoveDown:
				break;
			default:
				break;
			}

			if (result.state != ActionObject.DrawResult.State.Idel)
				break;
		}
	}

	public void OnSave (string path) {
		if (string.IsNullOrEmpty (path))
			return;
		
		string assetPath = path.Substring (path.IndexOf ("Assets"));

		AssetDatabase.CreateAsset (this, assetPath);
	}
	#endif
}

[System.Serializable]
public class ActionObject {

	[SerializeField]
	private string m_Name = string.Empty;

	[SerializeField]
	private string m_AnmPath;
	private AnimationClip m_AnmClip;

	[SerializeField]
	private float m_ActionLenght;
	[SerializeField]
	private float m_PlaySpeed = 1f;
	[SerializeField]
	private float m_SelectTime = 0f;
	[SerializeField]
	private float m_SampleTime = 0f;
	[SerializeField]
	private List <ActionKeyFrame> m_KeyFrames = new List<ActionKeyFrame> ();

	private bool m_IsPlaying = false;
	private int m_SelectFrameID = 0;


	private Texture2D m_TimeLineBGTex = null;
	private const float TIMELINE_WIDTH = 505f;
	private const float TIMELINE_HEIGHT = 15f;

	[System.Serializable]
	public class Effect {
		public string path;
		public string parent;
		public bool enable = false;
		public Vector3 offset = Vector3.zero;
		public Vector3 rotate = Vector3.zero;
		public Vector3 scale = Vector3.zero;
		public bool offsetEnable = true;
		public bool rotateEnable = true;
		public bool scaleEnable = true;
	}

	[System.Serializable]
	public class ActionKeyFrame {
		public float time;
		public Effect effect = new Effect ();

		private GameObject m_Effect = null;

		#if UNITY_EDITOR
		public float OnEditorDraw (Character previewChara, float startY) {
			EditorGUILayout.BeginVertical ("Box");

			{ // Column 1
				EditorGUILayout.BeginHorizontal ();

				ActionObject.DrawFloatFiledWithName ("Time", time, GUILayout.Width (40f));

				EditorGUILayout.EndHorizontal ();
			}

			{ // Column 2
				EditorGUILayout.BeginHorizontal ();

				if (GUILayout.Button ("Effect", GUILayout.Width (40f))) {
					effect.path = EditorUtility.OpenFilePanel ("Effect...", Asset.Effect.ASSET_DIR, Asset.Effect.EXTENSION);
				}

				if (!string.IsNullOrEmpty (effect.path)) {
					m_Effect = (GameObject)GameObject.Instantiate (
						AssetDatabase.LoadAssetAtPath (effect.path.Substring (effect.path.IndexOf ("Assets")), typeof (GameObject)));
				}

				if (!string.IsNullOrEmpty (effect.path)) {
					// Draw Enable on\off
					effect.enable = GUILayout.Toggle (effect.enable, "On/Off");
				}

				EditorGUILayout.ObjectField (m_Effect, typeof (GameObject));


				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.EndVertical ();

			return startY;

		}
		#endif
	}

	#if UNITY_EDITOR
	public class DrawResult {
		public enum State {
			Idel,
			Delete,
			MoveUp,
			MoveDown,
			Max
		}

		public State state = State.Idel;
	}

	public DrawResult OnEditorDraw (Character previewChara, float startY) {
		DrawResult result = new DrawResult ();
		float yToDraw = startY;

		EditorGUILayout.BeginVertical ("box");

		{ // Column 1
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("-", GUILayout.Width (80f))) {
				result.state = DrawResult.State.Delete;
			}

			if (GUILayout.Button ("^", GUILayout.Width (80f))) {
				result.state = DrawResult.State.MoveUp;
			}

			if (GUILayout.Button ("V", GUILayout.Width (80f))) {
				result.state = DrawResult.State.MoveDown;
			}

			GUILayout.Space (40f);
			GUILayout.Label ("Action Name: ", GUILayout.Width (80f));
			m_Name = GUILayout.TextField (m_Name);

			if (GUILayout.Button ("Setup", GUILayout.Width (80f))) {
				SetupAction (previewChara);
			}

			EditorGUILayout.EndHorizontal ();

			yToDraw += 22f;
		}

		{ // Column 2
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Animation")) {
				m_AnmPath = EditorUtility.OpenFilePanel ("Selete Animation...", Asset.Animation.ASSET_DIR, Asset.Animation.EXTENSION);
			}

			if (!string.IsNullOrEmpty (m_AnmPath))
				LoadAnimation ();
			
			string m_AnmName = string.IsNullOrEmpty (m_AnmPath) ? "" : m_AnmPath.Substring (m_AnmPath.IndexOf ("Animation"));
			GUILayout.TextField (m_AnmName);
			m_AnmClip = (AnimationClip)EditorGUILayout.ObjectField (m_AnmClip, typeof(AnimationClip), false);

			if (m_AnmClip != null) {
				m_AnmPath = string.Format (Application.dataPath + "/App/ExternalResources/Animation/{0}.anim", m_AnmClip.name);
			}

			if (GUILayout.Button ("Time Apply")) {
				m_ActionLenght = m_AnmClip.length;
			}

			EditorGUILayout.EndHorizontal ();

			yToDraw += 22f;
		}

		bool isMouseOn = false;
		float selTime = 0f;
		{ // Column 3
			EditorGUILayout.BeginHorizontal ();
			// Draw Time Line
			{
				CreateTimeLineBG ();

				Rect timeLineRect = new Rect (5f, yToDraw, TIMELINE_WIDTH, TIMELINE_HEIGHT);

				UnityEngine.Event currentEvent = UnityEngine.Event.current;
				isMouseOn = timeLineRect.Contains (currentEvent.mousePosition);

				if (isMouseOn) {
					float offset_x = currentEvent.mousePosition.x - timeLineRect.xMin;
					InsertVerticalColorLine ((int)offset_x, Color.green);
					selTime = (offset_x / timeLineRect.width) * m_ActionLenght;

					if (currentEvent.type == EventType.MouseUp) {
						m_SelectTime = selTime;
					} else if (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) {
						m_SampleTime = selTime;

						if (!m_IsPlaying) {
							SampleAnim (previewChara, m_SampleTime);
						}
					}
				}

				if (m_SelectTime > 0) {
					InsertVerticalColorLine ((int)((m_SelectTime / m_ActionLenght) * TIMELINE_WIDTH), Color.blue);
				}

				for (int i = 0; i < m_KeyFrames.Count; i++) {
					ActionKeyFrame frame = m_KeyFrames [i];
					InsertVerticalColorLine ((int)((frame.time / m_ActionLenght) * TIMELINE_WIDTH), (i == m_SelectFrameID ? Color.red : Color.black));
				}

				GUI.DrawTexture (timeLineRect, m_TimeLineBGTex);

				if (m_TimeLineBGTex != null)
					m_TimeLineBGTex.Apply ();
			}

			GUILayout.Space (TIMELINE_WIDTH);

			if (isMouseOn) {
				m_SelectTime = DrawFloatFiledWithName ("Select: ", selTime, GUILayout.Width (50f));
			} else {
				m_SelectTime = DrawFloatFiledWithName ("Select: ", m_SelectTime, GUILayout.Width (50f));
			}
			m_ActionLenght = DrawFloatFiledWithName ("Length: ", m_ActionLenght, GUILayout.Width (50f));
			m_PlaySpeed = DrawFloatFiledWithName ("Speed: ", m_PlaySpeed, GUILayout.Width (50f));

			{
				if (m_IsPlaying) {
					if (GUILayout.Button ("Pause")) {
						m_IsPlaying = false;
					}
				} else {
					if (GUILayout.Button ("Play")) {
						m_IsPlaying = true;

						if (m_SelectTime > m_ActionLenght) {
							m_SelectTime = 0f;
						}

						m_SampleTime = m_SelectTime;
					}
				}
					
				if (m_IsPlaying) {
					if (EditorApplication.isPlaying) {
						m_SelectTime += Time.deltaTime * m_PlaySpeed;
					} else {
						m_SelectTime += (Time.deltaTime / 1.3f) * m_PlaySpeed;
					}

					m_SampleTime = m_SelectTime;

					if (m_SampleTime > m_ActionLenght)
						m_IsPlaying = false;
					
					SampleAnim (previewChara, m_SampleTime);
				}
			}

			EditorGUILayout.EndHorizontal ();
			yToDraw += 22f;
		}

		{ // Column 4
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Key Frame", GUILayout.Width (60f));

			if (GUILayout.Button ("+", GUILayout.Width (30f))) {
				ActionKeyFrame frame = new ActionKeyFrame ();

				frame.time = m_SelectTime;
				m_KeyFrames.Add (frame);
			}

			if (GUILayout.Button ("-", GUILayout.Width (30f))) {
				if (m_SelectFrameID >= 0 && m_KeyFrames.Count > m_SelectFrameID) {
					m_KeyFrames.RemoveAt (m_SelectFrameID);
				}
			}

			GUILayout.Space (10f);

			if (GUILayout.Button ("<", GUILayout.Width (30f))) {
				if (m_KeyFrames.Count > 0) {
					m_SelectFrameID--;
					m_SelectFrameID = Mathf.Max (0, m_SelectFrameID);

					m_SelectTime = m_KeyFrames [m_SelectFrameID].time;
					m_SampleTime = m_SelectTime;

					if (!m_IsPlaying)
						SampleAnim (previewChara, m_SampleTime);
				} else {
					m_SelectFrameID = -1;
				}
			}

			if (GUILayout.Button (">", GUILayout.Width (30f))) {
				if (m_KeyFrames.Count > 0) {
					m_SelectFrameID ++;
					m_SelectFrameID = Mathf.Min (m_KeyFrames.Count - 1, m_SelectFrameID);

					m_SelectTime = m_KeyFrames [m_SelectFrameID].time;
					m_SampleTime = m_SelectTime;

					if (!m_IsPlaying)
						SampleAnim (previewChara, m_SampleTime);
				} else {
					m_SelectFrameID = -1;
				}
			}

			GUILayout.Space (10f);

			m_SelectFrameID = DrawIntFiledWithName ("ID", m_SelectFrameID, GUILayout.Width (30f));
			m_SelectFrameID = Mathf.Min (m_SelectFrameID, m_KeyFrames.Count - 1);

			if (m_SelectFrameID >= 0) {
				ActionKeyFrame currFrame = m_KeyFrames [m_SelectFrameID];

				if (currFrame != null) {
					yToDraw += currFrame.OnEditorDraw (previewChara, yToDraw);
				}
			}

			EditorGUILayout.EndHorizontal ();
		}


		EditorGUILayout.EndVertical ();

		return result;
	}

	private void SetupAction (Character chara) {
		if (chara == null)
			return;

		if (m_AnmClip == null)
			return;

		Animation targetAnimation = chara.GetComponent <Animation> ();

		if (targetAnimation == null)
			return;

		targetAnimation.clip = m_AnmClip;
		targetAnimation.AddClip (m_AnmClip, m_AnmClip.name);
		AnimationState state = targetAnimation [m_AnmClip.name];
		state.time = 0f;
		state.weight = 1f;
		state.enabled = true;
		targetAnimation.Sample ();
		state.enabled = false;

		m_ActionLenght = m_AnmClip.length;

		EditorUtility.SetDirty (targetAnimation);
	}

	private void CreateTimeLineBG () {
		if (m_TimeLineBGTex == null) {
			m_TimeLineBGTex = new Texture2D ((int)TIMELINE_WIDTH, (int)TIMELINE_HEIGHT);
		}

		int i = 0, j = 0;
		for (i = 0; i < (int)TIMELINE_WIDTH; i++) {
			for (j = 0; j < (int)TIMELINE_HEIGHT; j++) {
				m_TimeLineBGTex.SetPixel (i, j, Color.white);
			}
		}
	}

	private void InsertVerticalColorLine (int x, Color col) {
		if (m_TimeLineBGTex != null)
		{
			for ( int i = 0 ; i < (int)TIMELINE_HEIGHT ; ++i )
			{
				m_TimeLineBGTex.SetPixel(x, i, col);
			}
		}
	}

	private void SampleAnim (Character previewChara, float time) {
		if (previewChara == null)
			return;

		if (m_AnmClip == null)
			return;

		previewChara.gameObject.SampleAnimation (m_AnmClip, time);

		SceneView.RepaintAll ();
	}

	private static float DrawFloatFiledWithName (string name, float value, params GUILayoutOption[] args) {
		GUILayout.Label (name, GUILayout.Width (40));
		return EditorGUILayout.FloatField (value, args);
	}

	private static int DrawIntFiledWithName (string name, int value, params GUILayoutOption[] args) {
		GUILayout.Label (name, GUILayout.Width (20));
		return EditorGUILayout.IntField (value, args);
	}

	private void LoadAnimation () {
		m_AnmClip = (AnimationClip)AssetDatabase.LoadAssetAtPath (m_AnmPath.Substring (m_AnmPath.IndexOf ("Assets")), typeof(AnimationClip));
	}
	#endif
}
