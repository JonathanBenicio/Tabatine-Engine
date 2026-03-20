using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models
{
    public interface IOmieMetadata
    {
        string? DAlt { get; set; }
        string? HAlt { get; set; }
    }

    public static class OmieTimestampHelper
    {
        public static DateTime? ParseOmieDateTime(string? dAlt, string? hAlt)
        {
            if (string.IsNullOrWhiteSpace(dAlt)) return null;

            // Omie uses DD/MM/YYYY and HH:MM:SS
            if (DateTime.TryParseExact($"{dAlt} {hAlt ?? "00:00:00"}", "dd/MM/yyyy HH:mm:ss", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.AssumeLocal, out var result))
            {
                return result.ToUniversalTime();
            }

            return null;
        }
    }

    public class OmieRequest<T>
    {
        [JsonPropertyName("call")]
        public string Call { get; set; } = string.Empty;

        [JsonPropertyName("app_key")]
        public string AppKey { get; set; } = string.Empty;

        [JsonPropertyName("app_secret")]
        public string AppSecret { get; set; } = string.Empty;

        [JsonPropertyName("param")]
        public List<T> Param { get; set; } = new();
    }

    public class OmieResponse<T>
    {
        [JsonPropertyName("pagina")]
        [JsonConverter(typeof(OmieFlexibleIntConverter))]
        public int Pagina { get; set; }

        [JsonPropertyName("total_de_paginas")]
        [JsonConverter(typeof(OmieFlexibleIntConverter))]
        public int TotalDePaginas { get; set; }

        [JsonPropertyName("registros")]
        [JsonConverter(typeof(OmieFlexibleIntConverter))]
        public int Registros { get; set; }

        [JsonPropertyName("total_de_registros")]
        [JsonConverter(typeof(OmieFlexibleIntConverter))]
        public int TotalDeRegistros { get; set; }
    }

    public class OmieFlexibleIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (int.TryParse(stringValue, out var intValue))
                {
                    return intValue;
                }
            }

            return 0;
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
