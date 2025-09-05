using Microsoft.JSInterop;

namespace JsonBlazer.Services;

public class VirtualScrollService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<VirtualScrollService>? _objectRef;
    private bool _isInitialized = false;

    public VirtualScrollService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        _objectRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("initializeVirtualScroll", _objectRef);
        _isInitialized = true;
    }

    public async Task<string> CreateVirtualScrollContainer(string containerId, VirtualScrollOptions options)
    {
        if (!_isInitialized) await InitializeAsync();

        return await _jsRuntime.InvokeAsync<string>("createVirtualScrollContainer", containerId, new
        {
            options.ItemHeight,
            options.ContainerHeight,
            options.BufferSize,
            options.EnableLazyLoading,
            options.LoadingThreshold,
            options.OverScan
        });
    }

    public async Task UpdateVirtualScrollData<T>(string containerId, IEnumerable<T> data)
    {
        await _jsRuntime.InvokeVoidAsync("updateVirtualScrollData", containerId, data);
    }

    public async Task ScrollToIndex(string containerId, int index)
    {
        await _jsRuntime.InvokeVoidAsync("scrollVirtualScrollToIndex", containerId, index);
    }

    public async Task ScrollToItem(string containerId, string itemId)
    {
        await _jsRuntime.InvokeVoidAsync("scrollVirtualScrollToItem", containerId, itemId);
    }

    public async Task RefreshVirtualScroll(string containerId)
    {
        await _jsRuntime.InvokeVoidAsync("refreshVirtualScroll", containerId);
    }

    public async Task DestroyVirtualScroll(string containerId)
    {
        await _jsRuntime.InvokeVoidAsync("destroyVirtualScroll", containerId);
    }

    [JSInvokable]
    public async Task OnScroll(string containerId, VirtualScrollState state)
    {
        OnScrollChanged?.Invoke(containerId, state);
        await Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnItemVisible(string containerId, int index, string itemId)
    {
        OnItemBecameVisible?.Invoke(containerId, index, itemId);
        await Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnLoadMore(string containerId, int currentCount)
    {
        OnLoadMoreRequested?.Invoke(containerId, currentCount);
        await Task.CompletedTask;
    }

    public event Action<string, VirtualScrollState>? OnScrollChanged;
    public event Action<string, int, string>? OnItemBecameVisible;
    public event Action<string, int>? OnLoadMoreRequested;

    public void Dispose()
    {
        _objectRef?.Dispose();
    }
}

public class VirtualScrollOptions
{
    public int ItemHeight { get; set; } = 30;
    public int ContainerHeight { get; set; } = 400;
    public int BufferSize { get; set; } = 5;
    public bool EnableLazyLoading { get; set; } = true;
    public int LoadingThreshold { get; set; } = 10;
    public int OverScan { get; set; } = 3;
}

public class VirtualScrollState
{
    public int ScrollTop { get; set; }
    public int ScrollHeight { get; set; }
    public int ClientHeight { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int VisibleCount { get; set; }
    public int TotalCount { get; set; }
}