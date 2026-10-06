using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// One of the six stops. Receives step changes for its own steps, restores its world state from a save,
    /// and exposes its checkpoint spawn point and puzzle reset.
    /// </summary>
    public abstract class StopBase : MonoBehaviour
    {
        public StopId stopId;
        public Transform spawnPoint;
        public Transform guidePoint;

        protected GameManager Game => GameManager.Instance;
        protected GameState State => GameManager.Instance.State;

        /// <summary>Called once after the scene loads (before any step is entered).</summary>
        public virtual void Initialize() { }

        /// <summary>Bring the stop's world in line with the saved state (doors open, puzzles solved…).</summary>
        public virtual void RestoreState(GameState state) { }

        /// <summary>Called when a step owned by this stop becomes current.</summary>
        public abstract void OnStepEntered(QuestStep step);

        /// <summary>Called when the current step leaves this stop or the game reloads.</summary>
        public virtual void OnStepExited(QuestStep step) { }

        public virtual bool CanResetPuzzle => false;
        public virtual void ResetPuzzle() { }

        protected void Complete(QuestStep step) => Game.Complete(step);
        protected bool IsCurrent(QuestStep step) => Game.State.step == step;
    }
}
