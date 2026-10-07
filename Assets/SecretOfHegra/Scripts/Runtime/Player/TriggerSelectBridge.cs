using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SecretOfHegra
{
    /// <summary>
    /// The Starter Assets map "select" to the grip button and the trigger to "activate", but the puzzles tell the
    /// player to point and pull the trigger. This makes a trigger press while a controller's ray hovers a puzzle
    /// target (any <see cref="XRSimpleInteractable"/>: water gates, eagle/stars, Elephant Rock glyphs, chest lid)
    /// act as a quick select + release, exactly like a grip tap. Grabbable items are untouched (they stay on grip),
    /// and a controller that is already holding something is skipped.
    /// </summary>
    public class TriggerSelectBridge : MonoBehaviour
    {
        readonly List<NearFarInteractor> m_Interactors = new List<NearFarInteractor>();

        void OnEnable()
        {
            m_Interactors.Clear();
            GetComponentsInChildren(true, m_Interactors);
        }

        void Update()
        {
            foreach (var nf in m_Interactors)
            {
                if (!nf || !nf.isActiveAndEnabled || nf.hasSelection) continue;
                if (!nf.activateInput.ReadWasPerformedThisFrame()) continue;
                var target = FirstPuzzleTarget(nf);
                if (target == null) continue;
                var mgr = nf.interactionManager;
                if (!mgr) continue;
                mgr.SelectEnter((IXRSelectInteractor)nf, (IXRSelectInteractable)target);
                mgr.SelectExit((IXRSelectInteractor)nf, (IXRSelectInteractable)target);
            }
        }

        static XRSimpleInteractable FirstPuzzleTarget(NearFarInteractor nf)
        {
            foreach (var h in nf.interactablesHovered)
                if (h is XRSimpleInteractable s && s.isActiveAndEnabled && !s.isSelected)
                    return s;
            return null;
        }
    }
}
