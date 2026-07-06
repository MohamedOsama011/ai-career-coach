namespace AICareerCoach.BLL.DTOs
{
    public class Generalresponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
    }

    public class GeneralResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
