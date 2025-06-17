using System.Text.RegularExpressions;

namespace Lecture_web.Service
{
    public static class StringHelper
    {
        public static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Xóa khoảng trắng đầu/cuối, thay nhiều khoảng trắng liên tiếp bằng 1 khoảng trắng
            return Regex.Replace(input.Trim(), @"\s+", " ");
        }
    }
} 