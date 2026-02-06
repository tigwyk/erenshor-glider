using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ErenshorGlider.GUI.Installation;

/// <summary>
/// Service for managing BepInEx and plugin installation operations.
/// </summary>
public class InstallationService : IInstallationService, IDisposable
{
    private const string BepInExDownloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23/BepInEx_x64_5.4.23.0.zip";
    private const string BepInExFileName = "BepInEx_x64.zip";
    private const string GitHubReleasesUrl = "https://api.github.com/repos/erenshor-glider/erenshor-glider/releases/latest";
    private const string GitHubRepoUrl = "https://github.com/erenshor-glider/erenshor-glider";

    private readonly HttpClient _httpClient;
    private bool _disposed;

    /// <summary>
    /// Gets the cache directory for downloaded files.
    /// </summary>
    public string CacheDirectory { get; private set; }

    /// <summary>
    /// Initializes a new instance of the InstallationService class.
    /// </summary>
    public InstallationService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        // Set user agent to avoid issues with some servers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ErenshorGlider/1.0");

        // Initialize cache directory
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        CacheDirectory = Path.Combine(localAppData, "ErenshorGlider", "cache");

        if (!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
        }
    }

    /// <inheritdoc />
    public async Task<string?> DetectErenshorPathAsync()
    {
        // Try Steam registry detection first
        string? path = DetectErenshorPathFromSteam();

        if (path != null && ValidateErenshorPath(path))
        {
            return path;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<string?> DownloadBepInExAsync(IProgress<DownloadProgress>? progress = null)
    {
        string cachedPath = Path.Combine(CacheDirectory, BepInExFileName);

        // Return cached file if it exists
        if (File.Exists(cachedPath))
        {
            return cachedPath;
        }

        try
        {
            progress?.Report(DownloadProgress.Create(0, 0));

            // Download the file
            var response = await _httpClient.GetAsync(BepInExDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            long bytesRead = 0;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(cachedPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                int read;
                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;

                    if (totalBytes > 0)
                    {
                        progress?.Report(DownloadProgress.Create(bytesRead, totalBytes));
                    }
                }
            }

            progress?.Report(DownloadProgress.Create(bytesRead, bytesRead));

            return cachedPath;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<InstallationResult> InstallBepInExAsync(string erenshorPath, string archivePath)
    {
        if (string.IsNullOrEmpty(erenshorPath))
        {
            return InstallationResult.Failed("Erenshor path cannot be empty.");
        }

        if (string.IsNullOrEmpty(archivePath))
        {
            return InstallationResult.Failed("Archive path cannot be empty.");
        }

        if (!File.Exists(archivePath))
        {
            return InstallationResult.Failed($"Archive file not found: {archivePath}");
        }

        if (!Directory.Exists(erenshorPath))
        {
            return InstallationResult.Failed($"Erenshor directory not found: {erenshorPath}");
        }

        try
        {
            // Backup existing doorstop_config.dll if present
            string doorstopPath = Path.Combine(erenshorPath, "doorstop_config.dll");
            string? backupPath = null;

            if (File.Exists(doorstopPath))
            {
                backupPath = doorstopPath + ".backup";
                File.Copy(doorstopPath, backupPath, overwrite: true);
            }

            // Extract the zip archive
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    // Skip directories (they're created automatically when files are extracted)
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    // Get the full destination path
                    string destinationPath = Path.Combine(erenshorPath, entry.FullName);

                    // Ensure the directory exists
                    string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    // Extract the file
                    using (var entryStream = entry.Open())
                    using (var fileStream = File.Create(destinationPath))
                    {
                        await entryStream.CopyToAsync(fileStream);
                    }
                }
            }

            // Create BepInEx/plugins folder if missing
            string pluginsPath = Path.Combine(erenshorPath, "BepInEx", "plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            // Validate installation
            string bepinexCorePath = Path.Combine(erenshorPath, "BepInEx", "core", "BepInEx.dll");
            if (!File.Exists(bepinexCorePath))
            {
                return InstallationResult.Failed("Installation validation failed: BepInEx.dll not found in core folder.");
            }

            return InstallationResult.Succeeded($"BepInEx installed successfully to {erenshorPath}");
        }
        catch (IOException ex) when (IsFileLockException(ex))
        {
            return InstallationResult.Failed("Installation failed: File is locked. Please ensure Erenshor is not running.");
        }
        catch (Exception ex)
        {
            return InstallationResult.Failed($"Installation failed: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<InstallationResult> InstallPluginAsync(string sourceDllPath, string erenshorPath)
    {
        if (string.IsNullOrEmpty(sourceDllPath))
        {
            return InstallationResult.Failed("Source DLL path cannot be empty.");
        }

        if (string.IsNullOrEmpty(erenshorPath))
        {
            return InstallationResult.Failed("Erenshor path cannot be empty.");
        }

        if (!File.Exists(sourceDllPath))
        {
            return InstallationResult.Failed($"Source DLL not found: {sourceDllPath}");
        }

        try
        {
            // Ensure plugins directory exists
            string pluginsPath = Path.Combine(erenshorPath, "BepInEx", "plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            // Copy the plugin DLL
            string destinationPath = Path.Combine(pluginsPath, Path.GetFileName(sourceDllPath));
            File.Copy(sourceDllPath, destinationPath, overwrite: true);

            return InstallationResult.Succeeded($"Plugin installed successfully to {pluginsPath}");
        }
        catch (IOException ex) when (IsFileLockException(ex))
        {
            return InstallationResult.Failed("Plugin installation failed: File is locked. Please ensure Erenshor is not running.");
        }
        catch (Exception ex)
        {
            return InstallationResult.Failed($"Plugin installation failed: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<InstallationStatus> GetBepInExStatusAsync(string erenshorPath)
    {
        if (string.IsNullOrEmpty(erenshorPath) || !Directory.Exists(erenshorPath))
        {
            return InstallationStatus.NotInstalled;
        }

        string bepinexCorePath = Path.Combine(erenshorPath, "BepInEx", "core", "BepInEx.dll");

        return File.Exists(bepinexCorePath)
            ? InstallationStatus.Installed
            : InstallationStatus.NotInstalled;
    }

    /// <inheritdoc />
    public async Task<InstallationStatus> GetPluginStatusAsync(string erenshorPath)
    {
        if (string.IsNullOrEmpty(erenshorPath) || !Directory.Exists(erenshorPath))
        {
            return InstallationStatus.NotInstalled;
        }

        string pluginsPath = Path.Combine(erenshorPath, "BepInEx", "plugins");

        if (!Directory.Exists(pluginsPath))
        {
            return InstallationStatus.NotInstalled;
        }

        // Check for any ErenshorGlider DLL in the plugins folder
        string[] pluginDlls = Directory.GetFiles(pluginsPath, "ErenshorGlider*.dll");

        return pluginDlls.Length > 0
            ? InstallationStatus.Installed
            : InstallationStatus.NotInstalled;
    }

    /// <summary>
    /// Detects Erenshor installation path from Steam registry.
    /// </summary>
    /// <returns>The detected path, or null if not found.</returns>
    private string? DetectErenshorPathFromSteam()
    {
        try
        {
            // Try the 64-bit registry path first
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2879460");

            if (key != null)
            {
                var installLocation = key.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation) && ValidateErenshorPath(installLocation))
                {
                    return installLocation;
                }
            }

            // Fallback to 32-bit Steam on 64-bit Windows
            using var wowKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2879460");

            if (wowKey != null)
            {
                var installLocation = wowKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation) && ValidateErenshorPath(installLocation))
                {
                    return installLocation;
                }
            }

            return null;
        }
        catch (Exception)
        {
            // Handle registry access exceptions gracefully
            return null;
        }
    }

    /// <summary>
    /// Validates that the specified path contains a valid Erenshor installation.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid, false otherwise.</returns>
    private static bool ValidateErenshorPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string exePath = Path.Combine(path, "Erenshor.exe");
        return File.Exists(exePath);
    }

    /// <summary>
    /// Determines if an IOException is due to a file being locked.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a file lock.</returns>
    private static bool IsFileLockException(IOException ex)
    {
        int errorCode = ex.HResult & 0xFFFF;
        return errorCode == 32 || errorCode == 33; // ERROR_SHARING_VIOLATION or ERROR_LOCK_VIOLATION
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            // Get current assembly version
            string currentVersion = GetCurrentAssemblyVersion();

            // Query GitHub API for latest release
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            var response = await _httpClient.GetAsync(GitHubReleasesUrl);

            // Handle rate limiting
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // Rate limited - return no update
                return UpdateCheckResult.NoUpdate(currentVersion);
            }

            if (!response.IsSuccessStatusCode)
            {
                // Network or server error - handle gracefully
                return UpdateCheckResult.NoUpdate(currentVersion);
            }

            string json = await response.Content.ReadAsStringAsync();
            var releaseData = JsonSerializer.Deserialize<JsonElement>(json);

            // Parse tag name (e.g., "v1.0.0" -> "1.0.0")
            string? tagName = null;
            if (releaseData.TryGetProperty("tag_name", out var tagProp))
            {
                tagName = tagProp.GetString()?.TrimStart('v');
            }

            if (string.IsNullOrEmpty(tagName))
            {
                return UpdateCheckResult.NoUpdate(currentVersion);
            }

            // Parse release notes
            string? releaseNotes = null;
            if (releaseData.TryGetProperty("body", out var bodyProp))
            {
                releaseNotes = bodyProp.GetString();
            }

            // Get download URL for the DLL asset
            string? downloadUrl = null;
            if (releaseData.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsProp.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var nameProp))
                    {
                        string assetName = nameProp.GetString() ?? "";
                        if (assetName.Contains("ErenshorGlider") && assetName.EndsWith(".dll"))
                        {
                            if (asset.TryGetProperty("browser_download_url", out var urlProp))
                            {
                                downloadUrl = urlProp.GetString();
                                break;
                            }
                        }
                    }
                }
            }

            // Compare versions
            if (!string.IsNullOrEmpty(tagName) && IsNewerVersion(tagName, currentVersion))
            {
                return UpdateCheckResult.UpdateAvailable(tagName!, currentVersion, releaseNotes, downloadUrl);
            }

            return UpdateCheckResult.NoUpdate(currentVersion);
        }
        catch (HttpRequestException)
        {
            // Offline mode - handle gracefully
            return UpdateCheckResult.NoUpdate(GetCurrentAssemblyVersion());
        }
        catch (TaskCanceledException)
        {
            // Timeout - handle gracefully
            return UpdateCheckResult.NoUpdate(GetCurrentAssemblyVersion());
        }
        catch (JsonException)
        {
            // JSON parsing error - handle gracefully
            return UpdateCheckResult.NoUpdate(GetCurrentAssemblyVersion());
        }
        catch (Exception)
        {
            // Unexpected error - handle gracefully
            return UpdateCheckResult.Failed("Unable to check for updates.");
        }
    }

    /// <summary>
    /// Gets the current assembly version.
    /// </summary>
    private static string GetCurrentAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetName()
            .Version?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Compares two version strings to determine if the first is newer.
    /// </summary>
    /// <param name="latestVersion">The latest version string.</param>
    /// <param name="currentVersion">The current version string.</param>
    /// <returns>True if latestVersion is newer than currentVersion.</returns>
    private static bool IsNewerVersion(string? latestVersion, string currentVersion)
    {
        if (string.IsNullOrEmpty(latestVersion) || string.IsNullOrEmpty(currentVersion))
        {
            return false;
        }

        try
        {
            string latestStr = latestVersion!.TrimStart('v');
            string currentStr = currentVersion.TrimStart('v');
            var latest = Version.Parse(latestStr);
            var current = Version.Parse(currentStr);
            return latest > current;
        }
        catch
        {
            // If version parsing fails, do string comparison
            return string.Compare(latestVersion ?? "", currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }

    /// <summary>
    /// Disposes of resources used by the service.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
