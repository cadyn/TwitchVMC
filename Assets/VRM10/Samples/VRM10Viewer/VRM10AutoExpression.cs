﻿using System.Collections;
using UnityEngine;


namespace UniVRM10.VRM10Viewer
{
    /// <summary>
    /// 喜怒哀楽驚を循環させる
    /// </summary>
    public class VRM10AutoExpression : MonoBehaviour
    {
        [SerializeField]
        public VRM10Controller Controller;
        private void Reset()
        {
            Controller = GetComponent<VRM10Controller>();
        }

        Coroutine m_coroutine;

        [SerializeField]
        float m_wait = 0.5f;

        private void Awake()
        {
            if (Controller == null)
            {
                Controller = GetComponent<VRM10Controller>();
            }
        }

        IEnumerator RoutineNest(ExpressionPreset preset, float velocity, float wait)
        {
            for (var value = 0.0f; value <= 1.0f; value += velocity)
            {
                Controller.Vrm.Expression.SetWeight(ExpressionKey.CreateFromPreset(preset), value);
                yield return null;
            }
            Controller.Vrm.Expression.SetWeight(ExpressionKey.CreateFromPreset(preset), 1.0f);
            yield return new WaitForSeconds(wait);
            for (var value = 1.0f; value >= 0; value -= velocity)
            {
                Controller.Vrm.Expression.SetWeight(ExpressionKey.CreateFromPreset(preset), value);
                yield return null;
            }
            Controller.Vrm.Expression.SetWeight(ExpressionKey.CreateFromPreset(preset), 0);
            yield return new WaitForSeconds(wait * 2);
        }

        IEnumerator Routine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                var velocity = 0.1f;

                yield return RoutineNest(ExpressionPreset.happy, velocity, m_wait);
                yield return RoutineNest(ExpressionPreset.angry, velocity, m_wait);
                yield return RoutineNest(ExpressionPreset.sad, velocity, m_wait);
                yield return RoutineNest(ExpressionPreset.relaxed, velocity, m_wait);
                yield return RoutineNest(ExpressionPreset.surprised, velocity, m_wait);
            }
        }

        private void OnEnable()
        {
            m_coroutine = StartCoroutine(Routine());
        }

        private void OnDisable()
        {
            StopCoroutine(m_coroutine);
        }
    }
}
