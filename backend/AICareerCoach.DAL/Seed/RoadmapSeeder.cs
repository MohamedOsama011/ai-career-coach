using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AICareerCoach.DAL.Seed
{
    public static class RoadmapSeeder
    {
        public static async Task SeedAsync(AICareerCoachDbContext context)
        {
            var existingTracks = await context.Roadmaps.Select(r => r.Track).ToListAsync();
            var allTracks = new[] { "Backend", "Frontend", "Full Stack", "ML", "DevOps", "Data Analyst" };
            var missingTracks = allTracks.Except(existingTracks).ToList();

            if (!missingTracks.Any()) return;

            var templates = new List<Roadmap>
            {
                new()
                {
                    Track = "Backend",
                    Title = "Backend .NET Developer",
                    Description = "Complete roadmap to become a professional .NET Backend Developer",
                    OrderIndex = 1,
                    Steps = new List<RoadmapStep>
                    {
                        new() { Title = "C# Fundamentals", Description = "Master C# syntax, OOP, LINQ, and async/await", Level = "Beginner", OrderIndex = 1, Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/dotnet/csharp", "https://www.pluralsight.com" }) },
                        new() { Title = "ASP.NET Core Web API", Description = "Build RESTful APIs, middleware, routing, and filters", Level = "Intermediate", OrderIndex = 2, Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/aspnet/core" }) },
                        new() { Title = "Entity Framework Core", Description = "ORM, migrations, relationships, performance tuning", Level = "Intermediate", OrderIndex = 3, Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/ef/core" }) },
                        new() { Title = "SQL Server & T-SQL", Description = "Queries, stored procedures, indexes, optimization", Level = "Intermediate", OrderIndex = 4, Resources = JsonSerializer.Serialize(new List<string> { "https://www.sqlservertutorial.net" }) },
                        new() { Title = "Design Patterns & SOLID", Description = "Repository, Unit of Work, DI, Clean Architecture", Level = "Intermediate", OrderIndex = 5, Resources = JsonSerializer.Serialize(new List<string> { "https://refactoring.guru/design-patterns" }) },
                        new() { Title = "Auth & Security", Description = "JWT, OAuth2, Identity, HTTPS, CORS", Level = "Advanced", OrderIndex = 6, Resources = JsonSerializer.Serialize(new List<string> { "https://jwt.io" }) },
                        new() { Title = "Docker & Deployment", Description = "Containerize APIs, deploy to Azure/Railway", Level = "Advanced", OrderIndex = 7, Resources = JsonSerializer.Serialize(new List<string> { "https://docs.docker.com" }) }
                    }
                },
                new()
                {
                    Track = "Frontend",
                    Title = "Frontend Developer",
                    Description = "Complete roadmap to become a professional Frontend Developer",
                    OrderIndex = 2,
                    Steps = new List<RoadmapStep>
                    {
                        new() { Title = "HTML & CSS Fundamentals", Description = "Semantic HTML, CSS layout, Flexbox, Grid, responsive design", Level = "Beginner", OrderIndex = 1, Resources = JsonSerializer.Serialize(new List<string> { "https://developer.mozilla.org", "https://flexboxfroggy.com" }) },
                        new() { Title = "JavaScript Core", Description = "ES6+, DOM manipulation, promises, async/await, modules", Level = "Beginner", OrderIndex = 2, Resources = JsonSerializer.Serialize(new List<string> { "https://javascript.info", "https://developer.mozilla.org" }) },
                        new() { Title = "TypeScript", Description = "Types, interfaces, generics, decorators, advanced types", Level = "Intermediate", OrderIndex = 3, Resources = JsonSerializer.Serialize(new List<string> { "https://www.typescriptlang.org/docs" }) },
                        new() { Title = "Angular Framework", Description = "Components, services, routing, forms, signals, NgRx", Level = "Intermediate", OrderIndex = 4, Resources = JsonSerializer.Serialize(new List<string> { "https://angular.dev" }) },
                        new() { Title = "State Management & RxJS", Description = "Observables, operators, subjects, state containers", Level = "Intermediate", OrderIndex = 5, Resources = JsonSerializer.Serialize(new List<string> { "https://rxjs.dev", "https://ngrx.io" }) },
                        new() { Title = "Testing & Performance", Description = "Unit tests, E2E, lazy loading, bundle optimization, SSR", Level = "Advanced", OrderIndex = 6, Resources = JsonSerializer.Serialize(new List<string> { "https://jestjs.io", "https://web.dev" }) },
                        new() { Title = "CI/CD & Deployment", Description = "Netlify, Vercel, Docker, GitHub Actions, Nginx", Level = "Advanced", OrderIndex = 7, Resources = JsonSerializer.Serialize(new List<string> { "https://docs.github.com/actions", "https://vercel.com/docs" }) }
                    }
                },
                new()
                {
                    Track = "Full Stack",
                    Title = "Full Stack Developer",
                    Description = "Complete roadmap to become a Full Stack Developer with Angular + .NET",
                    OrderIndex = 3,
                    Steps = new List<RoadmapStep>
                    {
                        new() { Title = "C# & .NET Foundations", Description = "C# syntax, OOP, LINQ, async programming basics", Level = "Beginner", OrderIndex = 1, Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/dotnet/csharp" }) },
                        new() { Title = "Frontend Core", Description = "HTML, CSS, JavaScript, TypeScript fundamentals", Level = "Beginner", OrderIndex = 2, Resources = JsonSerializer.Serialize(new List<string> { "https://developer.mozilla.org", "https://javascript.info" }) },
                        new() { Title = "Angular Deep Dive", Description = "Components, services, routing, reactive forms, HTTP client", Level = "Intermediate", OrderIndex = 3, Resources = JsonSerializer.Serialize(new List<string> { "https://angular.dev" }) },
                        new() { Title = "ASP.NET Core & EF Core", Description = "REST APIs, middleware, EF migrations, relationships", Level = "Intermediate", OrderIndex = 4, Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/aspnet/core", "https://learn.microsoft.com/ef/core" }) },
                        new() { Title = "Auth & Full Stack Integration", Description = "JWT auth, CORS, deploy Angular + API together", Level = "Intermediate", OrderIndex = 5, Resources = JsonSerializer.Serialize(new List<string> { "https://jwt.io" }) },
                        new() { Title = "Cloud & DevOps", Description = "Docker, Azure/AWS, CI/CD pipelines, monitoring", Level = "Advanced", OrderIndex = 6, Resources = JsonSerializer.Serialize(new List<string> { "https://docs.docker.com", "https://azure.microsoft.com" }) },
                        new() { Title = "System Design & Architecture", Description = "Microservices, message queues, caching, scalability", Level = "Advanced", OrderIndex = 7, Resources = JsonSerializer.Serialize(new List<string> { "https://github.com/donnemartin/system-design-primer" }) }
                    }
                },
                new()
                {
                    Track = "ML",
                    Title = "Machine Learning Engineer",
                    Description = "Complete roadmap to become a Machine Learning Engineer",
                    OrderIndex = 4,
                    Steps = new List<RoadmapStep>
                    {
                        new() { Title = "Python & Mathematics", Description = "Python, NumPy, Pandas, linear algebra, calculus, statistics", Level = "Beginner", OrderIndex = 1, Resources = JsonSerializer.Serialize(new List<string> { "https://www.python.org", "https://www.khanacademy.org/math" }) },
                        new() { Title = "Data Analysis & Visualization", Description = "Matplotlib, Seaborn, Plotly, data cleaning, EDA", Level = "Beginner", OrderIndex = 2, Resources = JsonSerializer.Serialize(new List<string> { "https://pandas.pydata.org", "https://matplotlib.org" }) },
                        new() { Title = "Classic ML Algorithms", Description = "Regression, classification, clustering, decision trees, SVM", Level = "Intermediate", OrderIndex = 3, Resources = JsonSerializer.Serialize(new List<string> { "https://scikit-learn.org", "https://www.coursera.org/learn/machine-learning" }) },
                        new() { Title = "Deep Learning", Description = "Neural networks, CNNs, RNNs, TensorFlow, PyTorch", Level = "Intermediate", OrderIndex = 4, Resources = JsonSerializer.Serialize(new List<string> { "https://www.tensorflow.org", "https://pytorch.org" }) },
                        new() { Title = "NLP & Transformers", Description = "Word embeddings, attention, BERT, GPT, Hugging Face", Level = "Advanced", OrderIndex = 5, Resources = JsonSerializer.Serialize(new List<string> { "https://huggingface.co", "https://arxiv.org/abs/1706.03762" }) },
                        new() { Title = "MLOps & Deployment", Description = "Model serving, Docker, MLflow, CI/CD for ML, monitoring", Level = "Advanced", OrderIndex = 6, Resources = JsonSerializer.Serialize(new List<string> { "https://mlflow.org", "https://www.kubeflow.org" }) },
                        new() { Title = "Advanced Topics", Description = "Reinforcement learning, GANs, model optimization, edge AI", Level = "Advanced", OrderIndex = 7, Resources = JsonSerializer.Serialize(new List<string> { "https://github.com/ossu/computer-science" }) }
                    }
                },
                new()
                {
                    Track = "DevOps",
                    Title = "DevOps Engineer",
                    Description = "Complete roadmap to become a DevOps Engineer",
                    OrderIndex = 5,
                    Steps = new List<RoadmapStep>
                    {
                        new() { Title = "Linux & Scripting", Description = "Linux CLI, bash scripting, file systems, process management", Level = "Beginner", OrderIndex = 1, Resources = JsonSerializer.Serialize(new List<string> { "https://linuxjourney.com", "https://www.gnu.org/software/bash" }) },
                        new() { Title = "Version Control & Git", Description = "Git branching, rebasing, hooks, GitHub workflows", Level = "Beginner", OrderIndex = 2, Resources = JsonSerializer.Serialize(new List<string> { "https://git-scm.com", "https://docs.github.com" }) },
                        new() { Title = "Containerization & Docker", Description = "Dockerfile, compose, multi-stage builds, registries", Level = "Intermediate", OrderIndex = 3, Resources = JsonSerializer.Serialize(new List<string> { "https://docs.docker.com" }) },
                        new() { Title = "Orchestration & Kubernetes", Description = "Pods, deployments, services, ingress, Helm, Kustomize", Level = "Intermediate", OrderIndex = 4, Resources = JsonSerializer.Serialize(new List<string> { "https://kubernetes.io", "https://helm.sh" }) },
                        new() { Title = "CI/CD Pipelines", Description = "GitHub Actions, Jenkins, GitLab CI, ArgoCD", Level = "Intermediate", OrderIndex = 5, Resources = JsonSerializer.Serialize(new List<string> { "https://docs.github.com/actions", "https://www.jenkins.io" }) },
                        new() { Title = "Infrastructure as Code", Description = "Terraform, Ansible, Pulumi, cloud provisioning", Level = "Advanced", OrderIndex = 6, Resources = JsonSerializer.Serialize(new List<string> { "https://www.terraform.io", "https://www.ansible.com" }) },
                        new() { Title = "Monitoring & Observability", Description = "Prometheus, Grafana, ELK stack, OpenTelemetry, alerting", Level = "Advanced", OrderIndex = 7, Resources = JsonSerializer.Serialize(new List<string> { "https://prometheus.io", "https://grafana.com" }) }
                    }
                },
                new()
                {
                    Track = "Data Analyst",
                    Title = "Data Analyst",
                    Description = "Complete roadmap to become a Data Analyst",
                    OrderIndex = 6,
                    Steps = new List<RoadmapStep>
                    {
                        new() { Title = "Excel & Spreadsheets", Description = "Formulas, pivot tables, VLOOKUP, charts, macros", Level = "Beginner", OrderIndex = 1, Resources = JsonSerializer.Serialize(new List<string> { "https://support.microsoft.com/excel" }) },
                        new() { Title = "SQL for Data Analysis", Description = "SELECT, JOINs, aggregations, window functions, CTEs", Level = "Beginner", OrderIndex = 2, Resources = JsonSerializer.Serialize(new List<string> { "https://www.sqlservertutorial.net", "https://mode.com/sql-tutorial" }) },
                        new() { Title = "Python for Data Analysis", Description = "Pandas, NumPy, data cleaning, transformation, datetime", Level = "Intermediate", OrderIndex = 3, Resources = JsonSerializer.Serialize(new List<string> { "https://pandas.pydata.org", "https://www.python.org" }) },
                        new() { Title = "Data Visualization", Description = "Matplotlib, Seaborn, Tableau, Power BI, storytelling", Level = "Intermediate", OrderIndex = 4, Resources = JsonSerializer.Serialize(new List<string> { "https://www.tableau.com", "https://powerbi.microsoft.com" }) },
                        new() { Title = "Statistical Analysis", Description = "Hypothesis testing, regression, A/B testing, probability", Level = "Intermediate", OrderIndex = 5, Resources = JsonSerializer.Serialize(new List<string> { "https://www.khanacademy.org/math/statistics-probability" }) },
                        new() { Title = "Data Pipelines & Automation", Description = "ETL, Airflow, dbt, scheduled reporting, data warehousing", Level = "Advanced", OrderIndex = 6, Resources = JsonSerializer.Serialize(new List<string> { "https://airflow.apache.org", "https://docs.getdbt.com" }) },
                        new() { Title = "Business Intelligence & Strategy", Description = "KPI definition, dashboard design, stakeholder communication", Level = "Advanced", OrderIndex = 7, Resources = JsonSerializer.Serialize(new List<string> { "https://www.atlassian.com/analytics" }) }
                    }
                }
            };

            context.Roadmaps.AddRange(templates.Where(t => missingTracks.Contains(t.Track)));
            await context.SaveChangesAsync();
        }
    }
}
