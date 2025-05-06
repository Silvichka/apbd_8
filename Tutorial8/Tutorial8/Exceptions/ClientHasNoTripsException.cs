namespace Tutorial8.Exceptions
{
    public class ClientHasNoTripsException : Exception
    {
        public ClientHasNoTripsException(int clientId) :
            base($"Client with ID #{clientId} had not assigned trips yet.")
        {
        }
    }   
}