using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra
{
    /// <summary>
    /// A rotatable stone gate with a T-handle on a vertical shaft. Grab the handle and turn it (the disc
    /// follows the hand around the vertical axis and snaps to 90° on release), or simply select it with a
    /// ray/trigger for a quarter turn — the accessible alternative. Reports its quarter-turn index.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class ChannelGate : MonoBehaviour
    {
        public Transform rotor;            // rotates around world up; carries the grooves and the handle
        public Renderer correctGlow;       // shown by the strongest hint when this gate is set correctly
        public int quarterTurns;

        public event Action<ChannelGate> Turned;

        XRSimpleInteractable m_Interactable;
        Transform m_Hand;
        float m_StartHandAngle, m_StartRotorAngle, m_GrabTime;
        bool m_Dragging;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            m_Interactable.selectEntered.AddListener(OnSelect);
            m_Interactable.selectExited.AddListener(OnRelease);
            ApplyRotation();
        }

        public void SetQuarterTurns(int q, bool notify)
        {
            quarterTurns = ((q % 4) + 4) % 4;
            ApplyRotation();
            if (notify) Turned?.Invoke(this);
        }

        void ApplyRotation()
        {
            if (rotor) rotor.localRotation = Quaternion.Euler(0f, quarterTurns * 90f, 0f);
        }

        float HandAngle()
        {
            var v = transform.InverseTransformPoint(m_Hand.position);
            return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        }

        void OnSelect(SelectEnterEventArgs args)
        {
            m_Hand = args.interactorObject.GetAttachTransform(m_Interactable);
            m_StartHandAngle = HandAngle();
            m_StartRotorAngle = quarterTurns * 90f;
            m_GrabTime = Time.time;
            m_Dragging = true;
        }

        void Update()
        {
            if (!m_Dragging || !m_Hand || !rotor) return;
            float delta = Mathf.DeltaAngle(m_StartHandAngle, HandAngle());
            rotor.localRotation = Quaternion.Euler(0f, m_StartRotorAngle + delta, 0f);
        }

        void OnRelease(SelectExitEventArgs args)
        {
            if (!m_Dragging) return;
            m_Dragging = false;
            float angle = rotor ? rotor.localEulerAngles.y : quarterTurns * 90f;
            int snapped = Mathf.RoundToInt(angle / 90f);
            bool tap = Time.time - m_GrabTime < 0.35f && Mathf.Abs(Mathf.DeltaAngle(m_StartRotorAngle, angle)) < 20f;
            if (tap) snapped = quarterTurns + 1; // ray click / quick trigger = one quarter turn clockwise
            HegraAudio.PlayAt(Sfx.WaterGate, transform.position, 0.8f);
            SetQuarterTurns(snapped, true);
        }

        /// <summary>Accessibility/automation: one quarter turn clockwise.</summary>
        public void Step() => SetQuarterTurns(quarterTurns + 1, true);
    }
}
