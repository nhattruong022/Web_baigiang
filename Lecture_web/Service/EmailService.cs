using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Lecture_web.Service
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipient, string subject, string body);
        Task SendNewAccountNotificationAsync(string recipient, string username, string password, string fullName);
    }
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            var email = _configuration.GetValue<string>("EMAIL_CONFIGURATION:EMAIL");
            var password = _configuration.GetValue<string>("EMAIL_CONFIGURATION:PASSWORD");
            var host = _configuration.GetValue<string>("EMAIL_CONFIGURATION:HOST");
            var port = _configuration.GetValue<int>("EMAIL_CONFIGURATION:PORT");

            var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(email, password)
            };

            var message = new MailMessage(email!, recipient, subject, body)
            {
                IsBodyHtml = true,
            };

            await smtpClient.SendMailAsync(message);
        }

        public async Task SendNewAccountNotificationAsync(string recipient, string username, string password, string fullName)
        {
            var subject = "Thông báo tài khoản mới - Hệ thống Quản lý Bài giảng";
            
            var loginUrl = $"{_configuration.GetValue<string>("APP_URL", "http://localhost:5056")}/Account/ForgotPassword";
            
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 10px; margin-bottom: 20px;'>
                        <h2 style='color: #007bff; margin-bottom: 10px;'>Chào mừng bạn đến với Hệ thống Quản lý Bài giảng!</h2>
                        <p style='color: #6c757d; margin-bottom: 15px;'>Tài khoản của bạn đã được tạo thành công.</p>
                    </div>
                    
                    <div style='background-color: #ffffff; border: 1px solid #dee2e6; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
                        <h3 style='color: #495057; margin-bottom: 15px;'>Thông tin đăng nhập:</h3>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 15px;'>
                            <p style='margin: 5px 0;'><strong>Tên đăng nhập:</strong> <span style='color: #007bff;'>{username}</span></p>
                            <p style='margin: 5px 0;'><strong>Mật khẩu:</strong> <span style='color: #dc3545;'>{password}</span></p>
                            <p style='margin: 5px 0;'><strong>Họ tên:</strong> {fullName}</p>
                        </div>
                    </div>
                    
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <a href='{loginUrl}' style='background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>
                            Đăng nhập ngay
                        </a>
                    </div>
                    
                    <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 15px; margin-bottom: 20px;'>
                        <h4 style='color: #856404; margin-bottom: 10px;'>⚠️ Lưu ý quan trọng:</h4>
                        <ul style='color: #856404; margin: 0; padding-left: 20px;'>
                            <li>Vui lòng đổi mật khẩu sau khi đăng nhập lần đầu</li>
                            <li>Không chia sẻ thông tin đăng nhập với người khác</li>
                            <li>Liên hệ admin nếu gặp vấn đề khi đăng nhập</li>
                        </ul>
                    </div>
                    
                    <div style='text-align: center; color: #6c757d; font-size: 14px;'>
                        <p>Email này được gửi tự động từ hệ thống. Vui lòng không trả lời email này.</p>
                        <p>© 2025 Hệ thống Quản lý Bài giảng. All rights reserved.</p>
                    </div>
                </div>";

            await SendEmailAsync(recipient, subject, body);
        }
    }
}
