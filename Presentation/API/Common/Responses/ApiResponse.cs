using System.Net;

namespace Presentation.API.Common.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public ApiResponse(T data)
        {
            Success = true;
            StatusCode = HttpStatusCode.OK;
            Message = "Operação realizada com sucesso.";
            Data = data;
        }

        public ApiResponse(HttpStatusCode statusCode, string message)
        {
            Success = false;
            StatusCode = statusCode;
            Message = message;
            Data = default;
        }
    }
}