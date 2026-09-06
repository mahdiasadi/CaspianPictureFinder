using Microsoft.ML.OnnxRuntime;

namespace PhotoAI.Core.Inference;

public interface IInferenceSession : IDisposable
{
    string ModelPath { get; }
    ProcessingBackend Backend { get; }
    IReadOnlyList<string> InputNames { get; }
    IReadOnlyList<string> OutputNames { get; }
    IReadOnlyList<int[]> InputShapes { get; }
    IReadOnlyList<int[]> OutputShapes { get; }

    Task<InferenceResult> RunAsync(IReadOnlyCollection<NamedOnnxValue> inputs, CancellationToken cancellationToken = default);
}

public class InferenceResult
{
    public IReadOnlyList<NamedOnnxValue> Outputs { get; set; } = Array.Empty<NamedOnnxValue>();
    public TimeSpan InferenceTime { get; set; }
}

public interface IInferenceEngine
{
    ProcessingBackend Backend { get; }
    bool IsAvailable { get; }
    string DeviceInfo { get; }

    Task<IInferenceSession> CreateSessionAsync(string modelPath, SessionOptions? options = null, CancellationToken cancellationToken = default);
    Task<HardwareInfo> GetHardwareInfoAsync();
}

public class HardwareInfo
{
    public string CpuName { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public long TotalMemoryBytes { get; set; }
    public bool HasCuda { get; set; }
    public string? CudaVersion { get; set; }
    public string? GpuName { get; set; }
    public long GpuMemoryBytes { get; set; }
    public int GpuDeviceCount { get; set; }
}