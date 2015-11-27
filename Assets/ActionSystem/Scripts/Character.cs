using UnityEngine;
using System.Collections;

[RequireComponent(typeof (Animation))]
public class Character : MonoBehaviour {

	private ActionTable m_Action = null;
	private bool m_IsActing = false;
	private float m_ActingTime = 0f;
	public bool IsActing {
		get { return m_IsActing; }
	}

	public void PlayAction (ActionTable action) {
		m_Action = action;
		m_IsActing = true;
		m_ActingTime = 0f;
	}

	public void PlayAnimation (string name) {
		animation.CrossFade (name);
	}

	private void Update () {
		if (m_IsActing) {
			m_ActingTime += Time.deltaTime;
			m_Action.UpdateAction (this, m_ActingTime);
		}
	}

	public void OnActFinish () {
		m_IsActing = false;
		m_ActingTime = 0f;
	}
}
