using System.ComponentModel.DataAnnotations;
using System.Net;
using ProductManagement.Models.DTOs;

namespace ProductManagement.Exceptions
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case CustomException appEx:
                    context.Response.StatusCode = appEx.StatusCode;
                    response.StatusCode = appEx.StatusCode;
                    response.Message = appEx.Message;
                    response.ErrorCode = appEx.ErrorMessage;
                    break;

                case ValidationException valEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Validation failed";
                    response.ErrorCode = "VALIDATION_ERROR";
                    response.Errors = valEx.Errors;
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An unexpected error occurred";
                    response.ErrorCode = "INTERNAL_SERVER_ERROR";
                    break;
            }
            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
