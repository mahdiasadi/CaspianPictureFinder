using Microsoft.ML.OnnxRuntime;

namespace PhotoAI.Core.Inference;

public class CpuInferenceEngine : IInferenceEngine
{
    public ProcessingBackend Backend => ProcessingBackend.Cpu;
    public bool IsAvailable => true;
    public string DeviceInfo => "CPU (ONNX Runtime CPU EP)";

    public async Task<IInferenceSession> CreateSessionAsync(string modelPath, SessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        var sessionOptions = options ?? new SessionOptions();
        sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        sessionOptions.IntraOpNumThreads = Environment.ProcessorCount;
        
        var session = new InferenceSession(modelPath, sessionOptions);
        
        return new OnnxInferenceSession(session, modelPath, ProcessingBackend.Cpu);
    }

    public async Task<HardwareInfo> GetHardwareInfoAsync()
    {
        await Task.CompletedTask;
        
        return new HardwareInfo
        {
            CpuName = GetCpuName(),
            CpuCores = Environment.ProcessorCount,
            TotalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            HasCuda = false
        };
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

internal class OnnxInferenceSession : IInferenceSession
{
    private readonly InferenceSession _session;
    private readonly string _modelPath;
    private readonly ProcessingBackend _backend;
    private bool _disposed;

    public OnnxInferenceSession(InferenceSession session, string modelPath, ProcessingBackend backend)
    {
        _session = session;
        _modelPath = modelPath;
        _backend = backend;
    }

    public string ModelPath => _modelPath;
    public ProcessingBackend Backend => _backend;
    
    public IReadOnlyList<string> InputNames => _session.InputMetadata.Keys.ToList();
    public IReadOnlyList<string> OutputNames => _session.OutputMetadata.Keys.ToList();
    
    public IReadOnlyList<int[]> InputShapes => _session.InputMetadata.Values.Select(m => m.Dimensions.Select(d => (int)d).ToArray()).ToList();
    public IReadOnlyList<int[]> OutputShapes => _session.OutputMetadata.Values.Select(m => m.Dimensions.Select(d => (int)d).ToArray()).ToList();

    public async Task<InferenceResult> RunAsync(IReadOnlyCollection<NamedOnnxValue> inputs, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var outputNames = OutputNames.ToArray();
        var results = _session.Run(inputs, outputNames);
        
        stopwatch.Stop();
        
        return new InferenceResult
        {
            Outputs = results,
            InferenceTime = stopwatch.Elapsed
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}