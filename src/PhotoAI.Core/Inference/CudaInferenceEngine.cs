using Microsoft.ML.OnnxRuntime;

namespace PhotoAI.Core.Inference;

public class CudaInferenceEngine : IInferenceEngine
{
    public ProcessingBackend Backend => ProcessingBackend.Cuda;
    public bool IsAvailable => CheckCudaAvailability();
    public string DeviceInfo => _deviceInfo ?? "CUDA (checking...)";

    private string? _deviceInfo;
    private int _deviceId = 0;

    public CudaInferenceEngine(int deviceId = 0)
    {
        _deviceId = deviceId;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var info = await GetHardwareInfoAsync();
            if (info.HasCuda)
            {
                _deviceInfo = $"NVIDIA {info.GpuName} ({info.GpuMemoryBytes / (1024*1024*1024)} GB VRAM, CUDA {info.CudaVersion})";
            }
            else
            {
                _deviceInfo = "CUDA not available";
            }
        }
        catch
        {
            _deviceInfo = "CUDA initialization failed";
        }
    }

    public async Task<IInferenceSession> CreateSessionAsync(string modelPath, SessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        var sessionOptions = options ?? new SessionOptions();
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        
        // Append CUDA execution provider
        sessionOptions.AppendExecutionProvider_CUDA(_deviceId);
        
        var session = new InferenceSession(modelPath, sessionOptions);
        
        return new OnnxInferenceSession(session, modelPath, ProcessingBackend.Cuda);
    }

    public async Task<HardwareInfo> GetHardwareInfoAsync()
    {
        await Task.CompletedTask;
        
        var info = new HardwareInfo
        {
            CpuName = GetCpuName(),
            CpuCores = Environment.ProcessorCount,
            TotalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes
        };

        try
        {
            // Check CUDA via nvidia-smi
            var cudaInfo = GetCudaInfo();
            if (cudaInfo.HasCuda)
            {
                info.HasCuda = true;
                info.CudaVersion = cudaInfo.CudaVersion;
                info.GpuName = cudaInfo.GpuName;
                info.GpuMemoryBytes = cudaInfo.GpuMemoryBytes;
                info.GpuDeviceCount = cudaInfo.GpuDeviceCount;
            }
        }
        catch { }

        return info;
    }

    private static bool CheckCudaAvailability()
    {
        try
        {
            var info = GetCudaInfo();
            return info.HasCuda;
        }
        catch
        {
            return false;
        }
    }

    private static (bool HasCuda, string? CudaVersion, string? GpuName, long GpuMemoryBytes, int GpuDeviceCount) GetCudaInfo()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return (false, null, null, 0, 0);

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return (false, null, null, 0, 0);

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return (false, null, null, 0, 0);

            var firstGpu = lines[0].Split(',');
            var gpuName = firstGpu[0].Trim();
            var memoryMb = long.Parse(firstGpu[1].Trim());
            var driverVersion = firstGpu.Length > 2 ? firstGpu[2].Trim() : "unknown";

            // Get CUDA version from driver version or nvcc
            var cudaVersion = GetCudaVersionFromDriver(driverVersion);

            return (true, cudaVersion, gpuName, memoryMb * 1024 * 1024, lines.Length);
        }
        catch
        {
            return (false, null, null, 0, 0);
        }
    }

    private static string GetCudaVersionFromDriver(string driverVersion)
    {
        // Driver version to CUDA version mapping (approximate)
        // This is a rough mapping - in practice you'd query nvcc or cuda libraries
        return driverVersion;
    }

    private static string GetCpuName()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["Name"]?.ToString() ?? "Unknown CPU";
            }
        }
        catch { }
        return "Unknown CPU";
    }
}