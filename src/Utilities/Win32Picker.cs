using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Utilities;

/// <summary>
/// Native Win32 folder/file picker that bypasses Avalonia's StorageProvider
/// to avoid COM interop crashes on Windows.
/// Each dialog runs on a dedicated STA thread for complete COM isolation.
/// Falls back gracefully on non-Windows platforms (returns null).
/// </summary>
internal static class Win32Picker
{
    private const uint FOS_PICKFOLDERS = 0x20;
    private const uint FOS_FORCEFILESYSTEM = 0x40;
    private const uint SIGDN_FILESYSPATH = 0x80058000;

    /// <summary>
    /// Shows a native Win32 folder picker dialog on a dedicated STA thread.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static Task<string?> PickFolderAsync(string title = "Select Folder")
    {
        if (!OperatingSystem.IsWindows())
            return Task.FromResult<string?>(null);

        var tcs = new TaskCompletionSource<string?>();
        var thread = new Thread(() =>
        {
            string? result = null;
            IFileOpenDialog? dialog = null;
            try
            {
                dialog = (IFileOpenDialog)new FileOpenDialogClass();
                dialog.GetOptions(out uint options);
                dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM);
                dialog.SetTitle(title);

                int hr = dialog.Show(IntPtr.Zero);
                if (hr == 0) // S_OK
                {
                    dialog.GetResult(out IShellItem item);
                    item.GetDisplayName(SIGDN_FILESYSPATH, out result);
                    Marshal.ReleaseComObject(item);
                }
            }
            catch
            {
                // User cancelled or COM error — return null
            }
            finally
            {
                if (dialog != null)
                    Marshal.ReleaseComObject(dialog);
            }
            tcs.TrySetResult(result);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return tcs.Task;
    }

    /// <summary>
    /// Shows a native Win32 file picker dialog on a dedicated STA thread.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static Task<string?> PickFileAsync(string title = "Select File",
        string? filterName = null, string? filterPattern = null)
    {
        if (!OperatingSystem.IsWindows())
            return Task.FromResult<string?>(null);

        var tcs = new TaskCompletionSource<string?>();
        var thread = new Thread(() =>
        {
            string? result = null;
            IFileOpenDialog? dialog = null;
            try
            {
                dialog = (IFileOpenDialog)new FileOpenDialogClass();
                dialog.GetOptions(out uint options);
                dialog.SetOptions(options | FOS_FORCEFILESYSTEM);
                dialog.SetTitle(title);

                // Apply file type filter if provided
                if (!string.IsNullOrEmpty(filterName) && !string.IsNullOrEmpty(filterPattern))
                {
                    var filter = new COMDLG_FILTERSPEC
                    {
                        pszName = filterName,
                        pszSpec = filterPattern
                    };
                    int size = Marshal.SizeOf<COMDLG_FILTERSPEC>();
                    IntPtr ptr = Marshal.AllocCoTaskMem(size);
                    try
                    {
                        Marshal.StructureToPtr(filter, ptr, false);
                        dialog.SetFileTypes(1, ptr);
                        dialog.SetFileTypeIndex(1);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(ptr);
                    }
                }

                int hr = dialog.Show(IntPtr.Zero);
                if (hr == 0) // S_OK
                {
                    dialog.GetResult(out IShellItem item);
                    item.GetDisplayName(SIGDN_FILESYSPATH, out result);
                    Marshal.ReleaseComObject(item);
                }
            }
            catch
            {
                // User cancelled or COM error — return null
            }
            finally
            {
                if (dialog != null)
                    Marshal.ReleaseComObject(dialog);
            }
            tcs.TrySetResult(result);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return tcs.Task;
    }

    #region COM Interop Definitions

    // CLSID for FileOpenDialog
    [ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    private class FileOpenDialogClass { }

    // IFileOpenDialog interface — inherits IFileDialog which inherits IModalWindow
    // All methods must be declared in vtable order
    [ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        // IModalWindow
        [PreserveSig] int Show(IntPtr parent);

        // IFileDialog
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close([MarshalAs(UnmanagedType.Error)] int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);

        // IFileOpenDialog
        void GetResults(out IntPtr ppenum);
        void GetSelectedItems(out IntPtr ppsai);
    }

    // IShellItem interface
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(uint sigdnName,
            [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    // Filter specification for file types
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct COMDLG_FILTERSPEC
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszSpec;
    }

    #endregion
}
