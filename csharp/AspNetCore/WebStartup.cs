using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using gplat;

namespace gplat
{
	public abstract class WebStartup
	{
		public string toolDir => Crossplatform.MakePath(Directory.GetCurrentDirectory() + "/" + gplat.Server.Config.m_meta_tool_dir);
		public string schemaDir => Crossplatform.MakePath(Directory.GetCurrentDirectory() + "/" + gplat.Server.Config.m_meta_schema_dir);

		public string resourceDir => Crossplatform.MakePath(Directory.GetCurrentDirectory() + "/" + gplat.Server.Config.m_meta_resource_dir);

		protected bool m_stopped = false;

		public IConfiguration Configuration { get; }


		public WebStartup(IConfiguration in_configuration)
		{
			Configuration = in_configuration;

			WebLogic.m_tool_dir = toolDir;
			WebLogic.m_resource_dir = resourceDir; //원시데이터 경로 추가
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection in_services)
		{
			in_services.AddControllers();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder in_app, IWebHostEnvironment in_env)
		{
			//in_app.UseHttpsRedirection();
			in_app.UseRouting();
			in_app.UseAuthorization();

			ConfigureStaticFiles(in_app, in_env);

			in_app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}

		public void OnShutdown()
		{
			if (m_stopped)
			{
				return;
			}
			m_stopped = true;
			ProcessShutdown();
		}
		protected abstract void ProcessShutdown();

		//추가하고 싶은 페이지를 추가합니다. 
		protected abstract void ConfigureStaticFiles(IApplicationBuilder in_app, IWebHostEnvironment in_env);
	}
}
