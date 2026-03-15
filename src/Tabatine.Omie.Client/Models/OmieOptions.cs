namespace Tabatine.Omie.Client.Models
{
    public class OmieOptions
    {
        public string AppKey { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://app.omie.com.br/api/v1/";
    }
}
