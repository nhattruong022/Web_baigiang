using Microsoft.EntityFrameworkCore;
using Lecture_web.Models;

namespace Lecture_web
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaiKhoanModels> TaiKhoan { get; set; }
    }


} 