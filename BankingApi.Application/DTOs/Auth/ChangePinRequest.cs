namespace BankingApi.Application.DTOs.Auth
{
    public class ChangePinRequest
    {
        public string CurrentPin { get; set; } = string.Empty;
        public string NewPin { get; set; } = string.Empty;
    }
}
