using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PhotoAI.Core.Inference;
using PhotoAI.Core.Interfaces;

namespace PhotoAI.AI.Vision;

public class Places365SceneClassifier : ISceneClassifier
{
    private readonly IInferenceEngine _engine;
    private IInferenceSession? _session;
    private bool _disposed;

    public string ModelId => "places365-resnet50";
    public string ModelName => "Places365 ResNet50";
    public string Version => "1.0";
    public bool IsLoaded => _session != null;

    private const int InputSize = 224;

    // Places365 categories (top categories, full list is 365)
    private static readonly string[] _categories = new[]
    {
        "airport", "airport_terminal", "amusement_park", "aquarium", "aqueduct", "arch", "archive",
        "arena", "assembly_line", "atrium", "auto_factory", "badlands", "bakery", "balloon_field",
        "barn", "bar", "baseball_field", "basement", "basilica", "basketball_court", "bathroom",
        "beach", "beauty_salon", "bedroom", "berth", "biology_laboratory", "bistro", "boardwalk",
        "boat_deck", "boathouse", "bookstore", "botanical_garden", "bowling_alley", "boxing_ring",
        "bridge", "building_facade", "bullring", "burial_chamber", "bus_station", "cabana", "cafeteria",
        "campsite", "campus", "canal", "car_dealership", "car_interior", "carnival", "castle",
        "cathedral", "cave", "cemetery", "chalet", "chemistry_lab", "church", "church_interior",
        "classroom", "cliff", "clothing_store", "coast", "cockpit", "coffee_shop", "computer_room",
        "conference_room", "construction_site", "control_room", "corn_field", "corridor", "cottage",
        "courthouse", "courtroom", "courtyard", "creek", "dam", "dance_studio", "darkroom", "desert",
        "diner", "dining_room", "dock", "dorm_room", "driveway", "drugstore", "elevator", "entrance_hall",
        "escalator", "factory", "farm", "field", "fire_station", "fishing_pier", "florist", "forest",
        "food_court", "football_field", "forest_path", "formal_garden", "fountain", "garage",
        "gas_station", "general_store", "gift_shop", "glacier", "golf_course", "greenhouse", "gymnasium",
        "hair_salon", "harbor", "highway", "hospital", "hospital_room", "hotel", "hotel_room",
        "ice_cream_parlor", "ice_floe", "ice_shelf", "indoor_pool", "industrial_area", "islet",
        "jacuzzi", "jail", "japanese_garden", "jewelry_shop", "junkyard", "kasbah", "kennel", "kindergarten",
        "kitchen", "laboratory", "lake", "landfill", "laundry_room", "lecture_hall", "library",
        "light_house", "living_room", "lobby", "lock", "manufactured_home", "market", "martial_arts_gym",
        "maze", "meeting_room", "moat", "mosque", "mountain", "mountain_path", "mountain_snowy",
        "movie_theater", "museum", "nursery", "office", "office_building", "oil_field", "oil_rig",
        "operating_room", "orchard", "orchestra_pit", "outdoor", "pagoda", "palace", "park",
        "parking_garage", "parking_lot", "pasture", "patio", "pharmacy", "phone_booth", "physics_lab",
        "picnic_area", "pier", "pizzeria", "playground", "plaza", "pond", "porch", "power_plant",
        "promenade", "pub", "raceway", "rainforest", "reception", "recreation_room", "residential_neighborhood",
        "restaurant", "restaurant_kitchen", "rice_paddy", "river", "rock_arch", "rope_bridge", "ruin",
        "sandbox", "sauna", "school", "science_museum", "shoe_shop", "shop", "shopping_mall", "shower",
        "ski_lodge", "ski_resort", "ski_slope", "sky", "skyscraper", "slum", "snowfield", "soccer_field",
        "stable", "stage", "staircase", "street", "subway", "supermarket", "swamp", "swimming_pool",
        "television_room", "television_studio", "temple", "tent", "theater", "throne_room", "ticket_booth",
        "toll_plaza", "topiary_garden", "tower", "train_interior", "train_station", "tree_farm",
        "tree_house", "tundra", "underwater", "utility_room", "valley", "vegetable_garden", "veranda",
        "veterinarian_office", "viaduct", "village", "vineyard", "volcano", "waiting_room", "warehouse",
        "water_tower", "waterfall", "wedding_chapel", "wet_bar", "wheat_field", "wind_farm", "windmill",
        "wine_cellar", "yard", "yacht", "zoo"
    };

