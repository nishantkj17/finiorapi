using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialDiaryApi
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}
		public static string ConnectionString
		{
			get;
			private set;
		}
		public IConfiguration Configuration { get; }

		private void SetEncryptedMongoConnection()
		{
			var builder = Configuration.GetConnectionString("FinDB");
			var password = Configuration["admin"];
			ConnectionString = builder.Replace("PasswordToReplace", password);
		}
		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			SetEncryptedMongoConnection();
			services.AddCors(c =>
			{
				c.AddPolicy(name: "AllowSpecificOrigins", options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
			});
			services.AddSwaggerGen();
			services.AddControllers();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Finior API V1");
			});
			app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
