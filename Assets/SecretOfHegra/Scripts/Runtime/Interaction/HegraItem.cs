using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SecretOfHegra
{
    public enum ItemKind { Lamp, KohlJar, Coin, NameShard, Chisel, Treasure }

    /// <summary>
    /// A grabbable item with a home (a body socket or a spawn pose). Essential items are recovered
    /// automatically when they fall out of the world or are left far behind, and can be recalled from
    /// the inventory at any time.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class HegraItem : MonoBehaviour
    {
        public ItemKind kind;
        public bool essential = true;
        [Tooltip("Socket the item returns to (belt, satchel). Optional.")]
        public HegraSocket homeSocket;
        [Tooltip("Seconds an essential item may lie far from the player before it returns home.")]
        public float leftBehindSeconds = 6f;
        public float leftBehindDistance = 7f;
        public float killY = -4f;
        [Tooltip("If > 0: seconds the item may lie loose (not in a hand, not in a socket) before it returns home.")]
        public float looseReturnSeconds = 0f;

        /// <summary>Attached to the player's hand by <see cref="LampHandCarry"/>: never "lost", never auto-returned.</summary>
        [NonSerialized] public bool IsCarried;

        XRGrabInteractable m_Grab;
        Rigidbody m_Body;
        Pose m_SpawnPose;
        float m_FarTimer, m_LooseTimer;

        public XRGrabInteractable Grab => m_Grab ? m_Grab : (m_Grab = GetComponent<XRGrabInteractable>());
        public bool IsHeldByHand
        {
            get
            {
                if (!Grab.isSelected) return false;
                foreach (var i in Grab.interactorsSelecting)
                    if (!(i is XRSocketInteractor)) return true;
                return false;
            }
        }
        public bool IsInSocket
        {
            get
            {
                if (!Grab.isSelected) return false;
                foreach (var i in Grab.interactorsSelecting)
                    if (i is XRSocketInteractor) return true;
                return false;
            }
        }

        public event Action<HegraItem> Grabbed;
        public event Action<HegraItem> Released;
        public static event Action<HegraItem> AnyGrabbed;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Body = GetComponent<Rigidbody>();
            m_SpawnPose = new Pose(transform.position, transform.rotation);
        }

        void OnEnable()
        {
            Grab.selectEntered.AddListener(OnSelectEntered);
            Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            Grab.selectEntered.RemoveListener(OnSelectEntered);
            Grab.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) return;
            m_FarTimer = 0f;
            HegraAudio.PlayAt(Sfx.Grab, transform.position, 0.5f);
            Grabbed?.Invoke(this);
            AnyGrabbed?.Invoke(this);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) return;
            Released?.Invoke(this);
        }

        public void SetSpawnPose(Vector3 pos, Quaternion rot) => m_SpawnPose = new Pose(pos, rot);

        void Update()
        {
            if (IsCarried || !essential || Grab.isSelected) { m_FarTimer = 0f; m_LooseTimer = 0f; return; }

            if (transform.position.y < killY)
            {
                Recover();
                return;
            }

            // dropped and left lying (not held, not on the belt): bring it home quickly
            if (looseReturnSeconds > 0f)
            {
                m_LooseTimer += Time.deltaTime;
                if (m_LooseTimer > looseReturnSeconds) { Recover(); return; }
            }

            var gm = GameManager.Instance;
            if (gm == null || gm.Rig == null || homeSocket == null) return;
            float d = Vector3.Distance(transform.position, gm.Rig.Head.position);
            if (d > leftBehindDistance)
            {
                m_FarTimer += Time.deltaTime;
                if (m_FarTimer > leftBehindSeconds) Recover();
            }
            else m_FarTimer = 0f;
        }

        /// <summary>Returns the item to its home socket (or its spawn pose) without duplicating it.</summary>
        public void Recover()
        {
            m_FarTimer = 0f;
            m_LooseTimer = 0f;
            if (IsHeldByHand || IsCarried) return;
            if (m_Body)
            {
                if (!m_Body.isKinematic)
                {
                    m_Body.linearVelocity = Vector3.zero;
                    m_Body.angularVelocity = Vector3.zero;
                }
            }
            if (homeSocket && homeSocket.isActiveAndEnabled)
            {
                if (homeSocket.Holds(this)) return;
                transform.SetPositionAndRotation(homeSocket.attachTransform ? homeSocket.attachTransform.position : homeSocket.transform.position, homeSocket.transform.rotation);
                homeSocket.ForceInsert(this);
            }
            else
            {
                if (Grab.isSelected && Grab.interactionManager)
                    Grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)Grab);
                transform.SetPositionAndRotation(m_SpawnPose.position, m_SpawnPose.rotation);
            }
        }
    }
}
