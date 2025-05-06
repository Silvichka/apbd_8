namespace Tutorial8.Exceptions;

public class CreatingClientException : Exception
{
    public CreatingClientException() :
        base($"Smth went wrong while creating new client")
    {
    }
}