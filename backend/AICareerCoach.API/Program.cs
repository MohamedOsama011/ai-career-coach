using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.BLL.Interfaces.External;
using AICareerCoach.BLL.Services;
using AICareerCoach.BLL.Services.AI;
using AICareerCoach.BLL.Services.External;
using AICareerCoach.BLL.Services.Interfaces;
using AICareerCoach.BLL.Services.Pdf;
using QuestPDF.Infrastructure;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
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
            builder.Services.AddScoped<IUserService, UserService>();
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
            builder.Services.AddScoped<IAgentToolExecutor, AgentToolExecutor>();
            builder.Services.AddScoped<IChatAssistantService, ChatAssistantService>();

            builder.Services.AddHttpClient<IJobProvider, JoobleJobProvider>();
            builder.Services.AddScoped<ISkillExtractionService, SkillExtractionService>();
            builder.Services.AddScoped<IJobSyncService, JobSyncService>();
            builder.Services.AddHostedService<JobSyncHostedService>();
            builder.Services.AddScoped<IPdfReportService, PdfReportService>();

            builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
            builder.Services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
            builder.Services.AddScoped<ISubscriptionGateService, SubscriptionGateService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IAdminSubscriptionService, AdminSubscriptionService>();
builder.Services.AddScoped<IAdminRoadmapService, AdminRoadmapService>();
            builder.Services.AddScoped<IAdminInterviewService, AdminInterviewService>();
            builder.Services.AddHttpClient<IFawaterakService, FawaterakService>();
            builder.Services.AddHttpClient<IFawaterakTokenService, FawaterakTokenService>();
            builder.Services.AddMemoryCache();

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
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy =>
                    policy.WithOrigins("http://localhost:4200", "https://ai-career-coach-nc2y.vercel.app")
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            QuestPDF.Settings.License = LicenseType.Community;

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
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                // Ensure LimitsJson column exists (migration was hand-written but never applied)
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Subscriptions') AND name = 'LimitsJson')
                    BEGIN
                        ALTER TABLE Subscriptions ADD LimitsJson nvarchar(max) NULL;
                    END");

                await RoleSeeder.SeedAsync(roleManager);
                await AdminSeeder.SeedAsync(userManager, logger);
                //await JobSeeder.SeedAsync(context);
                await RoadmapSeeder.SeedAsync(context);
                await SubscriptionSeeder.SeedAsync(context);

                // Generate embeddings for roadmap templates if missing (one-time)
                var templateCount = await context.Roadmaps.CountAsync();
                var embeddingCount = await context.RoadmapTemplateEmbeddings.CountAsync();
                if (templateCount > 0 && embeddingCount == 0)
                {
                    logger.LogInformation("Generating embeddings for {Count} roadmap templates...", templateCount);
                    var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                    var templates = await context.Roadmaps.Include(r => r.Steps).ToListAsync();
                    foreach (var template in templates)
                    {
                        var stepsText = string.Join("\n    ", template.Steps.OrderBy(s => s.OrderIndex).Select(s =>
                            $"- {s.Title}: {s.Description} [Level: {s.Level}]"
                        ));
                        var combinedText = $"Track: {template.Track}\nTitle: {template.Title}\nDescription: {template.Description}\n\nSteps:\n    {stepsText}";
                        var embeddingVector = await embeddingService.GenerateEmbeddingAsync(combinedText);
                        context.RoadmapTemplateEmbeddings.Add(new RoadmapTemplateEmbedding
                        {
                            RoadmapId = template.Id,
                            Embedding = embeddingVector,
                            ComputedAt = DateTime.UtcNow
                        });
                    }
                    await context.SaveChangesAsync();
                    logger.LogInformation("Roadmap template embeddings generated successfully.");
                }
            }

            app.MapControllers();

            app.Run();
        }
    }
}
