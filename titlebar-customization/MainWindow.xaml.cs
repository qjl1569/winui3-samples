// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.
using System;
using System.Runtime.InteropServices;

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace titlebar_customization
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.InitializeTitleBar();
        }

        private void MinMaxCloseControl_OnClose(object sender, System.EventArgs e)
        {
            Close();
        }

        private void MinMaxCloseControl_OnMin(object sender, System.EventArgs e)
        {
            PInvoke.ShowWindow((HWND)WindowHandle, SHOW_WINDOW_CMD.SW_MINIMIZE);
        }

        private void MinMaxCloseControl_OnMax(object sender, System.EventArgs e)
        {
            PInvoke.ShowWindow((HWND)WindowHandle, PInvoke.IsZoomed((HWND)WindowHandle) ? SHOW_WINDOW_CMD.SW_RESTORE : SHOW_WINDOW_CMD.SW_MAXIMIZE);
        }

        private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            ResizeDragBarWindow();
        }

        private void MinMaxCloseControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeDragBarWindow();
        }

        private void ResizeDragBarWindow()
        {
            m_dragBarWindow?.ResizeDragBarWindow(
                new RECT(
                    (int)RootNavigationView.CompactPaneLength,
                    0,
                    (int)Bounds.Width - (int)MinMaxCloseControl.ActualWidth,
                    (int)MinMaxCloseControl.ActualHeight
                    )
                );
        }
    }

    public sealed partial class MainWindow
    {
        private const nuint WindowSubclassId = 101;

        private SUBCLASSPROC m_subclassProc;

        private DragBarWindow m_dragBarWindow;

        private void InitializeTitleBar()
        {
            m_dragBarWindow = new DragBarWindow(WindowHandle);

            m_subclassProc = new SUBCLASSPROC(SubclassProc);

            PInvoke.SetWindowSubclass((HWND)WindowHandle, m_subclassProc, WindowSubclassId, 0);

            // update frame
            PInvoke.SetWindowPos((HWND)WindowHandle, HWND.Null,
                0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED |
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE   |
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER     |
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE       |
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        }

        private unsafe LRESULT SubclassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
        {
            if (uMsg == PInvoke.WM_NCCALCSIZE)
            {
                if (wParam.Value != 0)
                {
                    var np = (NCCALCSIZE_PARAMS*)lParam.Value;
                    var top = np->rgrc._0.top;
                    PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
                    np->rgrc._0.top = PInvoke.IsZoomed(hWnd) ? 0 : top;
                }
                else
                {
                    var rect = (RECT*)lParam.Value;
                    var top = rect->top;
                    PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
                    rect->top = PInvoke.IsZoomed(hWnd) ? 0 : top;
                }
                return (LRESULT)0;
            }
            return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        public nint WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(this);
        public WindowId WindowId => Win32Interop.GetWindowIdFromWindow(WindowHandle);
        public AppWindow AppWindow => AppWindow.GetFromWindowId(WindowId);
    }

    internal class DragBarWindow
    {
        private WNDPROC m_wndProc;
        private nint m_parentWindowHandle;
        public unsafe DragBarWindow(nint parentWindowHandle)
        {
            m_parentWindowHandle = parentWindowHandle;
            m_wndProc = new WNDPROC(WndProc);

            var className = "DragBarWindowClass" + Guid.NewGuid().ToString();

            fixed (char* classNameLocal = className)
            {
                WNDCLASSEXW wcex = new WNDCLASSEXW()
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                    lpfnWndProc = m_wndProc,
                    hCursor = PInvoke.LoadCursor((HINSTANCE)0, PInvoke.IDC_ARROW),
                    lpszClassName = classNameLocal,
                    hInstance = new HINSTANCE(PInvoke.GetModuleHandle(string.Empty).DangerousGetHandle())
                };

                PInvoke.RegisterClassEx(wcex);
            }

            WindowHandle = PInvoke.CreateWindowEx(
                 WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP,
                 className,
                 string.Empty,
                 WINDOW_STYLE.WS_CHILD,
                 0, 0, 0, 0,
                 (HWND)m_parentWindowHandle,
                 null,
                 PInvoke.GetModuleHandle(string.Empty), 
                 null);
            PInvoke.SetLayeredWindowAttributes((HWND)WindowHandle, new COLORREF(0), 255, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);
        }

        public void ResizeDragBarWindow(RECT rect)
        {
            PInvoke.SetWindowPos((HWND)WindowHandle, (HWND)0, rect.X, rect.Y, rect.Width, rect.Height, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        }

        private LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            switch (uMsg)
            {
                case PInvoke.WM_NCHITTEST:
                    {
                        PInvoke.GetWindowRect(hWnd, out var rect);
                        if (Macros.GET_Y_LPARAM(lParam) - rect.top <= 8)
                        {
                            var isMax = PInvoke.IsZoomed((HWND)m_parentWindowHandle);
                            if (!isMax)
                            {
                                return (LRESULT)(nint)PInvoke.HTTOP;
                            }
                        }

                        return (LRESULT)(nint)PInvoke.HTCAPTION;
                    }
                case PInvoke.WM_NCLBUTTONDOWN:
                case PInvoke.WM_NCLBUTTONUP:
                case PInvoke.WM_NCLBUTTONDBLCLK:
                    {
                        return PInvoke.SendMessage((HWND)m_parentWindowHandle, uMsg, wParam, lParam);
                    }
            }

            return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }

        public nint WindowHandle { get; private set; }
    }

    internal class Macros
    {
        public static int GET_X_LPARAM(LPARAM lParam)
        {
            return ((int)(lParam.Value & 0xffff));
        }
        public static int GET_Y_LPARAM(LPARAM lParam)
        {
            return ((int)(lParam.Value >> 16));
        }

    }
}
