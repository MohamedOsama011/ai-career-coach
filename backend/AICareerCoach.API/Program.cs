using AICareerCoach.BLL.services.cv;
using AICareerCoach.BLL.services.FileStorage;
using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.repository;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<AICareerCoachDbContext>(options =>options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")));

            //inject repository and service 
            builder.Services.AddScoped(typeof(IBaserepo<>), typeof(GenericRepo<>));
            builder.Services.AddScoped(typeof(IBaseservice<>), typeof(Genericservice<>));
            builder.Services.AddScoped<ICVService,CVService>();
            builder.Services.AddScoped<IFileStorageService,LocalFileStorageService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}