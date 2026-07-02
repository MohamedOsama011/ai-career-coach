namespace AICareerCoach.BLL.DTOs.Interview
{
    /// <summary>
    /// One SSE event in the streamed next-question response (Phase E).
    /// Wire format: each event is a JSON object serialized as
    /// `data: {json}\n\n`. Final marker is `{ "type": "done" }`.
    /// </summary>
    public class StreamTokenDto
    {
        public string Type { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? Code { get; set; }

        public string? Message { get; set; }
    }
}
