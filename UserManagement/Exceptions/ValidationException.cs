namespace UserManagement.Exceptions
{
    public class ValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public ValidationException(Dictionary<string, string[]> errors)
            : base("Validation failed")
        {
            Errors = errors;
        }

        public ValidationException(string fieldName, string message)
            : base("Validation failed")
        {
            Errors = new Dictionary<string, string[]>
            {
                { fieldName, new[] { message } }
            };
        }
    }
}
