using System;
using System.IO;
using System.Threading.Tasks;

namespace ErenshorGlider.GUI.Installation;

/// <summary>
/// Interface for installation operations including BepInEx and plugin management.
/// </summary>
public interface IInstallationService
{
    /// <summary>
    /// Gets the cache directory for downloaded files.
    /// </summary>
    string CacheDirectory { get; }

    /// <summary>
    /// Detects the Erenshor installation path.
    /// </summary>
    /// <returns>The detected path, or null if not found.</returns>
    Task<string?> DetectErenshorPathAsync();

    /// <summary>
    /// Downloads BepInEx to the local cache.
    /// </summary>
    /// <param name="progress">Optional progress reporter for the download.</param>
    /// <returns>The path to the downloaded file, or null if download failed.</returns>
    Task<string?> DownloadBepInExAsync(IProgress<DownloadProgress>? progress = null);

    /// <summary>
    /// Installs BepInEx to the specified Erenshor directory.
    /// </summary>
    /// <param name="erenshorPath">The path to the Erenshor installation.</param>
    /// <param name="archivePath">The path to the BepInEx archive to install.</param>
    /// <returns>The result of the installation operation.</returns>
    Task<InstallationResult> InstallBepInExAsync(string erenshorPath, string archivePath);

    /// <summary>
    /// Installs the plugin DLL to the BepInEx plugins folder.
    /// </summary>
    /// <param name="sourceDllPath">The path to the source plugin DLL.</param>
    /// <param name="erenshorPath">The path to the Erenshor installation.</param>
    /// <returns>The result of the installation operation.</returns>
    Task<InstallationResult> InstallPluginAsync(string sourceDllPath, string erenshorPath);

    /// <summary>
    /// Checks the installation status of BepInEx.
    /// </summary>
    /// <param name="erenshorPath">The path to the Erenshor installation.</param>
    /// <returns>The installation status.</returns>
    Task<InstallationStatus> GetBepInExStatusAsync(string erenshorPath);

    /// <summary>
    /// Checks the installation status of the plugin.
    /// </summary>
    /// <param name="erenshorPath">The path to the Erenshor installation.</param>
    /// <returns>The installation status.</returns>
    Task<InstallationStatus> GetPluginStatusAsync(string erenshorPath);
}

/// <summary>
/// Represents the status of an installation.
/// </summary>
public enum InstallationStatus
{
    /// <summary>
    /// The component is not installed.
    /// </summary>
    NotInstalled,

    /// <summary>
    /// The component is installed.
    /// </summary>
    Installed,

    /// <summary>
    /// The component is installed and an update is available.
    /// </summary>
    UpdateAvailable
}

/// <summary>
/// Represents the result of an installation operation.
/// </summary>
public class InstallationResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets additional details about the operation.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="details">Optional details about the operation.</param>
    /// <returns>A successful installation result.</returns>
    public static InstallationResult Succeeded(string? details = null)
    {
        return new InstallationResult { Success = true, Details = details };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed installation result.</returns>
    public static InstallationResult Failed(string errorMessage)
    {
        return new InstallationResult { Success = false, ErrorMessage = errorMessage };
    }
}

/// <summary>
/// Represents download progress information.
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// Gets the number of bytes received.
    /// </summary>
    public long BytesReceived { get; private set; }

    /// <summary>
    /// Gets the total number of bytes to receive (0 if unknown).
    /// </summary>
    public long TotalBytes { get; private set; }

    /// <summary>
    /// Gets the download progress percentage (0-100, or -1 if unknown).
    /// </summary>
    public int Percentage { get; private set; }

    /// <summary>
    /// Creates a new download progress instance.
    /// </summary>
    /// <param name="bytesReceived">The number of bytes received.</param>
    /// <param name="totalBytes">The total number of bytes.</param>
    /// <returns>A new download progress instance.</returns>
    public static DownloadProgress Create(long bytesReceived, long totalBytes)
    {
        return new DownloadProgress
        {
            BytesReceived = bytesReceived,
            TotalBytes = totalBytes,
            Percentage = totalBytes > 0 ? (int)((bytesReceived * 100) / totalBytes) : -1
        };
    }
}
