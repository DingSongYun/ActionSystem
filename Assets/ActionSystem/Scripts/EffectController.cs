using UnityEngine;
using System.Collections;

public class EffectController : MonoBehaviour {

	private ActionObject.Effect m_ActEf = null;

	private ParticleSystem[] m_Particles = null;
	private Animation[] m_Animations = null;

	private bool m_RequestPlay = false;
	private Transform m_Parent = null;
	private bool[] defaultEnableEmission;

	private void Awake () {
		m_Particles = transform.GetComponentsInChildren <ParticleSystem> ();

		defaultEnableEmission = new bool[m_Particles.Length];
		for (int i = 0; i < m_Particles.Length; i++) {
			defaultEnableEmission [i] = m_Particles [i].enableEmission;
		}

		m_Animations = transform.GetComponentsInChildren <Animation> ();

		gameObject.layer = LayerMask.NameToLayer ("Effect");
	}

	public void Setup (Transform parent, ActionObject.Effect ef) {
		m_ActEf = ef;
		m_Parent = parent;
		gameObject.SetActive (false);
	}

	public void Play () {
		gameObject.SetActive (true);
		m_RequestPlay = true;

		UpdatePosition ();
	}

	private void Update () {

		if (!m_RequestPlay)
			return;

		m_RequestPlay = false;

		if (m_Animations != null && m_Animations.Length > 0) {
			foreach (var anm in m_Animations) {
				if (anm == null)
					continue;
				
				anm.enabled = true;
				anm.Play ();
			}
		}

		if (m_Particles != null && m_Particles.Length > 0) {
			for (int i = 0; i < m_Particles.Length; i++) {
				ParticleSystem par = m_Particles [i];
				par.enableEmission = defaultEnableEmission [i];
				par.SetParticles (null, 0);
				par.Simulate (5f);
				par.Emit (0);
				par.time = 0f;
				par.Stop ();
				par.Clear ();
				par.Play ();
			}
		}

		UpdatePosition ();
	}

	private void LateUpdate () {
		transform.parent = ActionEditorManager.I.anchorEffect;
	}

	private void UpdatePosition () {

		if (m_Parent != null)
			transform.parent = m_Parent;

		if (m_ActEf.offsetEnable) {
			transform.localPosition = m_ActEf.offset;
		} else {
			transform.localPosition = Vector3.zero;
		}

		if (m_ActEf.scaleEnable) {
			transform.localScale = m_ActEf.scale;
		} else {
			transform.localScale = Vector3.one;
		}

		if (m_ActEf.rotateEnable) {
			transform.localRotation = Quaternion.Euler (m_ActEf.rotate);
		} else {
			transform.localRotation = Quaternion.Euler (Vector3.zero);
		}
	}
}
