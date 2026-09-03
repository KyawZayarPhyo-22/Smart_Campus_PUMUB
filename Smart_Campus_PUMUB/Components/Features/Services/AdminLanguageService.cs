namespace Smart_Campus_PUMUB.Components.Features.Services
{
    public class AdminLanguageService
    {
        public string CurrentLanguage { get; private set; } = "en";
        public event Action? OnLanguageChanged;

        public bool IsMyanmar => string.Equals(CurrentLanguage, "my", StringComparison.OrdinalIgnoreCase) || 
                                 string.Equals(CurrentLanguage, "mm", StringComparison.OrdinalIgnoreCase);

        public void SetLanguage(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) return;

            var normalizedLang = (string.Equals(lang, "mm", StringComparison.OrdinalIgnoreCase) || 
                                  string.Equals(lang, "my", StringComparison.OrdinalIgnoreCase)) ? "my" : "en";

            if (CurrentLanguage != normalizedLang)
            {
                CurrentLanguage = normalizedLang;
                OnLanguageChanged?.Invoke();
            }
        }

        public void SetLanguage(bool isMm)
        {
            SetLanguage(isMm ? "my" : "en");
        }
    }
}
