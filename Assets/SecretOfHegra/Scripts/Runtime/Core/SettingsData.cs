using System;
using UnityEngine;

namespace SecretOfHegra
{
    public enum Language { Arabic = 0, English = 1 }
    public enum Handedness { Right = 0, Left = 1 }
    public enum Appearance { Male = 0, Female = 1 }
    public enum MoveSpeed { Slow = 0, Normal = 1, Fast = 2 }

    /// <summary>Player settings, persisted separately from progress so they survive "New Game".</summary>
    [Serializable]
    public class SettingsData
    {
        public Language language = Language.Arabic;
        public float subtitleScale = 1f;
        public bool seatedMode;
        public float interactionHeightOffset;
        public Handedness handedness = Handedness.Right;
        public bool smoothMovement = true;      // off-hand stick walks; the other hand keeps teleport + snap turn
        public MoveSpeed moveSpeed = MoveSpeed.Normal;
        public bool smoothTurn;                 // continuous turning instead of snap turning
        public float vignetteStrength = 0.5f;
        public float snapTurnAngle = 45f;
        public bool museumMode;
        public bool educationalCards = true;
        public Appearance appearance = Appearance.Female;
        public float musicVolume = 0.8f;
        public float ambienceVolume = 0.8f;
        public float sfxVolume = 0.9f;
        public float dialogueVolume = 1f;

        public const float SeatedHeightBoost = 0.45f;
        public const float SprintMultiplier = 1.7f;
        public const float SmoothTurnSpeed = 90f;

        /// <summary>Walking speed in m/s for the chosen preset (sprint multiplies this).</summary>
        public float WalkSpeed => moveSpeed == MoveSpeed.Slow ? 1.5f : moveSpeed == MoveSpeed.Fast ? 2.8f : 2.2f;
        public const float MinSubtitleScale = 0.7f;
        public const float MaxSubtitleScale = 1.6f;
        public const float MaxHeightOffset = 0.3f;

        public void Clamp()
        {
            subtitleScale = Mathf.Clamp(subtitleScale, MinSubtitleScale, MaxSubtitleScale);
            interactionHeightOffset = Mathf.Clamp(interactionHeightOffset, -MaxHeightOffset, MaxHeightOffset);
            vignetteStrength = Mathf.Clamp01(vignetteStrength);
            if (moveSpeed < MoveSpeed.Slow || moveSpeed > MoveSpeed.Fast) moveSpeed = MoveSpeed.Normal;
            if (snapTurnAngle != 30f && snapTurnAngle != 45f && snapTurnAngle != 60f && snapTurnAngle != 90f)
                snapTurnAngle = 45f;
            musicVolume = Mathf.Clamp01(musicVolume);
            ambienceVolume = Mathf.Clamp01(ambienceVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            dialogueVolume = Mathf.Clamp01(dialogueVolume);
        }

        /// <summary>Camera offset added to the XR Origin so seated players reach standing-height stations.</summary>
        public float CameraHeightOffset => (seatedMode ? SeatedHeightBoost : 0f) - interactionHeightOffset;

        public SettingsData Clone() => (SettingsData)MemberwiseClone();
    }
}
