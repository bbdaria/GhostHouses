using System.Threading.Tasks;
using WebServer.Models.Users;
using WebServer.Services;
using Xunit;

public class UserServiceTests
{
    private UserService CreateService()
    {
        return new UserService();
    }
