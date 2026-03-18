using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models
{
    public class OmieRequest<T>
    {
        [JsonPropertyName("app_key")]
        public string AppKey { get; set; } = string.Empty;

        [JsonPropertyName("app_secret")]
        public string AppSecret { get; set; } = string.Empty;

        [JsonPropertyName("call")]
        public string Call { get; set; } = string.Empty;

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
