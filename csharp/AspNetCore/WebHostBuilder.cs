using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net;
using Microsoft.Extensions.Logging;
using gplat;
using log4net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.IO;
using gplat.crypto;

namespace gplat
{
	public class WebHostBuilder
	{
		protected ILog m_log = Log.logger("web");
		protected IHost m_host = null;
		protected IHostBuilder m_host_builder = null;
		bool m_running = false;

		protected string m_ip_addr;
		protected UInt16 m_port;
		public void Init(string in_ip_addr, UInt16 in_port)
		{
			m_ip_addr = in_ip_addr;
			m_port = in_port;
		}

		public IHostBuilder CreateHostBuilder()
		{
			m_host_builder = Host.CreateDefaultBuilder()
			.ConfigureLogging(in_logging =>
			{
				//log4net으로 로거 변경시 주석 해제 
				in_logging.ClearProviders();
				in_logging.AddLog4Net("../config/log4net_server.xml");
			})
			.ConfigureWebHostDefaults(in_web_builder =>
			{
				in_web_builder.UseKestrel(in_options =>
				{
					// IPv6 형태로 맞춰서 외부접속이 가능하도록 변경
					IPAddress ip_addr = null;
					try
					{
						IPAddress[] ip_addrs = Dns.GetHostAddresses(m_ip_addr);
						ip_addr = ip_addrs[0];
					}
					catch (Exception ex)
					{
					}

					if (ip_addr == null)
					{
						ip_addr = IPAddress.IPv6Any;
					}

					//in_options.Listen(ip_addr, m_port);

					// open http
					in_options.Listen(ip_addr, m_port);

					// make cerificate 
					// 아래 X509Certificate2.CreateFromPemFile 구문이 작동하나 실제 접속시,
					// 보안 패키지에 사용할 수 있는 인증서가 없습니다. 라는 문제가 발생
					// 내부적으로 PEM을 로드하여, pk12 형태로 변환해주어서 사용하도록 변경
					var certificate = Certificate.GetCertificate(ServerConfig.it.sslCertPath, ServerConfig.it.sslPrivKeyPath);

					// open https
					in_options.Listen(ip_addr, m_port + 1,
						listenOptions =>
						{
							listenOptions.UseHttps(certificate);
						});

					//in_server_options.Listen(m_ip_addr, m_ssl_port,
					//	listenOptions =>
					//	{
					//		listenOptions.UseHttps("testCert.pfx",
					//			"testPassword");
					//	});
				});
			});

			OnCreateHostBuilder();

			m_log.Info($"{GetType().Name}:{m_ip_addr}:{m_port} build complete.");
			return m_host_builder;
		}

		protected virtual IHostBuilder OnCreateHostBuilder()
		{
			return m_host_builder;
		}

		public WebHostBuilder Start(string in_ip_addr = "", UInt16 in_port = 0)
		{
			if (m_running)
			{
				Log.logger("web").Warn("web host already started");
				return this;
			}

			if (in_ip_addr.HasStringValue())
			{
				Init(in_ip_addr, in_port);
			}

			m_host = CreateHostBuilder().Build();
			//m_host.StartAsync();
			m_host.Start();
			m_running = true;
			return this;
		}

		public void Stop()
		{
			m_host?.StopAsync();
		}
	}
}
