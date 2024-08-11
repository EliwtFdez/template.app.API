using element._118.app.API.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace element._118.app.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddCors(option =>
            {
                option.AddPolicy("MyPolicy", builder =>
                {
                    builder.WithOrigins("http://localhost:4200", "https://localhost:4200")
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });

            builder.Services.AddControllers();
            
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen();

            // Register AppDbContext with DI
            builder.Services.AddDbContext<AppDbContext>(options => { options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnStr"));});
            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = !true;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("aGVsbG9Ub0NoYXQxMjM0NTY3ODkwaGVsbG9Ub0NoYXQ=")),
                    ValidateAudience = false,
                    ValidateIssuer = false,
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("MyPolicy");
            
            app.UseAuthentication();
            
            app.UseAuthorization();


            app.MapControllers();
            app.Run();
        }
    }
}
