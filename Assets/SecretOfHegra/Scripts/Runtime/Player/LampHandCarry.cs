using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra
{
    /// <summary>
    /// The clay lamp starts the journey in the player's dominant hand, so it can never be missed. While carried it
    /// is simply attached to the hand (not an XR grab: no button has to be held, and it never takes the hand's ray
    /// or near-grab away from other objects). Once hanging it on the belt is allowed (the belt lesson onwards),
    /// bringing the carrying hand to the waist hangs it there; from then on it is a normal grabbable item that
    /// returns to the belt when dropped.
    /// </summary>
    public class LampHandCarry : MonoBehaviour
    {
        public HegraItem lamp;
        public HegraSocket belt;
        public Transform leftHand, rightHand;
        public Vector3 holdOffset = new Vector3(0f, -0.035f, 0.075f);
        [Tooltip("How close (m) the carried lamp must come to the belt hook to hang itself there.")]
        public float beltReach = 0.25f;

        /// <summary>Set by the game: hanging on the belt is only possible from the belt lesson onwards.</summary>
        [NonSerialized] public bool allowBeltHang;

        public bool IsCarrying { get; private set; }
        public event Action StoredOnBelt;

        Collider[] m_Colliders;
        Rigidbody m_Body;
        Transform m_FreeParent;
        RigidbodyInterpolation m_Interpolation;
        bool m_Storing;

        void Awake()
        {
            if (!lamp) return;
            m_Colliders = lamp.GetComponentsInChildren<Collider>(true);
            m_Body = lamp.GetComponent<Rigidbody>();
            m_FreeParent = lamp.transform.parent;
            if (m_Body) m_Interpolation = m_Body.interpolation;
        }

        /// <summary>Puts the lamp in the given hand (leaving the belt or any grab).</summary>
        public void Carry(Handedness hand)
        {
            if (!lamp) return;
            var parent = hand == Handedness.Left ? leftHand : rightHand;
            if (!parent) return;
            StopAllCoroutines();
            m_Storing = false;
            var grab = lamp.Grab;
            if (grab.isSelected && grab.interactionManager)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            if (m_Body)
            {
                if (!m_Body.isKinematic) { m_Body.linearVelocity = Vector3.zero; m_Body.angularVelocity = Vector3.zero; }
                m_Body.isKinematic = true;
                // an interpolated body would keep pulling the transform back to its own pose: off while in hand
                m_Body.interpolation = RigidbodyInterpolation.None;
            }
            grab.enabled = false;
            if (m_Colliders != null) foreach (var c in m_Colliders) if (c) c.enabled = false;
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = holdOffset;
            lamp.transform.localRotation = Quaternion.identity;
            IsCarrying = true;
            lamp.IsCarried = true;
        }

        /// <summary>Moves the lamp from the hand onto the belt hook (one frame later, once it is grabbable again).</summary>
        public void StoreOnBelt()
        {
            if (!lamp || !belt || m_Storing) return;
            if (!IsCarrying) { belt.ForceInsert(lamp); return; }
            StartCoroutine(StoreRoutine());
        }

        IEnumerator StoreRoutine()
        {
            m_Storing = true;
            var pos = belt.attachTransform ? belt.attachTransform.position : belt.transform.position;
            lamp.transform.SetParent(m_FreeParent, true);
            lamp.transform.position = pos;
            if (m_Colliders != null) foreach (var c in m_Colliders) if (c) c.enabled = true;
            if (m_Body) { m_Body.interpolation = m_Interpolation; m_Body.isKinematic = false; }
            lamp.Grab.enabled = true;   // re-registers with the interaction manager
            IsCarrying = false;
            lamp.IsCarried = false;
            yield return null;
            m_Storing = false;
            if (belt.ForceInsert(lamp)) StoredOnBelt?.Invoke();
        }

        void LateUpdate()
        {
            // keep the lamp exactly in the palm while carried
            if (IsCarrying && lamp && !m_Storing && lamp.transform.parent)
            {
                lamp.transform.localPosition = holdOffset;
                lamp.transform.localRotation = Quaternion.identity;
            }
        }

        void Update()
        {
            if (!IsCarrying || !allowBeltHang || !belt || m_Storing) return;
            var hook = belt.attachTransform ? belt.attachTransform.position : belt.transform.position;
            if (Vector3.Distance(lamp.transform.position, hook) < beltReach) StoreOnBelt();
        }
    }
}
