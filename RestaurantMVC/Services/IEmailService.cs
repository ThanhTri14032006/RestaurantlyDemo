using System.Threading.Tasks;

namespace RestaurantMVC.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string to, string subject, string htmlBody, string? plainText = null);
    }
}