    public Places365SceneClassifier(IInferenceEngine engine)
    {
        _engine = engine;
    }

    public async Task LoadAsync(string modelPath, ProcessingBackend backend = ProcessingBackend.Auto, CancellationToken cancellationToken = default)
    {
        if (_session != null) return;

        var selectedEngine = backend switch
        {
            ProcessingBackend.Cuda => InferenceEngineFactory.Create(ProcessingBackend.Cuda),
            ProcessingBackend.Cpu => InferenceEngineFactory.Create(ProcessingBackend.Cpu),
            _ => _engine
        };

        _session = await selectedEngine.CreateSessionAsync(modelPath, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SceneClassificationResult>> ClassifyAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();
        using var ms = new MemoryStream(imageData);
        return await ClassifyAsync(ms, cancellationToken);
    }

    public async Task<IReadOnlyList<SceneClassificationResult>> ClassifyAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        EnsureLoaded();

        var preprocessedTensor = await PreprocessImageAsync(imageStream, cancellationToken);

        var inputName = _session!.InputNames[0];
        var inputTensor = NamedOnnxValue.CreateFromTensor(
            _session.InputNames[0],
            new DenseTensor<float>(preprocessedTensor, new[] { 1, 3, InputSize, InputSize }));

        var inputs = new[] { inputTensor };
        var result = await _session.RunAsync(inputs, cancellationToken);

        return PostProcess(result);
    }

    public Task<IReadOnlyList<string>> GetSupportedScenesAsync()
    {
        return Task.FromResult((IReadOnlyList<string>)_categories);
    }

    public void Unload()
    {
        _session?.Dispose();
        _session = null;
    }

    private void EnsureLoaded()
    {
        if (_session == null)
            throw new InvalidOperationException("Model not loaded. Call LoadAsync first.");
    }

    private async Task<float[]> PreprocessImageAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageStream, cancellationToken);

        // Resize to 256 then center crop to 224
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(256, 256),
            Mode = ResizeMode.Max
        }));

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(InputSize, InputSize),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        // ImageNet normalization
        var mean = new float[] { 0.485f, 0.456f, 0.406f };
        var std = new float[] { 0.229f, 0.224f, 0.225f };

        var tensor = new float[3 * InputSize * InputSize];
        int idx = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int c = 0; c < 3; c++)
            {
                for (int y = 0; y < InputSize; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < InputSize; x++)
                    {
                        var pixel = row[x];
                        float val = c switch
                        {
                            0 => pixel.R / 255f,
                            1 => pixel.G / 255f,
                            _ => pixel.B / 255f
                        };
                        tensor[idx++] = (val - mean[c]) / std[c];
                    }
                }
            }
        });

        return tensor;
    }

    private IReadOnlyList<SceneClassificationResult> PostProcess(InferenceResult result)
    {
        var results = new List<SceneClassificationResult>();

        var outputs = result.Outputs;
        if (outputs.Count == 0) return results;

        // Get logits and apply softmax
        var logitsTensor = outputs[0].AsTensor<float>();
        var shape = logitsTensor.Dimensions.ToArray();

        if (shape.Length < 2) return results;

        int numClasses = shape[1];
        int numToReturn = Math.Min(numClasses, _categories.Length);

        // Find top-5 classes
        var scores = new List<(int classIdx, float score)>();

        for (int c = 0; c < numClasses; c++)
        {
            float score = 1f / (1f + MathF.Exp(-logitsTensor[0, c]));
            scores.Add((c, score));
        }

        var top5 = scores.OrderByDescending(s => s.score).Take(5);

        foreach (var (classIdx, score) in top5)
        {
            if (classIdx < _categories.Length && score > 0.1f)
            {
                results.Add(new SceneClassificationResult
                {
                    Label = _categories[classIdx],
                    Confidence = score
                });
            }
        }

        return results;
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