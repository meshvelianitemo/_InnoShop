namespace ProductManagement.Exceptions
{
    public class CustomException : Exception
    {
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public CustomException(string message, string errorMessage = "INTERNAL_ERROR", int statusCode = 500, Exception innerException = null)
                        : base(message)
        {
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }
    }

    //Product not found exception (404)    
    public class ProductNotFoundException : CustomException
    {
        public ProductNotFoundException(string message = "Product not found.")
            : base(message, "PRODUCT_NOT_FOUND", 404)
        {
        }
    }
    

    // Product Image not found (500)
    public class ImageNotFoundException : CustomException
    {
        public ImageNotFoundException(string message = "Product image not found")
            : base(message, "IMAGE_NOT_FOUND", 404)
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

    // Forbidden Operation Exception (403)

    public class ForbiddenOperationException : CustomException
    {
        public ForbiddenOperationException(string message = "You are not allowed to perform this action.")
            : base(message, "FORBIDDEN_OPERATION", 403)
        {
        }
    }

    // Category not found exception (404)
    public class CategoryNotFoundException : CustomException
    {
        public CategoryNotFoundException(string message = "Category not found.")
            : base(message, "CATEGORY_NOT_FOUND", 404)
        {
        }
    }

    // Validation Exception (400)
    public class BusinessValidationException : CustomException
    {
        public BusinessValidationException(string message)
            : base(message, "VALIDATION_ERROR", 400)
        {
        }
    }

}
