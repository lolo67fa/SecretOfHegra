using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SecretOfHegra
{
    /// <summary>
    /// Stop 6 — Qasr al-Farid and the finale. The player's own name shard goes into the niche in the rough,
    /// unfinished part of the facade; with all three signs (or on Kamkam's assisted route) the stone door
    /// opens onto the mason's secret chamber. The chest holds Aretas-IV-inspired coins, frankincense and a
    /// seal, and the smooth wall slowly reveals the player's name in Nabataean-inspired and modern letters.
    /// A souvenir picture of the wall can be saved from the ending panel.
    /// </summary>
    public class FaridStop : StopBase
    {
        public Transform facadeCenter;
        public float arriveDistance = 10f;

        [Header("Niche & door")]
        public HegraSocket niche;
        public Transform door;
        public Vector3 doorOpenOffset = new Vector3(0, -2.9f, 0);
        public float doorSeconds = 6f;
        public Light chamberLight;
        public float chamberLightIntensity = 2.2f;
        public Renderer[] chamberGlows;

        [Header("Chest")]
        public Transform chestLid;
        public Vector3 lidOpenEuler = new Vector3(105f, 0, 0);
        public XRSimpleInteractable lidHandle;
        public GameObject treasure;
        [Tooltip("Warm light over the gold: off while the lid is closed, rises as it opens.")]
        public Light treasureLight;
        public float treasureLightIntensity = 1.6f;

        [Header("Name wall")]
        public TextMeshPro wallNabataean;
        public TextMeshPro wallModern;
        public Renderer wallGlow;
        public Transform wallFocus;
        public float revealSeconds = 5f;

        [Header("Souvenir")]
        public Camera souvenirCamera;
        public int souvenirWidth = 1600, souvenirHeight = 1000;
        public Transform keyboardAnchor;

        Vector3 m_DoorClosed;
        Quaternion m_LidClosed;
        bool m_IntroStarted, m_MissingSaid, m_Opening;
        Coroutine m_Reveal;

        public string LastSouvenirPath { get; private set; }

        public override void Initialize()
        {
            if (door) m_DoorClosed = door.localPosition;
            if (chestLid) m_LidClosed = chestLid.localRotation;
            if (niche)
            {
                niche.extraFilter = item =>
                {
                    var shard = item ? item.GetComponent<NameShard>() : null;
                    bool own = shard && Game.State != null && Game.State.ShardBelongsToPlayer(shard.ShardId);
                    return own;
                };
                niche.ItemInserted += OnShardInserted;
                niche.ItemRemoved += OnShardRemoved;
            }
            if (lidHandle) lidHandle.selectEntered.AddListener(_ => OpenChest());
            if (souvenirCamera) souvenirCamera.enabled = false;
            if (Game && Game.Menus) Game.Menus.SaveSouvenirAction = SaveSouvenir;
            SetChamberLight(0f);
            SetWallReveal(0f);
        }

        public override void RestoreState(GameState s)
        {
            bool open = s.doorOpened || s.step > QuestStep.FaridDoorOpening;
            if (door) door.localPosition = open ? m_DoorClosed + doorOpenOffset : m_DoorClosed;
            SetChamberLight(open ? 1f : 0f);
            bool chest = s.chestOpened || s.step > QuestStep.FaridOpenChest;
            if (chestLid) chestLid.localRotation = chest ? m_LidClosed * Quaternion.Euler(lidOpenEuler) : m_LidClosed;
            if (treasure) treasure.SetActive(true);
            if (treasureLight) { treasureLight.intensity = chest ? treasureLightIntensity : 0f; treasureLight.enabled = chest; }
            WriteNameOnWall(s.playerName);
            SetWallReveal(s.step >= QuestStep.Complete ? 1f : 0f);
            // the shard stays in the niche once inserted
            if (s.shardInserted && niche && NameShard.Instance)
                StartCoroutine(InsertNextFrame(NameShard.Instance.Item));
        }

        IEnumerator InsertNextFrame(HegraItem item)
        {
            yield return null; // after the market has restored the shard to the satchel
            if (item && niche && !niche.Holds(item)) niche.ForceInsert(item);
        }

        public override void OnStepEntered(QuestStep step)
        {
            switch (step)
            {
                case QuestStep.FaridIntro:
                    ObjectiveBeacon.Current = facadeCenter;
                    m_IntroStarted = false;
                    break;

                case QuestStep.FaridInsertShard:
                    ObjectiveBeacon.Current = niche ? niche.transform : facadeCenter;
                    Game.Objective.SetActions(("ui.place_shard", PlaceShardFromSatchel));
                    if (Game.State.assistedRoute) StartCoroutine(AssistedArrival());
                    else if (Game.State.shardInserted || (NameShard.Instance && niche && niche.Holds(NameShard.Instance.Item)))
                        OnShardInserted(NameShard.Instance ? NameShard.Instance.Item : null); // placed during the intro
                    break;

                case QuestStep.FaridDoorOpening:
                    ObjectiveBeacon.Current = door;
                    if (!m_Opening) StartCoroutine(OpenDoor());
                    break;

                case QuestStep.FaridOpenChest:
                    ObjectiveBeacon.Current = lidHandle ? lidHandle.transform : chestLid;
                    Game.Objective.SetActions(("ui.open_chest", OpenChest));
                    break;

                case QuestStep.FaridFinale:
                    ObjectiveBeacon.Current = wallFocus;
                    WriteNameOnWall(Game.State.playerName);
                    if (m_Reveal != null) StopCoroutine(m_Reveal);
                    m_Reveal = StartCoroutine(Finale());
                    break;
            }
        }

        void Update()
        {
            if (Game == null || Game.State == null || !Game.InGame) return;
            if (IsCurrent(QuestStep.FaridIntro) && !m_IntroStarted && facadeCenter)
            {
                var d = Game.Rig.Head.position - facadeCenter.position; d.y = 0;
                if (d.magnitude < arriveDistance)
                {
                    m_IntroStarted = true;
                    Game.Dialogue.Play("farid_intro", () => Complete(QuestStep.FaridIntro));
                }
            }
        }

        // ───────── assisted route ─────────

        IEnumerator AssistedArrival()
        {
            yield return null;
            if (Game.State.shardEngraved)
            {
                Game.Dialogue.Play("assist_arrive");
                yield break;
            }
            // the door needs a name: Kamkam carves it once the player writes it
            bool done = false;
            Game.Dialogue.Play("assist_arrive", () => Game.Dialogue.Play("assist_name", () => done = true));
            while (!done) yield return null;
            var market = Game.GetStop(StopId.Market) as MarketStop;
            if (market) market.OpenKeyboardAt(keyboardAnchor ? keyboardAnchor : facadeCenter, OnAssistedName);
        }

        void OnAssistedName(string name)
        {
            Game.State.SetName(name);
            if (!Game.State.EngraveShard()) return;
            var market = Game.GetStop(StopId.Market) as MarketStop;
            if (market) market.HandOverShard();
            HegraAudio.PlayAt(Sfx.Carve, Game.Rig.Head.position, 0.7f);
            Game.Menus.ShowToast("toast.shard_received");
            WriteNameOnWall(Game.State.playerName);
            Game.NotifyStateChanged();
        }

        // ───────── niche ─────────

        void PlaceShardFromSatchel()
        {
            var shard = NameShard.Instance;
            if (!shard || !niche) return;
            if (!Game.State.ShardBelongsToPlayer(shard.ShardId)) { Game.Menus.ShowToast("toast.not_your_shard"); return; }
            niche.ForceInsert(shard.Item);
        }

        void OnShardInserted(HegraItem item)
        {
            if (!IsCurrent(QuestStep.FaridInsertShard)) return;
            if (!Game.State.shardInserted)
            {
                Game.State.shardInserted = true;
                HegraAudio.PlayAt(Sfx.StoneDoor, niche.transform.position, 0.4f);
            }
            if (Game.State.CanOpenFinalDoor())
            {
                Game.NotifyStateChanged(); // advances to FaridDoorOpening
                return;
            }
            Game.SaveNow();
            if (!m_MissingSaid)
            {
                m_MissingSaid = true;
                Game.Dialogue.Play("farid_missing");
            }
            Game.Objective.SetActions(("ui.place_shard", PlaceShardFromSatchel), ("menu.assist", Game.StartAssistedRouteFromMenu));
        }

        void OnShardRemoved(HegraItem item)
        {
            // taking the shard back out before the door moves undoes the insertion
            if (IsCurrent(QuestStep.FaridInsertShard) && Game.State.shardInserted)
            {
                Game.State.shardInserted = false;
                Game.SaveNow();
            }
        }

        // ───────── door & chamber ─────────

        IEnumerator OpenDoor()
        {
            m_Opening = true;
            Game.Dialogue.Play("farid_door");
            HegraAudio.PlayAt(Sfx.StoneDoor, door ? door.position : transform.position, 1f);
            for (float t = 0; t < doorSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / doorSeconds);
                if (door) door.localPosition = m_DoorClosed + doorOpenOffset * k;
                SetChamberLight(Mathf.Clamp01((t / doorSeconds - 0.25f) / 0.75f));
                yield return null;
            }
            if (door) door.localPosition = m_DoorClosed + doorOpenOffset;
            SetChamberLight(1f);
            m_Opening = false;
            Game.State.doorOpened = true;
            Complete(QuestStep.FaridDoorOpening);
        }

        void SetChamberLight(float k)
        {
            if (chamberLight)
            {
                chamberLight.enabled = k > 0.001f;
                chamberLight.intensity = chamberLightIntensity * k;
            }
            if (chamberGlows != null)
                foreach (var g in chamberGlows) if (g) g.enabled = k > 0.3f;
        }

        // ───────── chest ─────────

        void OpenChest()
        {
            if (!IsCurrent(QuestStep.FaridOpenChest) || Game.State.chestOpened) return;
            Game.State.chestOpened = true;
            StartCoroutine(LiftLid());
            Game.Dialogue.Play("farid_chest");
            Game.NotifyStateChanged(); // advances to FaridFinale
        }

        IEnumerator LiftLid()
        {
            HegraAudio.PlayAt(Sfx.Chest, chestLid ? chestLid.position : transform.position, 1f);
            var open = m_LidClosed * Quaternion.Euler(lidOpenEuler);
            if (treasureLight) treasureLight.enabled = true;
            for (float t = 0; t < 1.4f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 1.4f);
                if (chestLid) chestLid.localRotation = Quaternion.Slerp(m_LidClosed, open, k);
                if (treasureLight) treasureLight.intensity = k * treasureLightIntensity;
                yield return null;
            }
            if (chestLid) chestLid.localRotation = open;
            if (treasureLight) treasureLight.intensity = treasureLightIntensity;
        }

        // ───────── finale: the name appears on the wall ─────────

        void WriteNameOnWall(string playerName)
        {
            var gm = Game;
            if (!gm) return;
            if (wallNabataean)
            {
                if (gm.NabataeanFont) wallNabataean.font = gm.NabataeanFont;
                wallNabataean.isRightToLeftText = true;
                wallNabataean.text = string.IsNullOrEmpty(playerName) ? "" : gm.Nabataean.Convert(playerName).displayText;
            }
            if (wallModern) Loc.Apply(wallModern, playerName ?? "", TextAlignmentOptions.Center);
        }

        void SetWallReveal(float k)
        {
            k = Mathf.Clamp01(k);
            if (wallNabataean) wallNabataean.alpha = k;
            if (wallModern) wallModern.alpha = Mathf.Clamp01((k - 0.5f) * 2f);
            if (wallGlow) wallGlow.enabled = k > 0.05f;
        }

        IEnumerator Finale()
        {
            // wait until the chest lines have finished, then let the letters surface one breath at a time
            while (Game.Dialogue.IsPlaying) yield return null;
            HegraAudio.PlayAt(Sfx.Carve, wallFocus ? wallFocus.position : transform.position, 0.8f);
            bool finished = false;
            Game.Dialogue.Play(Game.State.assistedRoute ? "farid_finale_short" : "farid_finale", () => finished = true);
            for (float t = 0; t < revealSeconds; t += Time.deltaTime)
            {
                SetWallReveal(t / revealSeconds);
                yield return null;
            }
            SetWallReveal(1f);
            while (!finished) yield return null;
            m_Reveal = null;
            Complete(QuestStep.FaridFinale); // → Complete: the ending panel offers the souvenir
        }

        // ───────── souvenir ─────────

        /// <summary>Folder (under Pictures) where a copy appears in the headset's Files/gallery apps.</summary>
        public const string GalleryFolder = "Secret of Hegra";

        /// <summary>True when the last souvenir was also copied to the device's public Pictures folder.</summary>
        public bool LastSouvenirInGallery { get; private set; }

        /// <summary>
        /// Renders the name wall to a PNG under persistentDataPath/Souvenirs and returns the path. On Android (Quest)
        /// a copy also goes to Pictures/Secret of Hegra through MediaStore, so players can find and share it.
        /// </summary>
        public string RenderSouvenir()
        {
            if (!souvenirCamera) return null;
            var rt = RenderTexture.GetTemporary(souvenirWidth, souvenirHeight, 24, RenderTextureFormat.ARGB32);
            var prevTarget = souvenirCamera.targetTexture;
            var prevActive = RenderTexture.active;
            souvenirCamera.targetTexture = rt;
            souvenirCamera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(souvenirWidth, souvenirHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, souvenirWidth, souvenirHeight), 0, 0);
            tex.Apply();
            souvenirCamera.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            var dir = Path.Combine(Application.persistentDataPath, "Souvenirs");
            Directory.CreateDirectory(dir);
            var safeName = string.IsNullOrEmpty(Game.State.playerName) ? "traveler" : Game.State.playerName;
            foreach (var c in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(c, '_');
            var path = Path.Combine(dir, $"Hegra_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            var png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            Destroy(tex);
            LastSouvenirInGallery = SaveToDeviceGallery(png, Path.GetFileName(path));
            return path;
        }

        /// <summary>
        /// Android 10+: inserts the PNG into MediaStore under Pictures/Secret of Hegra (no storage permission is
        /// needed for the app's own images). Elsewhere (Editor, desktop) it does nothing. Never throws.
        /// </summary>
        static bool SaveToDeviceGallery(byte[] png, string fileName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var values = new AndroidJavaObject("android.content.ContentValues"))
                using (var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
                {
                    values.Call("put", "_display_name", fileName);
                    values.Call("put", "mime_type", "image/png");
                    values.Call("put", "relative_path", "Pictures/" + GalleryFolder);
                    using (var collection = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
                    using (var uri = resolver.Call<AndroidJavaObject>("insert", collection, values))
                    {
                        if (uri == null) return false;
                        using (var stream = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                        {
                            if (stream == null) return false;
                            stream.Call("write", (sbyte[])(Array)png);
                            stream.Call("flush");
                            stream.Call("close");
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hegra] Could not copy the souvenir to the gallery: " + e.Message);
                return false;
            }
#else
            return false;
#endif
        }

        void SaveSouvenir()
        {
            try
            {
                LastSouvenirPath = RenderSouvenir();
                if (string.IsNullOrEmpty(LastSouvenirPath)) throw new Exception("no souvenir camera");
                Game.State.souvenirSaved = true;
                Game.SaveNow();
                var key = LastSouvenirInGallery ? "toast.souvenir_saved_gallery" : "toast.souvenir_saved";
                Game.Menus.ShowToastRaw(string.Format(Loc.Get(key), Path.GetFileName(LastSouvenirPath)), 6f);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hegra] Souvenir failed: " + e.Message);
                Game.Menus.ShowToast("toast.souvenir_failed");
            }
        }
    }
}
