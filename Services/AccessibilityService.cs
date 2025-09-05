using Microsoft.JSInterop;

namespace JsonBlazer.Services;

public class AccessibilityService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<AccessibilityService>? _objectRef;
    private bool _isInitialized = false;

    public AccessibilityService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _objectRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("initializeAccessibility", _objectRef);
        _isInitialized = true;
    }

    public async Task AnnounceToScreenReader(string message, AccessibilityAnnouncement type = AccessibilityAnnouncement.Polite)
    {
        if (!_isInitialized) await InitializeAsync();
        await _jsRuntime.InvokeVoidAsync("announceToScreenReader", message, type.ToString().ToLowerInvariant());
    }

    public async Task SetElementAriaLabel(string elementId, string label)
    {
        await _jsRuntime.InvokeVoidAsync("setAriaLabel", elementId, label);
    }

    public async Task SetElementAriaDescribedBy(string elementId, string describedById)
    {
        await _jsRuntime.InvokeVoidAsync("setAriaDescribedBy", elementId, describedById);
    }

    public async Task SetElementAriaExpanded(string elementId, bool expanded)
    {
        await _jsRuntime.InvokeVoidAsync("setAriaExpanded", elementId, expanded);
    }

    public async Task SetElementRole(string elementId, string role)
    {
        await _jsRuntime.InvokeVoidAsync("setElementRole", elementId, role);
    }

    public async Task SetElementTabIndex(string elementId, int tabIndex)
    {
        await _jsRuntime.InvokeVoidAsync("setTabIndex", elementId, tabIndex);
    }

    public async Task FocusElement(string elementId, FocusOptions? options = null)
    {
        if (options != null)
        {
            await _jsRuntime.InvokeVoidAsync("focusElementWithOptions", elementId, new
            {
                preventScroll = options.PreventScroll,
                selectText = options.SelectText
            });
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("focusElement", elementId);
        }
    }

    public async Task CreateSkipLink(string targetId, string text, string position = "top-left")
    {
        await _jsRuntime.InvokeVoidAsync("createSkipLink", targetId, text, position);
    }

    public async Task AddLiveRegion(string elementId, AccessibilityLiveRegion type = AccessibilityLiveRegion.Polite)
    {
        await _jsRuntime.InvokeVoidAsync("addLiveRegion", elementId, type.ToString().ToLowerInvariant());
    }

    public async Task UpdateLiveRegion(string elementId, string content)
    {
        await _jsRuntime.InvokeVoidAsync("updateLiveRegion", elementId, content);
    }

    public async Task EnableHighContrastMode(bool enable)
    {
        await _jsRuntime.InvokeVoidAsync("enableHighContrastMode", enable);
    }

    public async Task EnableReducedMotion(bool enable)
    {
        await _jsRuntime.InvokeVoidAsync("enableReducedMotion", enable);
    }

    public async Task SetupFocusTrap(string containerId)
    {
        await _jsRuntime.InvokeVoidAsync("setupFocusTrap", containerId);
    }

    public async Task RemoveFocusTrap(string containerId)
    {
        await _jsRuntime.InvokeVoidAsync("removeFocusTrap", containerId);
    }

    public async Task<AccessibilityFeatures> GetUserAccessibilityPreferences()
    {
        if (!_isInitialized) await InitializeAsync();
        return await _jsRuntime.InvokeAsync<AccessibilityFeatures>("getUserAccessibilityPreferences");
    }

    public async Task ValidateAccessibility(string elementId)
    {
        await _jsRuntime.InvokeVoidAsync("validateAccessibility", elementId);
    }

    [JSInvokable]
    public async Task OnFocusChanged(string elementId, bool hasFocus)
    {
        OnElementFocusChanged?.Invoke(elementId, hasFocus);
        await Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnAccessibilityIssueDetected(AccessibilityIssue issue)
    {
        OnAccessibilityIssue?.Invoke(issue);
        await Task.CompletedTask;
    }

    public event Action<string, bool>? OnElementFocusChanged;
    public event Action<AccessibilityIssue>? OnAccessibilityIssue;

    public void Dispose()
    {
        _objectRef?.Dispose();
    }
}

public enum AccessibilityAnnouncement
{
    Polite,
    Assertive,
    Off
}

public enum AccessibilityLiveRegion
{
    Off,
    Polite,
    Assertive
}

public class FocusOptions
{
    public bool PreventScroll { get; set; } = false;
    public bool SelectText { get; set; } = false;
}

public class AccessibilityFeatures
{
    public bool PrefersReducedMotion { get; set; }
    public bool PrefersHighContrast { get; set; }
    public bool UsesScreenReader { get; set; }
    public bool PrefersColorScheme { get; set; }
    public double FontScale { get; set; } = 1.0;
    public string ColorScheme { get; set; } = "auto";
}

public class AccessibilityIssue
{
    public string ElementId { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}