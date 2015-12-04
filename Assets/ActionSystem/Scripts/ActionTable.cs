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

	private ActionObject m_CurrAction = null;

	public void UpdateAction (Character player, float time) {
		ActionObject action = null;
		//ActionObject oldAction = m_CurrAction;
		float timeForAction = GetCurrentAction (time, out action); 

		List <ActionObject.ActionKeyFrame> keyFrames = new List<ActionObject.ActionKeyFrame> ();

		if (m_CurrAction != action) {
			if (m_CurrAction != null) {
				m_CurrAction.UpdateActionTime (player, (timeForAction + m_CurrAction.ActionTime));
				keyFrames.AddRange (m_CurrAction.GetCurrKeyFrames ());
			}

			m_CurrAction = action;
		}

		if (m_CurrAction != null) {
			m_CurrAction.UpdateActionTime (player, timeForAction);
			keyFrames.AddRange (m_CurrAction.GetCurrKeyFrames ());
		} else {
			// Action End

			m_CurrAction = null;
			player.OnActFinish ();
		}

		foreach (ActionObject.ActionKeyFrame frame in keyFrames) {
			if (frame.effect.enable && !string.IsNullOrEmpty (frame.effect.path)) {
				PlayEffect (player, frame.effect);
			}
			if(frame.message != ""){
				player.SendMessage(frame.message, null, SendMessageOptions.RequireReceiver);
			}
		}
	}

	private void PlayEffect (Character player, ActionObject.Effect ef) {
		#if UNITY_EDITOR
		GameObject efObj = (GameObject) GameObject.Instantiate (
			AssetDatabase.LoadAssetAtPath (Asset.CheckAssetPath (ef.path), typeof (GameObject)));

		EffectController efController = efObj.AddComponent <EffectController> ();
		Transform parent = null;

		if (!string.IsNullOrEmpty (ef.parent)) {
			parent = Utils.SearchTransform (player.transform, ef.parent);
		}

		if (parent == null) 
			parent = player.transform;
		
		efController.Setup (parent, ef);
		efController.Play ();
		#endif
	}

	private float GetCurrentAction (float totalTime, out ActionObject currAction) {
		currAction = null;

		foreach (var act in m_ActionObjects) {
			if (act.ActionTime >= totalTime) {
				currAction = act;

				break;
			}

			totalTime -= act.ActionTime;
		}

		return totalTime;
	}

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

			startY = result.size;
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
	public string m_AnmPath;
	private AnimationClip m_AnmClip;
	public string AnmName {
		get {
			if (string.IsNullOrEmpty (m_AnmPath))
				return "";
			
			return m_AnmPath.Substring (m_AnmPath.IndexOf ("Animation"));
		}
	}

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

	public float ActionTime { get { return m_ActionLenght; } }
	public List <ActionKeyFrame> KeyFrames { get {return m_KeyFrames;} }

	private Texture2D m_TimeLineBGTex = null;
	private const float TIMELINE_WIDTH = 505f;
	private const float TIMELINE_HEIGHT = 15f;

	[System.Serializable]
	public class Effect {
		public string path;
		public string parent;
		public bool enable = true;
		public bool offsetEnable = true;
		public Vector3 offset = Vector3.zero;
		public bool rotateEnable = true;
		public Vector3 rotate = Vector3.zero;
		public bool scaleEnable = true;
		public Vector3 scale = Vector3.one;
	}

	[System.Serializable]
	public class ActionKeyFrame {
		public float time;
		public Effect effect = new Effect ();
		public string message = "";
		//private GameObject m_Effect = null;
		private GameObject m_EfParent = null;

		#if UNITY_EDITOR
		public float OnEditorDraw (Character previewChara) {
			float yToDraw = 0f;

			EditorGUILayout.BeginVertical ("Box");

			{ // Column 1
				EditorGUILayout.BeginHorizontal ();

				time = ActionObject.DrawFloatFiledWithName ("Time", time, GUILayout.Width (40f));

				EditorGUILayout.EndHorizontal ();
			}

			yToDraw += 25f;

			{ // Column 2
				EditorGUILayout.BeginHorizontal ();

				//string efPath = string.Empty;
				if (GUILayout.Button ("Effect", GUILayout.Width (40f))) {
					effect.path = EditorUtility.OpenFilePanel ("Effect...", Asset.Effect.ASSET_DIR, Asset.Effect.EXTENSION);
				}

				GUILayout.TextField (string.IsNullOrEmpty (effect.path) ? "" : effect.path.Substring (effect.path.IndexOf ("Effect")));

				if (!string.IsNullOrEmpty (effect.path)) {
					// Draw Enable on\off
					effect.enable = GUILayout.Toggle (effect.enable, "On/Off");
				} else {
					effect.enable = false;
				}

				EditorGUILayout.EndHorizontal ();
			}

			yToDraw += 22f;

			if (effect.enable) {
				
				{ // Column 3
					EditorGUILayout.BeginHorizontal ();

					// Draw Parent
					GUILayout.Label ("Parent", GUILayout.Width (50f));

					if (previewChara != null) {
						if (!string.IsNullOrEmpty (effect.parent)) {
							if (m_EfParent == null || !m_EfParent.name.Equals (effect.parent)) {
								m_EfParent = Utils.SearchTransform (previewChara.transform, effect.parent).gameObject;
							}
						} else {
							m_EfParent = null;
						}

						m_EfParent = (GameObject)EditorGUILayout.ObjectField (m_EfParent, typeof(GameObject));

						if (m_EfParent != null) {
							effect.parent = m_EfParent.name;
						} else {
							effect.parent = string.Empty;
						}
					} else {
						effect.parent = GUILayout.TextField (string.IsNullOrEmpty (effect.parent) ? "" : effect.parent);
					}

					{
						EditorGUILayout.BeginVertical ();

						{
							EditorGUILayout.BeginHorizontal ();
							effect.offsetEnable = GUILayout.Toggle (effect.offsetEnable, "Offset");
							effect.offset = EditorGUILayout.Vector3Field ("", effect.offset);
							EditorGUILayout.EndHorizontal ();
						}
						yToDraw += 34f;
						{
							EditorGUILayout.BeginHorizontal ();
							effect.rotateEnable = GUILayout.Toggle (effect.rotateEnable, "Rotate");
							effect.rotate = EditorGUILayout.Vector3Field ("", effect.rotate);
							EditorGUILayout.EndHorizontal ();
						}
						yToDraw += 34f;
						{
							EditorGUILayout.BeginHorizontal ();
							effect.scaleEnable = GUILayout.Toggle (effect.scaleEnable, "Scale");
							effect.scale = EditorGUILayout.Vector3Field ("", effect.scale);
							EditorGUILayout.EndHorizontal ();
						}
						yToDraw += 34f;
						EditorGUILayout.EndVertical ();
					}

					EditorGUILayout.EndHorizontal ();
				}

			}

			{ // Column 4 sendmessage
				EditorGUILayout.BeginHorizontal ();

				GUILayout.Label ("SendMessage",GUILayout.Width (80f));
				message = GUILayout.TextField ( message );
			
				EditorGUILayout.EndHorizontal ();
			}
			EditorGUILayout.EndVertical ();

			return yToDraw;

		}
		#endif
	}

	private float m_OldTime = -1f;
	private float m_CurrTime = -1f;
	public void UpdateActionTime (Character player, float time) {
		m_OldTime = m_CurrTime;
		m_CurrTime = time;	


		UpdateActionAnim (player);
		m_SelectTime = m_CurrTime;
	}

	private void UpdateActionAnim (Character player) {
		if (string.IsNullOrEmpty (AnmName))
			return;

		if (m_ActionLenght >= m_CurrTime) {
			player.animation.Stop ();
				
			player.PlayAnimation (AnmName);

			AnimationState anmState = player.animation [AnmName];
			if (anmState != null) {
				anmState.time = m_CurrTime;
			}
		}
	}

	public List <ActionKeyFrame> GetCurrKeyFrames () {
		List <ActionKeyFrame> result = new List<ActionKeyFrame> ();

		foreach (var keyFrame in m_KeyFrames) {
			Debug.Log ("KeyFram:" + keyFrame.time + "|" + m_OldTime + "|" + m_CurrTime);
			if (keyFrame.time > m_OldTime && keyFrame.time <= m_CurrTime) {
				result.Add (keyFrame);
				Debug.LogError ("Trigger KeyFrame");
			}

		}
		/*
		return m_KeyFrames.FindAll (delegate(ActionKeyFrame frame) {
			return frame.time > m_OldTime && frame.time <= m_CurrTime;
		});
		*/

		return result;
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
		public float size = 0f;
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
			string newAnmPath = "";
			if (GUILayout.Button ("Animation")) {
				newAnmPath = EditorUtility.OpenFilePanel ("Selete Animation...", Asset.Animation.ASSET_DIR, Asset.Animation.EXTENSION);
				newAnmPath = newAnmPath.Substring(newAnmPath.IndexOf("Assets"));
			}

			if (!string.IsNullOrEmpty (newAnmPath))
				LoadAnimation (newAnmPath);
			
			string m_AnmName = string.IsNullOrEmpty (m_AnmPath) ? "" : m_AnmPath.Substring (m_AnmPath.IndexOf ("Animation"));
			GUILayout.TextField (m_AnmName);
			m_AnmClip = (AnimationClip)EditorGUILayout.ObjectField (m_AnmClip, typeof(AnimationClip), false);

			if (m_AnmClip != null) {
				newAnmPath = string.Format ("Assets/ActionSystem/ExternalResources/Animation/{0}.anim", m_AnmClip.name);
			} else {
				newAnmPath = string.Empty;
			}

			if (!newAnmPath.Equals (m_AnmPath)) {
				if (m_AnmClip != null)
					m_ActionLenght = m_AnmClip.length;
				else
					m_ActionLenght = 0f;

				m_AnmPath = newAnmPath;
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
				CreateNewKeyFrame ();
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
					yToDraw += currFrame.OnEditorDraw (previewChara);
				}
			} else {
				yToDraw += 22f;
			}

			EditorGUILayout.EndHorizontal ();
		}


		EditorGUILayout.EndVertical ();

		result.size = yToDraw;

		return result;
	}

	private void CreateNewKeyFrame () {
		foreach (var frame in m_KeyFrames) {
			if (frame.time == m_SelectTime)
				return;
		}

		ActionKeyFrame newFrame = new ActionKeyFrame ();

		newFrame.time = m_SelectTime;
		m_KeyFrames.Add (newFrame);

		m_SelectFrameID = m_KeyFrames.Count - 1;
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
		targetAnimation.AddClip (m_AnmClip, AnmName);
		AnimationState state = targetAnimation [AnmName];
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

	public void LoadAnimation (string path) {
		m_AnmClip = (AnimationClip)AssetDatabase.LoadAssetAtPath (path.Substring (path.IndexOf ("Assets")), typeof(AnimationClip));
	}
	#endif
}
