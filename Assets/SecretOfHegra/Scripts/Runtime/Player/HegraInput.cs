using UnityEngine.InputSystem;

namespace SecretOfHegra
{
    /// <summary>
    /// Game-specific input actions (in addition to the XRI Starter Assets actions used for grab, UI,
    /// teleport and turning). Desktop keys avoid the XR Interaction Simulator's own bindings.
    /// </summary>
    public class HegraInput
    {
        public readonly InputAction menu;
        public readonly InputAction advance;
        public readonly InputAction hint;
        public readonly InputAction inventory;
        public readonly InputAction sprint;

        public HegraInput()
        {
            menu = new InputAction("Menu", InputActionType.Button);
            menu.AddBinding("<XRController>{LeftHand}/menuButton");
            menu.AddBinding("<XRController>{RightHand}/menuButton");
            menu.AddBinding("<Keyboard>/escape");

            advance = new InputAction("AdvanceDialogue", InputActionType.Button);
            advance.AddBinding("<XRController>{RightHand}/primaryButton");
            advance.AddBinding("<XRController>{LeftHand}/primaryButton");
            advance.AddBinding("<Keyboard>/enter");

            hint = new InputAction("Hint", InputActionType.Button);
            hint.AddBinding("<XRController>{LeftHand}/secondaryButton");
            hint.AddBinding("<Keyboard>/f1");

            inventory = new InputAction("Inventory", InputActionType.Button);
            inventory.AddBinding("<XRController>{RightHand}/secondaryButton");
            inventory.AddBinding("<Keyboard>/f2");

            // Click the walking thumbstick to sprint (XR Interaction Simulator: key 3 = primary 2D axis click).
            sprint = new InputAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<XRController>{LeftHand}/primary2DAxisClick");
            sprint.AddBinding("<XRController>{RightHand}/primary2DAxisClick");
            sprint.AddBinding("<Keyboard>/f3");
        }

        public void Enable()
        {
            menu.Enable(); advance.Enable(); hint.Enable(); inventory.Enable(); sprint.Enable();
        }

        public void Disable()
        {
            menu.Disable(); advance.Disable(); hint.Disable(); inventory.Disable(); sprint.Disable();
        }

        public void Dispose()
        {
            menu.Dispose(); advance.Dispose(); hint.Dispose(); inventory.Dispose(); sprint.Dispose();
        }

        /// <summary>Human-readable binding text for controller hints, e.g. "Menu [XR: menuButton | Esc]".</summary>
        public static string Describe(InputAction action)
        {
            var s = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
            return string.IsNullOrEmpty(s) ? action.name : s.Replace(" | ", " / ");
        }
    }
}
