using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;


namespace AICareerCoach.DAL.Seed
{
    /// <summary>
    /// Fallback seeder — only runs if Adzuna sync fails on first startup.
    /// Called by JobSyncHostedService, NOT by Program.cs anymore.
    /// Provides 20 hardcoded Egypt-focused .NET/Angular jobs so the app is
    /// demoable offline (no Adzuna credentials required).
    /// </summary>
    public static class JobSeeder
    {
        public static async Task SeedAsync(AICareerCoachDbContext context)
        {
            if (await context.Jobs.AnyAsync()) return;

            var jobs = new List<Job>
            {
                new() {
                    Title = "Backend .NET Developer",
                    Company = "Vodafone Egypt",
                    Description = "Build and maintain scalable APIs using .NET 8 and SQL Server.",
                    RequiredSkills = """["C#", ".NET 8", "SQL Server", "REST APIs", "Entity Framework"]""",
                    Location = "Cairo, Egypt",
                    Salary = 25000,
                    PostedAt = DateTime.UtcNow.AddDays(-2),
                    CompanyLogoUrl = "https://img.logo.dev/vodafone.com.eg?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Full Stack Developer",
                    Company = "Banque Misr",
                    Description = "Develop Angular frontend with .NET backend for banking systems.",
                    RequiredSkills = """["Angular", "TypeScript", "C#", ".NET", "SQL Server"]""",
                    Location = "Cairo, Egypt",
                    Salary = 30000,
                    PostedAt = DateTime.UtcNow.AddDays(-5),
                    CompanyLogoUrl = "https://img.logo.dev/banquemisr.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Junior Backend Developer",
                    Company = "Instabug",
                    Description = "Work on API development and microservices architecture.",
                    RequiredSkills = """["C#", "ASP.NET Core", "Docker", "PostgreSQL"]""",
                    Location = "Cairo, Egypt (Hybrid)",
                    Salary = 18000,
                    PostedAt = DateTime.UtcNow.AddDays(-1),
                    CompanyLogoUrl = "https://img.logo.dev/instabug.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Angular Frontend Developer",
                    Company = "Breadfast",
                    Description = "Build responsive web apps using Angular and RxJS.",
                    RequiredSkills = """["Angular", "TypeScript", "RxJS", "SCSS", "REST APIs"]""",
                    Location = "Cairo, Egypt",
                    Salary = 22000,
                    PostedAt = DateTime.UtcNow.AddDays(-3),
                    CompanyLogoUrl = "https://img.logo.dev/breadfast.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Software Engineer",
                    Company = "Amazon Egypt",
                    Description = "Design distributed systems and services at scale.",
                    RequiredSkills = """["C#", "AWS", "Microservices", "System Design", "SQL"]""",
                    Location = "Cairo, Egypt",
                    Salary = 55000,
                    PostedAt = DateTime.UtcNow.AddDays(-7),
                    CompanyLogoUrl = "https://img.logo.dev/amazon.eg?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Mid-Level .NET Developer",
                    Company = "Raya Corporation",
                    Description = "Maintain and enhance enterprise ERP solutions.",
                    RequiredSkills = """["C#", ".NET", "WPF", "SQL Server", "LINQ"]""",
                    Location = "Giza, Egypt",
                    Salary = 20000,
                    PostedAt = DateTime.UtcNow.AddDays(-4),
                    CompanyLogoUrl = "https://img.logo.dev/rayacorp.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "React Frontend Developer",
                    Company = "Paymob",
                    Description = "Build payment UIs and dashboards using React.",
                    RequiredSkills = """["React", "JavaScript", "TypeScript", "TailwindCSS"]""",
                    Location = "Cairo, Egypt (Remote)",
                    Salary = 28000,
                    PostedAt = DateTime.UtcNow.AddDays(-6),
                    CompanyLogoUrl = "https://img.logo.dev/paymob.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "DevOps Engineer",
                    Company = "Fawry",
                    Description = "Manage CI/CD pipelines, Docker, and Azure infrastructure.",
                    RequiredSkills = """["Docker", "Azure", "GitHub Actions", "Linux", "Kubernetes"]""",
                    Location = "Cairo, Egypt",
                    Salary = 35000,
                    PostedAt = DateTime.UtcNow.AddDays(-10),
                    CompanyLogoUrl = "https://img.logo.dev/fawry.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Data Engineer",
                    Company = "Orange Egypt",
                    Description = "Build data pipelines and ETL processes.",
                    RequiredSkills = """["Python", "SQL", "Apache Spark", "Azure Data Factory"]""",
                    Location = "Cairo, Egypt",
                    Salary = 32000,
                    PostedAt = DateTime.UtcNow.AddDays(-8),
                    CompanyLogoUrl = "https://img.logo.dev/orange.com.eg?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Mobile Developer (Flutter)",
                    Company = "Halan",
                    Description = "Build cross-platform mobile apps using Flutter.",
                    RequiredSkills = """["Flutter", "Dart", "REST APIs", "Firebase"]""",
                    Location = "Cairo, Egypt",
                    Salary = 24000,
                    PostedAt = DateTime.UtcNow.AddDays(-9),
                    CompanyLogoUrl = "https://img.logo.dev/halan.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Senior .NET Software Engineer",
                    Company = "Valu",
                    Description = "Design and optimize high-throughput fintech APIs using .NET 8 and microservices.",
                    RequiredSkills = """["C#", ".NET Core", "Redis", "RabbitMQ", "Microservices", "SQL Server"]""",
                    Location = "Cairo, Egypt",
                    Salary = 48000,
                    PostedAt = DateTime.UtcNow.AddDays(-1),
                    CompanyLogoUrl = "https://img.logo.dev/valu.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Frontend Developer (Angular)",
                    Company = "Andela Egypt",
                    Description = "Build state-of-the-art dashboards and web interfaces for international clients.",
                    RequiredSkills = """["Angular", "TypeScript", "State Management", "RxJS", "SASS"]""",
                    Location = "Cairo, Egypt (Remote)",
                    Salary = 40000,
                    PostedAt = DateTime.UtcNow.AddDays(-3),
                    CompanyLogoUrl = "https://img.logo.dev/andela.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "AI Engineer / Prompt Engineer",
                    Company = "RDI Egypt",
                    Description = "Integrate LLMs and build conversational AI agents using Semantic Kernel or LangChain.",
                    RequiredSkills = """["Python", "C#", "Semantic Kernel", "OpenAI API", "Prompt Engineering"]""",
                    Location = "Giza, Egypt (Hybrid)",
                    Salary = 35000,
                    PostedAt = DateTime.UtcNow.AddDays(-2),
                    CompanyLogoUrl = "https://img.logo.dev/rdi.net?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Junior Full Stack Developer",
                    Company = "Sarmady (Vodafone Digital)",
                    Description = "Assist in developing web solutions using .NET backend and Angular frontend ecosystems.",
                    RequiredSkills = """["C#", "ASP.NET Core", "Angular", "SQL Server", "Git"]""",
                    Location = "Cairo, Egypt",
                    Salary = 16000,
                    PostedAt = DateTime.UtcNow.AddDays(-4),
                    CompanyLogoUrl = "https://img.logo.dev/sarmady.net?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Cybersecurity Specialist",
                    Company = "CIB Egypt",
                    Description = "Conduct secure code reviews, penetration testing, and defend against OWASP Top 10 vulnerabilities.",
                    RequiredSkills = """["Web Security", "Penetration Testing", "OWASP", "SQL Injection defense", "Network Security"]""",
                    Location = "Giza, Egypt",
                    Salary = 38000,
                    PostedAt = DateTime.UtcNow.AddDays(-12),
                    CompanyLogoUrl = "https://img.logo.dev/cibeg.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Mid-Level Angular Engineer",
                    Company = "BBI Consultancy",
                    Description = "Develop and maintain robust enterprise business intelligence web portals.",
                    RequiredSkills = """["Angular", "TypeScript", "Bootstrap", "RESTful APIs", "RxJS"]""",
                    Location = "Cairo, Egypt",
                    Salary = 23000,
                    PostedAt = DateTime.UtcNow.AddDays(-6),
                    CompanyLogoUrl = "https://img.logo.dev/bbi.com.eg?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Cloud Backend Developer",
                    Company = "IBM Egypt",
                    Description = "Build cloud-native backend services using .NET, Docker, and IBM Cloud.",
                    RequiredSkills = """["C#", "ASP.NET Core", "Docker", "Kubernetes", "Cloud Computing"]""",
                    Location = "Cairo, Egypt (Hybrid)",
                    Salary = 50000,
                    PostedAt = DateTime.UtcNow.AddDays(-5),
                    CompanyLogoUrl = "https://img.logo.dev/ibm.com?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "UI/UX Developer",
                    Company = "Wuzzuf",
                    Description = "Bridge the gap between design and implementation, translating Figma into Angular components.",
                    RequiredSkills = """["HTML5", "CSS3", "SCSS", "Angular", "Figma", "UI Design"]""",
                    Location = "Cairo, Egypt",
                    Salary = 21000,
                    PostedAt = DateTime.UtcNow.AddDays(-8),
                    CompanyLogoUrl = "https://img.logo.dev/wuzzuf.net?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "SQL Server Database Administrator",
                    Company = "Telecom Egypt (WE)",
                    Description = "Manage, optimize, and secure massive telecom databases and query performance.",
                    RequiredSkills = """["SQL Server", "T-SQL", "Database Tuning", "Backup & Recovery", "Indexing"]""",
                    Location = "Cairo, Egypt",
                    Salary = 27000,
                    PostedAt = DateTime.UtcNow.AddDays(-11),
                    CompanyLogoUrl = "https://img.logo.dev/telecomegypt.com.eg?token=pk_Y-0y45UETNSPK67lM2VJdg"
                },
                new() {
                    Title = "Software Development Engineer in Test (SDET)",
                    Company = "Etisalat Egypt",
                    Description = "Build automated testing frameworks for APIs and web applications using xUnit and Selenium.",
                    RequiredSkills = """["C#", "xUnit", "Selenium", "API Testing", "Automation", "CI/CD"]""",
                    Location = "Cairo, Egypt (Hybrid)",
                    Salary = 29000,
                    PostedAt = DateTime.UtcNow.AddDays(-7),
                    CompanyLogoUrl = "https://img.logo.dev/etisalat.eg?token=pk_Y-0y45UETNSPK67lM2VJdg"
                }
            };

            context.Jobs.AddRange(jobs);
            await context.SaveChangesAsync();

        }

    }
}
