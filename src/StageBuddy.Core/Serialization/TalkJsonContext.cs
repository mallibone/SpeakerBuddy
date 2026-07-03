using System.Text.Json;
using System.Text.Json.Serialization;
using StageBuddy.Core.Models;

namespace StageBuddy.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(BeatTypeJsonConverter)],
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TalkDocument))]
public sealed partial class TalkJsonContext : JsonSerializerContext;
