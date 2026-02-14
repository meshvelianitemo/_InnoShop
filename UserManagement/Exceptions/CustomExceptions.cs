namespace UserManagement.Exceptions
{
    public class CustomException : Exception
    {
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public CustomException(string message, string errorMessage = "INTERNAL_ERROR", int statusCode = 500, Exception innerException = null)
                        : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        //User not found exception (404)    
        public class UserNotFoundException : CustomException
        {
            public UserNotFoundException(string message = "User not found.")
                : base(message, "USER_NOT_FOUND", 404)
            {
            }

        }

        //Email already exists exception (400)
        public class EmailAlreadyExistsException : CustomException
        {
            public EmailAlreadyExistsException(string message = "Email already exists.")
                : base(message, "EMAIL_ALREADY_EXISTS", 400)
            {
            }
        }

        //Invalid credentials exception (401)
        public class InvalidCredentialsException : CustomException
        {
            public InvalidCredentialsException(string message = "Invalid credentials.")
                : base(message, "INVALID_CREDENTIALS", 401)
            {
            }
        }

        // Verification code invalid/expired exception (400)
        public class VerificationCodeException : CustomException
        {
            public VerificationCodeException(string message = "Verification code is invalid or expired.")
                : base(message, "INVALID_VERIFICATION_CODE", 400)
            {
            }
        }

        //Email Sendind Failure Exception (500)
        public class EmailSendingException : CustomException
        {
            public EmailSendingException(string message)
            : base($"Failed to send email: {message}", "EMAIL_SENDING_FAILED", 500)
            {
            }
        }

        // Role not found (500)
        public class RoleNotFoundException : CustomException
        {
            public RoleNotFoundException(string message = "Role not found")
                : base(message, "ROLE_NOT_FOUND", 500)
            {
            }
        }

        // Database operation failed (500)
        public class DatabaseOperationException : CustomException
        {
            public DatabaseOperationException(string message)
                : base($"Database operation failed: {message}", "DATABASE_ERROR", 500)
            {
            }
        }

        // Thrown when HttpClient fails to reach the Product MS
        public class ProductServiceConnectionException : CustomException
        {
            public ProductServiceConnectionException(string message = "Failed to connect to Product Service.")
                : base(message, "PRODUCT_SERVICE_CONNECTION_FAILED", 503)
            {
            }

            public ProductServiceConnectionException(string message, Exception inner)
                : base(message, "PRODUCT_SERVICE_CONNECTION_FAILED", 503, inner)
            {
            }
        }

        // Thrown when Product MS returns an unsuccessful HTTP status
        public class ProductServiceResponseException : CustomException
        {
            public ProductServiceResponseException(int statusCode, string message = "Product Service returned an error.")
                : base(message, "PRODUCT_SERVICE_RESPONSE_ERROR", statusCode)
            {
            }

            public ProductServiceResponseException(int statusCode, string message, Exception inner)
                : base(message, "PRODUCT_SERVICE_RESPONSE_ERROR", statusCode, inner)
            {
            }
        }

        // Thrown when deserialization from Product MS fails
        public class ProductServiceDeserializationException : CustomException
        {
            public ProductServiceDeserializationException(string message = "Failed to deserialize response from Product Service.")
                : base(message, "PRODUCT_SERVICE_DESERIALIZATION_FAILED", 500)
            {
            }

            public ProductServiceDeserializationException(string message, Exception inner)
                : base(message, "PRODUCT_SERVICE_DESERIALIZATION_FAILED", 500, inner)
            {
            }
        }
    }
}
