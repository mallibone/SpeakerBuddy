using System.Text.Json;
using System.Text.Json.Serialization;
using StageBuddy.Core.Models;
using StageBuddy.Core.Sync;

namespace StageBuddy.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(BeatTypeJsonConverter)],
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TalkDocument))]
[JsonSerializable(typeof(SyncEnvelope))]
public sealed partial class TalkJsonContext : JsonSerializerContext;
