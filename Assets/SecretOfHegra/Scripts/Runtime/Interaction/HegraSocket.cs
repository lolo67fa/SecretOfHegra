using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SecretOfHegra
{
    /// <summary>Socket that only accepts specific item kinds (belt hook, satchel, final-door niche…).</summary>
    public class HegraSocket : XRSocketInteractor
    {
        [SerializeField] ItemKind[] m_Accepts = { ItemKind.Lamp };

        /// <summary>Extra filter (e.g. the final door only accepts the player's own shard).</summary>
        public Func<HegraItem, bool> extraFilter;

        public event Action<HegraItem> ItemInserted;
        public event Action<HegraItem> ItemRemoved;

        public ItemKind[] Accepts { get => m_Accepts; set => m_Accepts = value; }

        public HegraItem HeldItem
        {
            get
            {
                if (!hasSelection) return null;
                foreach (var i in interactablesSelected)
                    if (i is Component c) return c.GetComponent<HegraItem>();
                return null;
            }
        }

        public bool Holds(HegraItem item) => item && HeldItem == item;

        bool Accept(IXRInteractable interactable)
        {
            if (!(interactable is Component c)) return false;
            var item = c.GetComponent<HegraItem>();
            if (!item) return false;
            if (Array.IndexOf(m_Accepts, item.kind) < 0) return false;
            return extraFilter == null || extraFilter(item);
        }

        public override bool CanHover(IXRHoverInteractable interactable) => base.CanHover(interactable) && Accept(interactable);
        public override bool CanSelect(IXRSelectInteractable interactable) => base.CanSelect(interactable) && Accept(interactable);

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            var item = (args.interactableObject as Component)?.GetComponent<HegraItem>();
            if (item) ItemInserted?.Invoke(item);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            var item = (args.interactableObject as Component)?.GetComponent<HegraItem>();
            if (item) ItemRemoved?.Invoke(item);
        }

        /// <summary>Puts an item into this socket programmatically (accessibility buttons, recovery, restore).</summary>
        public bool ForceInsert(HegraItem item)
        {
            if (!item || !interactionManager) return false;
            if (Holds(item)) return true;
            var grab = item.Grab;
            if (grab.isSelected)
                interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            if (hasSelection)
                return false;
            if (Array.IndexOf(m_Accepts, item.kind) < 0) return false;
            item.transform.SetPositionAndRotation(attachTransform ? attachTransform.position : transform.position,
                attachTransform ? attachTransform.rotation : transform.rotation);
            interactionManager.SelectEnter((IXRSelectInteractor)this, (IXRSelectInteractable)grab);
            return Holds(item);
        }
    }
}
