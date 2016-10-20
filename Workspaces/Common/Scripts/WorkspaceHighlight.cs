﻿using System.Collections;
using UnityEngine.VR.Extensions;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.UI
{
	public class WorkspaceHighlight : MonoBehaviour
	{
		const string kMaterialHighlightColorTopProperty = "_ColorTop";
		const string kMaterialHighlightColorBottomProperty = "_ColorBottom";
		const string kMaterialHighlightAlphaProperty = "_Alpha";

		Coroutine m_HighlightCoroutine;
		Material m_TopHighlightMaterial;

		[SerializeField]
		MeshRenderer m_TopHighlightRenderer;

		public bool visible
		{
			get { return m_HighlightVisible; }
			set
			{
				if (m_HighlightVisible == value)
					return;

				m_HighlightVisible = value;

				if (m_HighlightCoroutine != null)
					this.StopCoroutine(ref m_HighlightCoroutine);

				if (m_HighlightVisible == true)
					m_HighlightCoroutine = StartCoroutine(ShowHighlight());
				else
					m_HighlightCoroutine = StartCoroutine(HideHighlight());
			}
		}
		bool m_HighlightVisible;

		void Awake()
		{
			m_TopHighlightMaterial = U.Material.GetMaterialClone(m_TopHighlightRenderer);
			m_TopHighlightMaterial.SetColor(kMaterialHighlightColorTopProperty, UnityBrandColorScheme.sessionGradient.a);
			m_TopHighlightMaterial.SetColor(kMaterialHighlightColorBottomProperty, UnityBrandColorScheme.sessionGradient.b);
			m_TopHighlightMaterial.SetFloat(kMaterialHighlightAlphaProperty, 0f); // hide the highlight initially
		}

		void OnDestory()
		{
			U.Object.Destroy(m_TopHighlightMaterial);
		}

		IEnumerator ShowHighlight()
		{
			const float kTargetAlpha = 1f;
			var currentAlpha = m_TopHighlightMaterial.GetFloat(kMaterialHighlightAlphaProperty);
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			const float kTargetDuration = 0.3f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentAlpha = U.Math.SmoothDamp(currentAlpha, kTargetAlpha, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_TopHighlightMaterial.SetFloat(kMaterialHighlightAlphaProperty, currentAlpha);
				yield return null;
			}

			m_TopHighlightMaterial.SetFloat(kMaterialHighlightAlphaProperty, kTargetAlpha); // set value after loop because precision matters in this case
			m_HighlightCoroutine = null;
		}

		IEnumerator HideHighlight()
		{
			const float kTargetAlpha = 0f;
			var currentAlpha = m_TopHighlightMaterial.GetFloat(kMaterialHighlightAlphaProperty);
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			const float kTargetDuration = 0.35f;
			while (currentDuration < kTargetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				currentAlpha = U.Math.SmoothDamp(currentAlpha, kTargetAlpha, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				m_TopHighlightMaterial.SetFloat(kMaterialHighlightAlphaProperty, currentAlpha);
				yield return null;
			}

			m_TopHighlightMaterial.SetFloat(kMaterialHighlightAlphaProperty, kTargetAlpha); // set value after loop because precision matters in this case
			m_HighlightCoroutine = null;
		}
	}
}