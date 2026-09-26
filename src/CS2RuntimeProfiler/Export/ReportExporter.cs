using System;
using System.IO;
using System.Text;
using Colossal.PSI.Environment;

namespace CS2RuntimeProfiler.Export
{
    public sealed class ReportExportResult
    {
        private ReportExportResult(bool success, string path, string error)
        {
            Success = success;
            Path = path;
            Error = error;
        }

        public bool Success { get; }
        public string Path { get; }
        public string Error { get; }

        public static ReportExportResult Succeeded(string path) => new ReportExportResult(true, path, null);
        public static ReportExportResult Failed(string error) => new ReportExportResult(false, null, error);
    }

    public sealed class ReportExporter
    {
        private const int MaxCollisionRetries = 1000;

        public ReportExportResult Export(PerformanceReport report)
        {
            try
            {
                var directory = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id);
                Directory.CreateDirectory(directory);

                var timestamp = DateTime.Now;
                var stem = $"CS2Profiler-report-{timestamp:yyyy-MM-dd_HHmmss_fff}";
                var json = PerformanceReportSerializer.Serialize(report);
                var encoding = new UTF8Encoding(false);

                for (var attempt = 0; attempt < MaxCollisionRetries; attempt++)
                {
                    var suffix = attempt == 0 ? string.Empty : $"-{attempt}";
                    var path = Path.Combine(directory, stem + suffix + ".json");

                    try
                    {
                        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(stream, encoding))
                            writer.Write(json);

                        return ReportExportResult.Succeeded(path);
                    }
                    catch (IOException) when (File.Exists(path))
                    {
                        // Another export already claimed this name. Retry with a numeric suffix.
                    }
                }

                throw new IOException("Could not allocate a unique profiler report filename.");
            }
            catch (Exception ex)
            {
                Mod.Log.Error(ex, "Failed to export CS2 Runtime Profiler report");
                return ReportExportResult.Failed(PrivacySanitizer.Sanitize(ex.Message));
            }
        }
    }
}
