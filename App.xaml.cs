using CaseIH8940MS.Resources.Styles;
#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif

namespace CaseIH8940MS;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
#if WINDOWS
		// iOS-ish fonts/colors/Shell chrome for WinUI preview only. On iOS/MacCatalyst, omit so UIKit defaults surface.
		Resources.MergedDictionaries.Add(new WindowsPreviewStyles());
#endif
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
#if WINDOWS
		// Roughly phone portrait + title bar chrome on WinUI.
		window.Width = 448;
		window.Height = 960;
		window.MinimumWidth = 400;
		window.MinimumHeight = 720;
		// Unpackaged WinUI: taskbar needs SetTaskbarIcon, not only SetIcon (Windows App SDK 1.7+).
		window.HandlerChanged += WinEnqueueApplyWindowIcons;
#endif
		return window;
	}

#if WINDOWS
	static void WinEnqueueApplyWindowIcons(object? sender, EventArgs e)
	{
		if (sender is not Window w)
			return;
		if (w.Handler?.PlatformView is not Microsoft.UI.Xaml.Window winUi)
			return;

		w.HandlerChanged -= WinEnqueueApplyWindowIcons;

		// Defer until WinUI window / HWND is ready for AppWindow.GetFromWindowId.
		if (winUi.DispatcherQueue is null)
		{
			WinTryApplyWindowIconsToNative(w);
			return;
		}

		_ = winUi.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
			WinTryApplyWindowIconsWithRetry(w, winUi, 0));
	}

	static void WinTryApplyWindowIconsWithRetry(Window mauiWindow, Microsoft.UI.Xaml.Window winUi, int attempt)
	{
		if (WinTryApplyWindowIconsToNative(mauiWindow))
			return;
		if (attempt >= 12 || winUi.DispatcherQueue is null)
			return;
		_ = winUi.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
			() => WinTryApplyWindowIconsWithRetry(mauiWindow, winUi, attempt + 1));
	}

	/// <summary>Applies MauiIcon output (appicon.ico next to the app) to title bar and taskbar.</summary>
	static bool WinTryApplyWindowIconsToNative(Window mauiWindow)
	{
		if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window winUi)
			return false;

		var iconPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "appicon.ico"));
		if (!File.Exists(iconPath))
			return false;

		var hWnd = WindowNative.GetWindowHandle(winUi);
		if (hWnd == IntPtr.Zero)
			return false;

		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
		var appWindow = AppWindow.GetFromWindowId(windowId);
		appWindow.SetIcon(iconPath);
		appWindow.SetTaskbarIcon(iconPath);
		return true;
	}
#endif
}
