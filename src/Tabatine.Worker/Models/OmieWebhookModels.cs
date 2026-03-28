using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tabatine.Worker.Models
{
    /// <summary>
    /// Modelo unificado para receber webhooks da Omie.
    /// Suporta tanto o formato legado (event=string, message=object) 
    /// quanto o Omie Connect 2.0 (topic=string, event=object).
    /// </summary>
    public class OmieWebhookRequest
    {
        // --- Campos comuns ---
        [JsonPropertyName("appKey")]
        public string? AppKey { get; set; }

        [JsonPropertyName("appHash")]
        public string? AppHash { get; set; }

        // --- Formato Legado ---

        /// <summary>
        /// Formato legado: nome do evento como string (ex: "VendaProduto.Novo").
        /// No Connect 2.0, este campo contém um OBJETO (dados do evento).
        /// </summary>
        [JsonPropertyName("event")]
        public JsonElement? Event { get; set; }

        [JsonPropertyName("author")]
        public JsonElement? Author { get; set; }

        [JsonPropertyName("message")]
        public JsonElement? Message { get; set; }

        // --- Formato Omie Connect 2.0 ---

        /// <summary>
        /// Connect 2.0: nome do evento (ex: "VendaProduto.Incluida").
        /// </summary>
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        /// <summary>
        /// Connect 2.0: UUID único do webhook para idempotência.
        /// </summary>
        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        /// <summary>
        /// Connect 2.0: origem do webhook (ex: "omie-connect-2.0").
        /// </summary>
        [JsonPropertyName("origin")]
        public string? Origin { get; set; }

        // --- Helpers ---

        /// <summary>
        /// Detecta se o payload é no formato Omie Connect 2.0.
        /// </summary>
        public bool IsConnect2 => !string.IsNullOrEmpty(Topic);

        /// <summary>
        /// Retorna o nome do evento, independente do formato.
        /// Connect 2.0: campo "topic". Legado: campo "event" como string.
        /// </summary>
        public string? ResolvedEventName
        {
            get
            {
                if (IsConnect2) return Topic;

                // Formato legado: "event" é string
                if (Event.HasValue && Event.Value.ValueKind == JsonValueKind.String)
                    return Event.Value.GetString();

                return null;
            }
        }

        /// <summary>
        /// Retorna o payload de dados do evento como JSON string.
        /// Connect 2.0: campo "event" (objeto). Legado: campo "message".
        /// </summary>
        public string? ResolvedPayload
        {
            get
            {
                if (IsConnect2)
                {
                    // No Connect 2.0, "event" contém os dados do evento como objeto
                    if (Event.HasValue && Event.Value.ValueKind == JsonValueKind.Object)
                        return Event.Value.GetRawText();
                    return null;
                }

                // Formato legado: "message" contém os dados
                if (Message.HasValue && Message.Value.ValueKind != JsonValueKind.Null && Message.Value.ValueKind != JsonValueKind.Undefined)
                    return Message.Value.GetRawText();

                return null;
            }
        }
    }
}
