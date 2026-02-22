using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Tuning")]
    public string doorPassword = "251012";
    public float interactDistance = 3.2f;
    public float doctorMinSeconds = 10f;
    public float doctorMaxSeconds = 20f;

    [Header("Room Roots (auto found by name if empty)")]
    public Transform patientRoomRoot;
    public Transform officeRoot;
    public Transform treatmentRoomRoot;

    [Header("Spawn Offsets (relative to room root)")]
    public Vector3 patientSpawnLocal = new Vector3(0f, 0.05f, -2f);
    public Vector3 officeSpawnLocal = new Vector3(0f, 0.05f, 0f);
    public Vector3 treatSpawnLocal = new Vector3(0f, 0.05f, 0f);
    public float patientYaw = 0f;
    public float officeYaw = 0f;
    public float treatYaw = 180f;

    [Header("Runtime Flags")]
    public bool doorUnlocked = false;
    public bool circuitFixed = false;
    public bool inChase = false;
    public bool isHidden = false;
    public bool inTreatment = false;
    public bool screaming = false;
    public bool girlCalmed = false;
    public bool questionnairePassed = false;

    [NonSerialized] public string lastKeypadEntry = "";

    float _chaseEndTime = 0f;

    // Inventory
    readonly HashSet<string> _inventory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // ✅ 让“背包里的物品”能对应到“图片+文字”
    readonly Dictionary<string, DocumentData> _itemDocs = new Dictionary<string, DocumentData>(StringComparer.OrdinalIgnoreCase);

    // ===== Documents =====
    [Header("Documents")]
    public bool documentOpen = false;
    public DocumentData currentDocument = null;

    readonly HashSet<string> _readDocs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    Vector2 _docScroll;

    // ✅ 文档面板里的“Pick up”上下文
    bool _docHasPickup = false;
    string _docPickupItemId = "";
    DocumentData _docPickupDoc = null;
    GameObject _docPickupSource = null;
    bool _docPickupDestroySource = true;

    // UI state
    string _hint = "";
    string _toast = "";
    float _toastUntil = 0f;

    bool _showInventory = false;

    bool _showKeypad = false;
    string _keypadInput = "";
    Action<string> _keypadSubmit;

    bool _showEnd = false;
    string _endText = "";

    // ===== Story Overlay (Reusable Narrative Beats) =====
    [Header("Story Overlay")]
    public bool playIntroOnStart = false;
    public StorySequenceData introSequence;

    bool _storyOpen = false;
    StorySequenceData _storySeq = null;
    string[] _storyLines = null;
    int _storyIndex = 0;
    float _storyNextAutoTime = 0f;

    // runtime config for ad-hoc story (when _storySeq == null)
    bool _runtimeAllowSkip = true;
    bool _runtimeBlockInput = true;
    float _runtimeAutoAdvanceSeconds = 0f;
    float _runtimeOverlayAlpha = 0.86f;
    string _runtimeTitle = "";

    // ===== UI Blocking (Modal stack) =====
    int _uiBlockCount = 0;

    FPSController _player;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        Application.targetFrameRate = 60;

        patientRoomRoot = patientRoomRoot != null ? patientRoomRoot : GameObject.Find("PatientRoom")?.transform;
        officeRoot = officeRoot != null ? officeRoot : GameObject.Find("Office")?.transform;
        treatmentRoomRoot = treatmentRoomRoot != null ? treatmentRoomRoot : GameObject.Find("TreatmentRoom")?.transform;

        _player = FindObjectOfType<FPSController>();

        Toast("Objective: Go to the Lock. Press E to enter password.", 3f);

        ApplyUiMode(IsUiBlocking());

        if (playIntroOnStart && introSequence != null)
            PlayStory(introSequence);
    }

    void Update()
    {
        // Story overlay input (modal)
        if (_storyOpen && !_showEnd)
        {
            float auto = (_storySeq != null) ? _storySeq.autoAdvanceSeconds : _runtimeAutoAdvanceSeconds;
            bool allowSkip = (_storySeq != null) ? _storySeq.allowSkip : _runtimeAllowSkip;

            // auto-advance
            if (auto > 0f && Time.time >= _storyNextAutoTime)
                NextStoryLine();

            // manual advance (keyboard only)
            // Mouse-click advancing is handled in OnGUI so we don't double-advance when clicking the "Next" button.
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                NextStoryLine();

            // skip/close
            if (allowSkip && Input.GetKeyDown(KeyCode.Escape))
                CloseStory();

            // While story is open, do not process other gameplay inputs.
            return;
        }

        // 背包 I 切换
        if (Input.GetKeyDown(KeyCode.I) && !_showEnd)
        {
            _showInventory = !_showInventory;
            ApplyUiMode(IsUiBlocking());
        }

        // 文档 Esc 关闭（不会拾取，拾取必须点 Pick up）
        if (documentOpen && Input.GetKeyDown(KeyCode.Escape) && !_showEnd)
        {
            CloseDocument();
        }

        if (_showEnd)
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            return;
        }

        // Chase timer check
        if (inChase && !isHidden)
        {
            if (Time.time >= _chaseEndTime)
            {
                Fail("Doctor found you (you didn't hide in time).");
            }
        }
    }

    // =========================
    // ✅ UI 模态控制
    // =========================
    void ApplyUiMode(bool uiOn)
    {
        _player = _player != null ? _player : FindObjectOfType<FPSController>();

        if (_player != null)
        {
            _player.SetFrozen(uiOn);
            _player.LockCursor(!uiOn);
        }
        else
        {
            Cursor.lockState = uiOn ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = uiOn;
        }
    }

    public void SetHint(string hint) => _hint = hint ?? "";

    public void Toast(string msg, float seconds = 2.0f)
    {
        _toast = msg ?? "";
        _toastUntil = Time.time + Mathf.Max(0.1f, seconds);
    }

    public bool IsUiBlocking()
        => _uiBlockCount > 0 || _showKeypad || _showInventory || _showEnd || documentOpen || _storyOpen;

    // ====== Modal UI Stack ======
    public void PushUiBlock()
    {
        _uiBlockCount++;
        ApplyUiMode(true);
    }

    public void PopUiBlock()
    {
        _uiBlockCount = Mathf.Max(0, _uiBlockCount - 1);
        ApplyUiMode(IsUiBlocking());
    }

    // =========================
    // ✅ Story Overlay API
    // =========================
    public bool IsStoryOpen() => _storyOpen;

    /// <summary>
    /// Play a reusable story sequence (full-screen overlay). Blocks gameplay input while open.
    /// </summary>
    public void PlayStory(StorySequenceData seq)
    {
        if (seq == null) return;
        if (seq.lines == null || seq.lines.Length == 0) return;

        _storySeq = seq;
        _storyLines = seq.lines;
        _storyIndex = 0;
        _storyOpen = true;

        if (seq.blockInput) PushUiBlock();
        else ApplyUiMode(IsUiBlocking());

        _storyNextAutoTime = Time.time + Mathf.Max(0.01f, seq.autoAdvanceSeconds);
    }

    /// <summary>
    /// Play ad-hoc story lines (no ScriptableObject needed).
    /// </summary>
    public void PlayStoryLines(string[] lines, float autoAdvanceSeconds = 0f, bool allowSkip = true, bool blockInput = true, float overlayAlpha = 0.86f, string title = "")
    {
        if (lines == null || lines.Length == 0) return;

        _storySeq = null;
        _storyLines = lines;
        _storyIndex = 0;
        _storyOpen = true;

        _runtimeAllowSkip = allowSkip;
        _runtimeBlockInput = blockInput;
        _runtimeAutoAdvanceSeconds = Mathf.Max(0f, autoAdvanceSeconds);
        _runtimeOverlayAlpha = Mathf.Clamp01(overlayAlpha);
        _runtimeTitle = title ?? "";

        if (blockInput) PushUiBlock();
        else ApplyUiMode(IsUiBlocking());

        _storyNextAutoTime = (_runtimeAutoAdvanceSeconds > 0f)
            ? Time.time + Mathf.Max(0.01f, _runtimeAutoAdvanceSeconds)
            : float.PositiveInfinity;
    }

    void NextStoryLine()
    {
        if (!_storyOpen) return;
        if (_storyLines == null || _storyLines.Length == 0) { CloseStory(); return; }

        _storyIndex++;
        if (_storyIndex >= _storyLines.Length)
        {
            CloseStory();
            return;
        }

        float auto = (_storySeq != null) ? _storySeq.autoAdvanceSeconds : _runtimeAutoAdvanceSeconds;
        _storyNextAutoTime = auto > 0f ? (Time.time + Mathf.Max(0.01f, auto)) : float.PositiveInfinity;
    }

    public void CloseStory()
    {
        if (!_storyOpen) return;

        bool shouldPop = (_storySeq != null) ? _storySeq.blockInput : _runtimeBlockInput;

        _storyOpen = false;
        _storySeq = null;
        _storyLines = null;
        _storyIndex = 0;
        _storyNextAutoTime = 0f;

        if (shouldPop) PopUiBlock();
        else ApplyUiMode(IsUiBlocking());
    }

    // ===== Inventory =====
    public bool HasItem(string itemId) => _inventory.Contains(itemId);

    // ✅ 新增：可带 doc（用于背包 View）
    public void AddItem(string itemId, DocumentData doc = null)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;

        itemId = itemId.Trim();
        _inventory.Add(itemId);

        if (doc != null)
            _itemDocs[itemId] = doc;

        Toast($"Picked up: {itemId}", 2.0f);
    }

    public void RemoveItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        itemId = itemId.Trim();

        _inventory.Remove(itemId);
        _itemDocs.Remove(itemId);
    }

    public IEnumerable<string> InventoryItems() => _inventory;

    // ===== Keypad =====
    public void OpenKeypad(Action<string> onSubmit, string prefill = "")
    {
        _showKeypad = true;
        _keypadSubmit = onSubmit;
        _keypadInput = prefill ?? "";
        ApplyUiMode(true);
    }

    public void CloseKeypad()
    {
        _showKeypad = false;
        _keypadSubmit = null;
        ApplyUiMode(IsUiBlocking());
    }

    // ===== Documents =====
    public void OpenDocument(DocumentData doc)
    {
        if (doc == null) return;

        currentDocument = doc;
        documentOpen = true;
        _docScroll = Vector2.zero;

        // ✅ 普通查看时，清空拾取上下文
        _docHasPickup = false;
        _docPickupItemId = "";
        _docPickupDoc = null;
        _docPickupSource = null;
        _docPickupDestroySource = true;

        ApplyUiMode(true);
    }

    // ✅ 新增：打开文档 + 在文档里显示 Pick up
    public void OpenDocumentForPickup(DocumentData doc, string itemId, GameObject source, bool destroySource)
    {
        if (doc == null) return;
        if (string.IsNullOrWhiteSpace(itemId)) { OpenDocument(doc); return; }

        currentDocument = doc;
        documentOpen = true;
        _docScroll = Vector2.zero;

        _docHasPickup = true;
        _docPickupItemId = itemId.Trim();
        _docPickupDoc = doc;
        _docPickupSource = source;
        _docPickupDestroySource = destroySource;

        // 读过就算 read（可选）
        if (!string.IsNullOrWhiteSpace(doc.id))
            MarkDocumentRead(doc.id);

        ApplyUiMode(true);
    }

    public void CloseDocument()
    {
        documentOpen = false;
        currentDocument = null;

        // ✅ 关闭也清掉拾取上下文（避免下次误显示）
        _docHasPickup = false;
        _docPickupItemId = "";
        _docPickupDoc = null;
        _docPickupSource = null;
        _docPickupDestroySource = true;

        ApplyUiMode(IsUiBlocking());
    }

    void PickupFromDocumentIfPossible()
    {
        if (!_docHasPickup) return;
        if (string.IsNullOrWhiteSpace(_docPickupItemId)) return;

        // 已经有了就不重复拾取
        if (!HasItem(_docPickupItemId))
            AddItem(_docPickupItemId, _docPickupDoc);

        // 处理场景里的物体
        if (_docPickupSource != null)
        {
            if (_docPickupDestroySource) Destroy(_docPickupSource);
            else _docPickupSource.SetActive(false);
        }

        CloseDocument();
    }

    public void MarkDocumentRead(string docId)
    {
        if (string.IsNullOrWhiteSpace(docId)) return;
        _readDocs.Add(docId.Trim());
    }

    public bool HasReadDocument(string docId)
    {
        if (string.IsNullOrWhiteSpace(docId)) return false;
        return _readDocs.Contains(docId.Trim());
    }

    // ===== Ending =====
    public void End(string text)
    {
        _showEnd = true;
        _endText = text ?? "";
        ApplyUiMode(true);
    }

    public void Fail(string reason)
    {
        Toast($"FAIL: {reason}", 2.5f);
        Restart();
    }

    public void Restart()
    {
        doorUnlocked = false;
        circuitFixed = false;
        inChase = false;
        isHidden = false;
        inTreatment = false;
        screaming = false;
        girlCalmed = false;
        questionnairePassed = false;

        lastKeypadEntry = "";

        _inventory.Clear();
        _itemDocs.Clear();
        _readDocs.Clear();

        _showInventory = false;
        _showKeypad = false;
        _keypadInput = "";
        _keypadSubmit = null;
        _showEnd = false;
        _endText = "";
        _hint = "";

        _uiBlockCount = 0;

        documentOpen = false;
        currentDocument = null;

        _docHasPickup = false;
        _docPickupItemId = "";
        _docPickupDoc = null;
        _docPickupSource = null;
        _docPickupDestroySource = true;

        var door = GameObject.Find("PatientRoom/Door");
        if (door != null) door.SetActive(true);

        var doll = GameObject.Find("Office/Doll");
        if (doll != null) doll.SetActive(true);

        _player = _player != null ? _player : FindObjectOfType<FPSController>();
        if (_player != null && patientRoomRoot != null)
        {
            _player.TeleportTo(patientRoomRoot.position + patientSpawnLocal, patientYaw);
        }

        ApplyUiMode(false);

        Toast("Restarted. Objective: Unlock the door at Lock.", 2.5f);
    }

    public void TeleportToPatient()
    {
        _player = _player != null ? _player : FindObjectOfType<FPSController>();
        if (_player == null || patientRoomRoot == null) return;
        _player.TeleportTo(patientRoomRoot.position + patientSpawnLocal, patientYaw);
    }

    public void TeleportToOffice()
    {
        _player = _player != null ? _player : FindObjectOfType<FPSController>();
        if (_player == null) return;

        Vector3 pos = officeRoot != null ? officeRoot.position + officeSpawnLocal : new Vector3(0f, 0.05f, 10.5f);
        _player.TeleportTo(pos, officeYaw);
    }

    public void TeleportToTreatment()
    {
        _player = _player != null ? _player : FindObjectOfType<FPSController>();
        if (_player == null) return;

        Vector3 pos = treatmentRoomRoot != null ? treatmentRoomRoot.position + treatSpawnLocal : new Vector3(0f, -2.95f, 13f);
        _player.TeleportTo(pos, treatYaw);
    }

    public void StartChaseTimer()
    {
        inChase = true;
        isHidden = false;
        float dur = UnityEngine.Random.Range(doctorMinSeconds, doctorMaxSeconds);
        _chaseEndTime = Time.time + dur;
        Toast($"Doctor patrol started ({dur:0}s). Hide in the closet!", 2.2f);
    }

    public float ChaseTimeLeft()
    {
        if (!inChase) return 0f;
        return Mathf.Max(0f, _chaseEndTime - Time.time);
    }

    void OnGUI()
    {
        int pad = 12;
        int w = Screen.width;
        int h = Screen.height;

        GUI.skin.label.fontSize = 18;
        GUI.skin.box.fontSize = 18;
        GUI.skin.button.fontSize = 18;
        GUI.skin.textField.fontSize = 18;

        if (!string.IsNullOrEmpty(_hint))
        {
            var box = new Rect(pad, h - 98, w - pad * 2, 70);
            GUI.Box(box, _hint);
        }

        if (inChase && !isHidden && !_showEnd)
        {
            float left = ChaseTimeLeft();
            GUI.Box(new Rect(pad, pad + 40, w - pad * 2, 34), $"Doctor arrives in: {left:0.0}s");
        }

        if (!string.IsNullOrEmpty(_toast) && Time.time < _toastUntil)
        {
            GUI.Box(new Rect(pad, pad, w - pad * 2, 34), _toast);
        }

        // ======================
        // ✅ Story Overlay (fullscreen)
        // ======================
        if (_storyOpen && !_showEnd)
        {
            float alpha = (_storySeq != null) ? Mathf.Clamp01(_storySeq.overlayAlpha) : Mathf.Clamp01(_runtimeOverlayAlpha);
            bool allowSkip = (_storySeq != null) ? _storySeq.allowSkip : _runtimeAllowSkip;
            float auto = (_storySeq != null) ? _storySeq.autoAdvanceSeconds : _runtimeAutoAdvanceSeconds;

            string title = (_storySeq != null) ? (_storySeq.title ?? "") : (_runtimeTitle ?? "");
            bool showTitle = !string.IsNullOrWhiteSpace(title);

            string line = "";
            if (_storyLines != null && _storyLines.Length > 0)
            {
                int idx = Mathf.Clamp(_storyIndex, 0, _storyLines.Length - 1);
                line = _storyLines[idx] ?? "";
            }

            // Dark overlay
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;

            // Text box centered
            float boxW = Mathf.Min(980, w - 120);
            float boxH = Mathf.Min(320, h - 120);
            Rect box = new Rect((w - boxW) * 0.5f, (h - boxH) * 0.62f, boxW, boxH);
            GUI.Box(box, "");

            float tx = box.x + 24;
            float ty = box.y + 18;
            float tw = box.width - 48;

            if (showTitle)
            {
                GUI.Label(new Rect(tx, ty, tw, 26), title);
                ty += 32;
            }

            GUI.Label(new Rect(tx, ty, tw, box.height - 96), line);

            string hint = auto > 0f ? "(Auto)" : "(Click / Space to continue)";
            if (allowSkip) hint += "   Esc: Skip";
            GUI.Label(new Rect(tx, box.y + box.height - 44, tw, 22), hint);

            // Next button
            Rect nextBtn = new Rect(box.x + box.width - 170, box.y + box.height - 58, 150, 36);
            bool advancedThisGui = false;
            if (GUI.Button(nextBtn, "Next"))
            {
                NextStoryLine();
                advancedThisGui = true;
            }

            // Click-to-continue anywhere inside the text box (but NOT on the Next button), handled here to avoid double-advance.
            // IMGUI fires multiple event types per frame; we only advance on MouseDown and consume the event.
            Event e = Event.current;
            if (!advancedThisGui && auto <= 0f && e != null && e.type == EventType.MouseDown && e.button == 0)
            {
                if (box.Contains(e.mousePosition) && !nextBtn.Contains(e.mousePosition))
                {
                    NextStoryLine();
                    e.Use();
                }
            }

            // Block the rest of GUI while overlay is open
            return;
        }

        // ======================
        // ✅ Inventory：可 View
        // ======================
        if (_showInventory && !_showEnd)
        {
            var box = new Rect(pad, 90, 520, 320);
            GUI.Box(box, "Backpack (I to close)");

            int y = 122;
            bool any = false;

            foreach (var it in _inventory)
            {
                any = true;

                GUI.Label(new Rect(pad + 18, y, 300, 26), "• " + it);

                bool hasDoc = _itemDocs.TryGetValue(it, out var doc) && doc != null;
                if (hasDoc)
                {
                    if (GUI.Button(new Rect(pad + 340, y - 2, 150, 30), "View"))
                    {
                        OpenDocument(doc);
                    }
                }

                y += 34;
            }

            if (!any) GUI.Label(new Rect(pad + 18, y, 380, 26), "(empty)");
        }

        if (_showKeypad && !_showEnd)
        {
            var box = new Rect(w / 2 - 220, h / 2 - 150, 440, 300);
            GUI.Box(box, "Door Keypad");
            GUI.Label(new Rect(box.x + 20, box.y + 50, 400, 24), "Hint: 251012");
            _keypadInput = GUI.TextField(new Rect(box.x + 20, box.y + 85, 400, 36), _keypadInput, 16);

            if (GUI.Button(new Rect(box.x + 20, box.y + 140, 190, 44), "Submit"))
            {
                lastKeypadEntry = _keypadInput;
                _keypadSubmit?.Invoke(_keypadInput);
            }

            if (GUI.Button(new Rect(box.x + 230, box.y + 140, 190, 44), "Cancel"))
                CloseKeypad();
        }

        // ======================
        // ✅ Document：图片+文字 + Pick up
        // ======================
        // ======================
        // ✅ Document：左图右字 + Pick up
        // ======================
        if (documentOpen && currentDocument != null && !_showEnd)
        {
            float winW = Mathf.Min(860, w - 80);
            float winH = Mathf.Min(620, h - 80);
            var box = new Rect((w - winW) * 0.5f, (h - winH) * 0.5f, winW, winH);

            GUI.Box(box, currentDocument.title ?? "");

            float padX = 16f;
            float x = box.x + padX;
            float y = box.y + 52f;


            float bottomH = 62f;
            float contentH = box.height - (y - box.y) - bottomH;

            float imgW = 240f;
            float gap = 14f;

            bool hasImg = currentDocument.image != null;

            float rightX = x + (hasImg ? (imgW + gap) : 0f);
            float rightW = box.width - padX * 2 - (hasImg ? (imgW + gap) : 0f);

            // Left: image
            if (hasImg)
            {
                var tex = currentDocument.image.texture;
                var imgRect = new Rect(x, y, imgW, contentH);
                GUI.DrawTexture(imgRect, tex, ScaleMode.ScaleToFit);
            }

            // Right: scroll text
            var scrollRect = new Rect(rightX, y, rightW, contentH);

            // 粗略估算内容高度，避免长文本被截断
            int len = currentDocument.body != null ? currentDocument.body.Length : 0;
            float approxH = Mathf.Max(contentH + 10f, len * 0.9f);

            var viewRect = new Rect(0, 0, scrollRect.width - 20f, approxH);

            _docScroll = GUI.BeginScrollView(scrollRect, _docScroll, viewRect);
            GUI.Label(new Rect(0, 0, viewRect.width, viewRect.height), currentDocument.body);
            GUI.EndScrollView();

            // Buttons
            float btnY = box.y + box.height - 52;

            // ✅ Pick up（只有打开方式是 OpenDocumentForPickup 才显示）
            if (_docHasPickup && !string.IsNullOrWhiteSpace(_docPickupItemId))
            {
                string btn = HasItem(_docPickupItemId) ? "Picked" : "Pick up";
                GUI.enabled = !HasItem(_docPickupItemId);
                if (GUI.Button(new Rect(box.x + 16, btnY, 160, 36), btn))
                {
                    PickupFromDocumentIfPossible();
                }
                GUI.enabled = true;
            }

            if (GUI.Button(new Rect(box.x + box.width - 160, btnY, 140, 36), "Close (Esc)"))
                CloseDocument();
        }

        if (_showEnd)
        {
            var box = new Rect(w / 2 - 280, h / 2 - 180, 560, 360);
            GUI.Box(box, "ENDING");
            GUI.Label(new Rect(box.x + 20, box.y + 60, 520, 190), _endText);
            GUI.Label(new Rect(box.x + 20, box.y + 265, 520, 26), "R = Restart");
        }
    }
}