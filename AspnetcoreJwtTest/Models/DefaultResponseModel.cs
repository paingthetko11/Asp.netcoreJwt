namespace AspnetcoreJwtTest.Models
{
    public class DefaultResponseModel
    {
        public required bool Success { get; set; }
        public required int Code { get; set; }
        public dynamic? Message { get; set; }
        public dynamic? Data { get; set; }
    }
}
