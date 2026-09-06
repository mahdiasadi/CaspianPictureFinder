using Microsoft.ML.OnnxRuntime;

namespace PhotoAI.Core.Inference;

public class AutoInferenceEngine : IInferenceEngine
{
    private readonly IInferenceEngine _cpuEngine;
    private readonly IInferenceEngine? _cudaEngine;
    private IInferenceEngine _selectedEngine;

    public ProcessingBackend Backend => _selectedEngine.Backend;
    public bool IsAvailable => _selectedEngine.IsAvailable;
    public string DeviceInfo => _selectedEngine.DeviceInfo;

    public AutoInferenceEngine()
    {
        _cpuEngine = new CpuInferenceEngine();
        _cudaEngine = new CudaInferenceEngine();
        
        // Default to CUDA if available, otherwise CPU
        _selectedEngine = _cudaEngine.IsAvailable ? _cudaEngine : _cpuEngine;
    }

    public void SetBackend(ProcessingBackend backend)
    {
        _selectedEngine = backend switch
        {
            ProcessingBackend.Cuda => _cudaEngine?.IsAvailable == true ? _cudaEngine : _cpuEngine,
            ProcessingBackend.Cpu => _cpuEngine,
            _ => _cudaEngine?.IsAvailable == true ? _cudaEngine : _cpuEngine
        };
    }

    public async Task<IInferenceSession> CreateSessionAsync(string modelPath, SessionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await _selectedEngine.CreateSessionAsync(modelPath, options, cancellationToken);
    }

    public async Task<HardwareInfo> GetHardwareInfoAsync()
    {
        var info = await _selectedEngine.GetHardwareInfoAsync();
        
        // Also get CPU info
        var cpuInfo = await _cpuEngine.GetHardwareInfoAsync();
        info.CpuName = cpuInfo.CpuName;
        info.CpuCores = cpuInfo.CpuCores;
        info.TotalMemoryBytes = cpuInfo.TotalMemoryBytes;
        
        return info;
    }
}

public static class InferenceEngineFactory
{
    public static IInferenceEngine Create(ProcessingBackend backend)
    {
        return backend switch
        {
            ProcessingBackend.Cuda => new CudaInferenceEngine(),
            ProcessingBackend.Cpu => new CpuInferenceEngine(),
            _ => new AutoInferenceEngine()
        };
    }
}