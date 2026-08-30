namespace Abstract.Data.Exceptions;

/// <summary>
/// The exception that is thrown when a url is invalid or not supported.
/// </summary>
public class InvalidUrlException() : Exception(Message)
{
    private new const string Message = "Unsupported or error link";
}