namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class CreatePaymentResponseDto
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? UserSubscriptionId { get; set; }
    }
}
