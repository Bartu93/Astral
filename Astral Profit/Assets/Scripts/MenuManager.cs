using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("ScrollView Settings")]
    [SerializeField] private List<ScrollRect> scrollViews = new List<ScrollRect>();
    [SerializeField] private List<Button> menuButtons = new List<Button>();

    [Header("Visual Feedback")]
    [SerializeField] private Color activeButtonColor = Color.white;
    [SerializeField] private Color inactiveButtonColor = Color.gray;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentActiveIndex = 0;
    private bool isAnimating = false;

    void Start()
    {
        InitializeMenu();
    }

    void InitializeMenu()
    {
        // Ensure we have matching numbers of buttons and scroll views
        if (scrollViews.Count != menuButtons.Count)
        {
            Debug.LogWarning("MenuManager: Number of scroll views doesn't match number of buttons!");
        }

        // Setup button listeners
        for (int i = 0; i < menuButtons.Count; i++)
        {
            int index = i; // Capture the index for the closure
            menuButtons[i].onClick.AddListener(() => SwitchToScrollView(index));
        }

        // Initialize - show first scroll view, hide others
        ShowScrollView(0);
        UpdateButtonVisuals();
    }

    public void SwitchToScrollView(int index)
    {
        // Prevent switching during animation or to the same view
        if (isAnimating || index == currentActiveIndex || index < 0 || index >= scrollViews.Count)
            return;

        if (useAnimation)
        {
            StartCoroutine(AnimateScrollViewSwitch(index));
        }
        else
        {
            ShowScrollView(index);
        }

        currentActiveIndex = index;
        UpdateButtonVisuals();
    }

    void ShowScrollView(int index)
    {
        // Hide all scroll views
        for (int i = 0; i < scrollViews.Count; i++)
        {
            if (scrollViews[i] != null)
            {
                scrollViews[i].gameObject.SetActive(i == index);
            }
        }
    }

    void UpdateButtonVisuals()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (menuButtons[i] != null)
            {
                ColorBlock colors = menuButtons[i].colors;
                colors.normalColor = (i == currentActiveIndex) ? activeButtonColor : inactiveButtonColor;
                menuButtons[i].colors = colors;
            }
        }
    }

    System.Collections.IEnumerator AnimateScrollViewSwitch(int targetIndex)
    {
        isAnimating = true;

        GameObject currentScrollView = scrollViews[currentActiveIndex].gameObject;
        GameObject targetScrollView = scrollViews[targetIndex].gameObject;

        // Activate target scroll view
        targetScrollView.SetActive(true);

        // Get CanvasGroup components (add them if they don't exist)
        CanvasGroup currentGroup = GetOrAddCanvasGroup(currentScrollView);
        CanvasGroup targetGroup = GetOrAddCanvasGroup(targetScrollView);

        // Set initial alpha values
        currentGroup.alpha = 1f;
        targetGroup.alpha = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);

            currentGroup.alpha = 1f - curveValue;
            targetGroup.alpha = curveValue;

            yield return null;
        }

        // Ensure final values
        currentGroup.alpha = 0f;
        targetGroup.alpha = 1f;

        // Deactivate the previous scroll view
        currentScrollView.SetActive(false);

        isAnimating = false;
    }

    CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    // Public methods for external scripts
    public void SwitchToNextScrollView()
    {
        int nextIndex = (currentActiveIndex + 1) % scrollViews.Count;
        SwitchToScrollView(nextIndex);
    }

    public void SwitchToPreviousScrollView()
    {
        int prevIndex = (currentActiveIndex - 1 + scrollViews.Count) % scrollViews.Count;
        SwitchToScrollView(prevIndex);
    }

    public int GetCurrentActiveIndex()
    {
        return currentActiveIndex;
    }

    public ScrollRect GetCurrentActiveScrollView()
    {
        if (currentActiveIndex >= 0 && currentActiveIndex < scrollViews.Count)
        {
            return scrollViews[currentActiveIndex];
        }
        return null;
    }

    // Method to add scroll views and buttons at runtime
    public void AddScrollViewButton(ScrollRect scrollView, Button button)
    {
        if (scrollView != null && button != null)
        {
            scrollViews.Add(scrollView);
            menuButtons.Add(button);

            int index = menuButtons.Count - 1;
            button.onClick.AddListener(() => SwitchToScrollView(index));

            // Hide the new scroll view initially
            scrollView.gameObject.SetActive(false);

            UpdateButtonVisuals();
        }
    }

    // Method to remove scroll views and buttons
    public void RemoveScrollViewButton(int index)
    {
        if (index >= 0 && index < scrollViews.Count)
        {
            scrollViews.RemoveAt(index);
            menuButtons.RemoveAt(index);

            // Adjust current active index if necessary
            if (currentActiveIndex >= scrollViews.Count)
            {
                currentActiveIndex = Mathf.Max(0, scrollViews.Count - 1);
            }

            ShowScrollView(currentActiveIndex);
            UpdateButtonVisuals();
        }
    }
}