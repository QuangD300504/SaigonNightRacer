using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI References (Shared Overlay)")]
    [Tooltip("Panel nền mờ phủ toàn màn hình, dùng chung cho mọi menu (Settings/Store/...)")]
    public GameObject settingPanel;

    [Header("Settings Menu")]
    public GameObject settingMenuPrefab;
    public Button settingsButton;

    [Header("Store Menu")]
    public GameObject storeMenuPrefab;
    public Button storeButton;

    [Header("Animation")]
    [Tooltip("Thời gian animate vào/ra")]
    public float animationDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Close Button Lookup")]
    [Tooltip("Tên nút Close bên trong mỗi prefab menu. Để rỗng sẽ tự lấy Button đầu tiên tìm thấy.")]
    public string closeButtonName = "CloseButton";

    // ---- Internal state ----
    private enum MenuKind { None, Settings, Store }
    private MenuKind currentMenuKind = MenuKind.None;
    private GameObject currentMenuGO;
    private Coroutine animationCoroutine;
    private bool isMenuOpen = false;

    void Start()
    {
        // Ẩn overlay ban đầu
        if (settingPanel != null) settingPanel.SetActive(false);

        // Gán sự kiện mở từng menu
        if (settingsButton != null) settingsButton.onClick.AddListener(() => ToggleMenu(MenuKind.Settings));
        if (storeButton != null) storeButton.onClick.AddListener(() => ToggleMenu(MenuKind.Store));
    }

    // -------- Public UI hooks --------
    public void PlayGame()
    {
        // Reset audio before scene transition
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSceneTransition();
        }
        
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void Settings() => ToggleMenu(MenuKind.Settings);
    public void OpenSettings() => OpenMenu(MenuKind.Settings);
    public void CloseSettings() => CloseMenu();

    public void OpenStore() => OpenMenu(MenuKind.Store);
    public void CloseStore() => CloseMenu();

    public void Quit()
    {
        Application.Quit();
    }

    // -------- Core modal logic (generic) --------
    private void ToggleMenu(MenuKind kind)
    {
        if (isMenuOpen && currentMenuKind == kind)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu(kind);
        }
    }

    private void OpenMenu(MenuKind kind)
    {
        if (kind == MenuKind.None) return;

        // Nếu đang mở menu khác -> chuyển (switch) mượt
        if (isMenuOpen && currentMenuKind != kind)
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(SwitchMenuCoroutine(kind));
            return;
        }

        if (isMenuOpen) return; // đang mở đúng menu rồi

        var prefab = GetPrefab(kind);
        if (prefab == null || settingPanel == null) return;

        // Bật overlay
        settingPanel.SetActive(true);

        // Tạo menu
        currentMenuGO = Instantiate(prefab, settingPanel.transform);
        StretchToParent(currentMenuGO);

        // Gắn sự kiện close vào nút Close của prefab
        BindCloseButton(currentMenuGO);

        // Animate In
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateIn(currentMenuGO));

        currentMenuKind = kind;
        isMenuOpen = true;
    }

    private void CloseMenu()
    {
        if (!isMenuOpen) return;

        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateOutAndHide());
    }

    private IEnumerator SwitchMenuCoroutine(MenuKind nextKind)
    {
        // Animate out menu hiện tại
        yield return StartCoroutine(AnimateOut(currentMenuGO));

        // Hủy menu cũ
        if (currentMenuGO != null)
        {
            Destroy(currentMenuGO);
            currentMenuGO = null;
        }

        // Tạo menu mới
        var prefab = GetPrefab(nextKind);
        if (prefab == null)
        {
            // Không có prefab -> đóng luôn overlay
            if (settingPanel != null) settingPanel.SetActive(false);
            currentMenuKind = MenuKind.None;
            isMenuOpen = false;
            animationCoroutine = null;
            yield break;
        }

        currentMenuGO = Instantiate(prefab, settingPanel.transform);
        StretchToParent(currentMenuGO);
        BindCloseButton(currentMenuGO);

        // Animate In
        yield return StartCoroutine(AnimateIn(currentMenuGO));

        currentMenuKind = nextKind;
        isMenuOpen = true;
        animationCoroutine = null;
    }

    // -------- Animation helpers --------
    private IEnumerator AnimateIn(GameObject panel)
    {
        if (panel == null) yield break;
        panel.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / animationDuration);
            float s = scaleCurve.Evaluate(k);
            panel.transform.localScale = Vector3.one * s;
            yield return null;
        }
        panel.transform.localScale = Vector3.one;
        animationCoroutine = null;
    }

    private IEnumerator AnimateOut(GameObject panel)
    {
        if (panel == null) yield break;

        Vector3 start = panel.transform.localScale;
        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / animationDuration);
            float s = scaleCurve.Evaluate(1f - k);
            panel.transform.localScale = start * s;
            yield return null;
        }
    }

    private IEnumerator AnimateOutAndHide()
    {
        yield return StartCoroutine(AnimateOut(currentMenuGO));

        if (currentMenuGO != null)
        {
            Destroy(currentMenuGO);
            currentMenuGO = null;
        }

        if (settingPanel != null) settingPanel.SetActive(false);

        isMenuOpen = false;
        currentMenuKind = MenuKind.None;
        animationCoroutine = null;
    }

    // -------- Utils --------
    private GameObject GetPrefab(MenuKind kind)
    {
        switch (kind)
        {
            case MenuKind.Settings: return settingMenuPrefab;
            case MenuKind.Store: return storeMenuPrefab;
            default: return null;
        }
    }

    // Fill full parent rect
    private void StretchToParent(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    private void BindCloseButton(GameObject root)
    {
        Button closeBtn = null;

        // Ưu tiên tìm theo tên
        if (!string.IsNullOrEmpty(closeButtonName))
        {
            var t = root.transform.Find(closeButtonName);
            if (t != null) closeBtn = t.GetComponent<Button>();
        }

        // Không thấy thì lấy Button đầu tiên
        if (closeBtn == null)
        {
            closeBtn = root.GetComponentInChildren<Button>(true);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(CloseMenu);
        }
    }
}
