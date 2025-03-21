
namespace AuthService.Exceptions
{
    public class UserServiceException : Exception
    {
        public UserServiceException(string message) : base(message) { }
        public override string ToString()
        {
            return $"{Message}";
            
        }
    }
}
