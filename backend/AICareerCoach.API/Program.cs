using AICareerCoach.API.Controllers;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.BLL.services;
using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.Services.AI;
using AICareerCoach.BLL.Services.External;
using AICareerCoach.BLL.Services.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Models;
using AICareerCoach.DAL.repository;
using AICareerCoach.DAL.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AICareerCoach.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AICareerCoachDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

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

            builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
            builder.Services.AddScoped<ILlmExplanationService, LlmExplanationService>();
            builder.Services.AddScoped<IJobRecommendationService, JobRecommendationService>();
            builder.Services.AddScoped<IRoadmapTemplateStore, RoadmapTemplateStore>();
            builder.Services.AddScoped<IRoadmapLlmService, RoadmapLlmService>();
            builder.Services.AddScoped<IInterviewLlmService, InterviewLlmService>();
            builder.Services.AddScoped<IInterviewService, InterviewService>();
            builder.Services.AddScoped<IUserRoadmapService, UserRoadmapService>();
            builder.Services.AddScoped<Iusersubscription, UsersubscriptionService>();


            builder.Services.AddScoped<ISubsription,SubscriptionService>();
            builder.Services.AddHttpClient<Ifawaterak,FawaterakService>();
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient<IFawaterakTokenService, FawaterakTokenService>();


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

            // Fail-closed default: every endpoint requires an authenticated user
            // unless it explicitly opts out via [AllowAnonymous]. Individual
            // controllers may still add role requirements on top of this.
            //builder.Services.AddAuthorization(options =>
            //{
            //    options.FallbackPolicy = new AuthorizationPolicyBuilder()
            //        .RequireAuthenticatedUser()
            //        .Build();
            //});

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });
    //        builder.Services.AddSwaggerGen(c =>
    //        {
    //            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    //            {
    //                Name = "Authorization",
    //                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
    //                Scheme = "bearer",
    //                BearerFormat = "JWT",
    //                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    //                Description = "Enter your JWT token"
    //            });

    //            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    //{
    //    {
    //        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    //        {
    //            Reference = new Microsoft.OpenApi.Models.OpenApiReference
    //            {
    //                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        Array.Empty<string>()
    //    }
    //});
    //        });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

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

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AICareerCoachDbContext>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await RoleSeeder.SeedAsync(roleManager);
                //await JobSeeder.SeedAsync(context);
                await RoadmapSeeder.SeedAsync(context);
            }

            app.MapControllers();

            app.Run();
        }
    }
}
