using admin.common;

using gplat;

using log4net;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[ApiController]
public class AdminWebController : ControllerBase
{
	ILog m_log = gplat.Log.logger("admin.webserver");

	[Route("")]
	[HttpGet]
	public RedirectResult Default()
	{
		// HTTPS 로 Redirect 코드 추가
		if (Request.Scheme.Equals("http") && Request.Host.Host.Contains("vividstudio.co.kr"))
		{
			return Redirect($"https://{Request.Host.Host}:{Request.Host.Port + 1}");
		}
		var app_dir = gplat.Server.Config.m_admin_app_dir;
		return Redirect($"{app_dir}/index.html");
	}

	gplat.Result handlerServerInfo()
	{
		var gen_result = new gplat.Result();

		// world / build mode / schema revision  
		//성공시 결과값 미리 생성 
		var result_data_obj = new JObject();
		result_data_obj["name"] = BuildInfocsServer.m_last_changed_revision;
		result_data_obj["build_mode"] = BuildInfocsServer.m_build_mode; //server lib의 빌드모드로 판단한다.
		return gen_result.setOk(JsonConvert.SerializeObject(result_data_obj));
	}

	[Route("/admin/login")]
	[HttpGet]
	public string GetLogin()
	{
		var gen_result = new gplat.Result();
		var admin_user_name = Request.Query["admin_user_name"];
		var admin_user_password = Request.Query["admin_user_password"];


		gen_result = AdminManager.it.AdminLogin(admin_user_name, admin_user_password);

		return gen_result.toJson();
	}

	[Route("/admin/change_password")]
	[HttpGet]
	public string GetChangePassword()
	{
		var gen_result = new gplat.Result();

		string json_admin_change_password_info = Request.Query["admin_change_password_info"];


		sc_info.AdminChangePasswordInfo admin_change_password_info = JsonConvert.DeserializeObject<sc_info.AdminChangePasswordInfo>(json_admin_change_password_info);

		gen_result = AdminManager.it.AdminChangePassword(admin_change_password_info);

		return gen_result.toJson();
	}

	[Route("/dashboard")]
	[HttpGet]
	public string GetDashboard()
	{
		return new gplat.Result().setOk().toJson();
	}

	[Route("/server_info")]
	[HttpGet]
	public string GetServerInfo()
	{
		//NAdminManager.it.onRoute(m_log, Request, authority_e.Read);
		var gen_result = new gplat.Result();

		var m_start_at = Request.Query["start_at"];
		var m_end_at = Request.Query["end_at"];
		QueryValue world_id = Request.Query["world_id"];

		gen_result = ServerInfoLogGet(m_start_at, m_end_at, world_id);

		return gen_result.toJson();
	}

	gplat.Result ServerInfoLogGet(string start_at, string end_at, Int32 world_id)
	{
		gplat.Result gen_result;

		var db_req = new server_msg_db.req_server_info_log_get_gamelog();
		db_req.In_start_at = start_at;
		db_req.In_end_at = end_at;
		db_req.In_world_id = world_id;

		var db_exec = new exec_server_info_log_get_gamelog();
		gen_result = db_exec.process(db_req);
		if (gen_result.fail())
		{
			return gen_result;
		}

		var db_ack = db_exec.toThrift();

		return gen_result.setOk(JsonConvert.SerializeObject(db_ack));
	}

	[Route("/set_region")]
	[HttpGet]
	public string GetSetRegion()
	{
		//NAdminManager.it.onRoute(m_log, Request, authority_e.Read);
		var gen_result = new gplat.Result();

		var locale = Request.Query["locale"].ToString();

		if (locale == null)
		{
			return gen_result.setFail(result.code_e.SYSTEM_INVALID_ADMIN_LOCALE, "Invalid local value");
		}

		gen_result = AdminManager.it.SetRegion(locale);

		return gen_result.toJson();
	}
}
