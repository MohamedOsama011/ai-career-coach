using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.services;
using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.Services.Interfaces;
using AICareerCoach.BLL.Services.Pdf;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Models;
using AICareerCoach.DAL.repository;
using AICareerCoach.DAL.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;

namespace AICareerCoach.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            QuestPDF.Settings.License = LicenseType.Community;

            // DB Context
            builder.Services.AddDbContext<AICareerCoachDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Identity
            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<AICareerCoachDbContext>()
            .AddDefaultTokenProviders();

            // Repositories & Services
            builder.Services.AddScoped(typeof(IBaserepo<>), typeof(GenericRepo<>));
            builder.Services.AddScoped(typeof(IBaseservice<>), typeof(Genericservice<>));

            builder.Services.AddScoped<IJobRepository, JobRepository>();
            builder.Services.AddScoped<IRoadmapRepository, RoadmapRepository>();

            builder.Services.AddScoped<ICVService, CVService>();
            builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IJobService, JobService>();
            builder.Services.AddScoped<IRoadmapService, RoadmapService>();
            builder.Services.AddScoped<IPdfExtractorService, PdfExtractorService>();
            builder.Services.AddScoped<ILlmService, LlmService>();
            builder.Services.AddScoped<ICvFeedbackService, CvFeedbackService>();
            builder.Services.AddScoped<IPdfReportService, PdfReportService>();

            // Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("AllowAngular");

            app.UseAuthentication();
            app.UseAuthorization();

            try
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AICareerCoachDbContext>();

                if (!context.Jobs.Any())
                    await JobSeeder.SeedAsync(context);

                if (!context.Roadmaps.Any())
                    await RoadmapSeeder.SeedAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SEEDING ERROR: " + ex.Message);
            }

            app.MapControllers();

            app.Run();
        }
    }
}