namespace PhotoAI.Core.Enums;

public enum MediaType
{
    Unknown = 0,
    Image = 1,
    Video = 2
}

public enum MediaStatus
{
    Pending = 0,
    Processing = 1,
    Indexed = 2,
    Failed = 3,
    Skipped = 4
}

public enum ProcessingBackend
{
    Auto = 0,
    Cpu = 1,
    Cuda = 2
}

public enum IndexJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Paused = 5
}

public enum DuplicateGroupType
{
    Exact = 0,
    NearDuplicate = 1
}
