using AICareerCoach.DAL.Data;
using Microsoft.EntityFrameworkCore;

using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.repository;
using Microsoft.Extensions.Options;

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
            //inject repository and service 
            builder.Services.AddScoped(typeof(IBaserepo<>), typeof(GenericRepo<>));
            builder.Services.AddScoped(typeof(IBaseservice<>), typeof(Genericservice<>));
            //database configuration 
            builder.Services.AddDbContext<AICareerCoachDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("cs")));
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