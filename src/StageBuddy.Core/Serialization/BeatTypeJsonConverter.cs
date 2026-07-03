using System.Text.Json;
using System.Text.Json.Serialization;
using StageBuddy.Core.Models;

namespace StageBuddy.Core.Serialization;

public sealed class BeatTypeJsonConverter() : JsonStringEnumConverter<BeatType>(JsonNamingPolicy.CamelCase);
