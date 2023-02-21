using gplat;
using log4net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using System.Runtime.InteropServices;
using System.Diagnostics;


using MetaExcelLogic = gplat.MetaExcelLogicEpp;

using Microsoft.AspNetCore.Mvc;


//.net5에서는 모듈위치가 다른 dll에 있으면 자동 등록이 되지 않음. 위치 이동
//public class GameMetaModule : Nancy.NancyModule
[ApiController]
public class MetaWebController : ControllerBase
{
	// project_namespace 설정으로
	string m_project_namespace = "gplat"; //메타 테이블 작업파일 확장자 변경

	gplat.Result m_gen_result = new gplat.Result();
	ILog m_log = gplat.Log.logger("meta.webserver");

	public MetaWebController()
	{

	}

	[Route("")]
	public RedirectResult Default()
	{
		return Redirect("/meta_tool/index.html");
	}

	[Route("/meta/{cmd}")]
	[HttpGet]
	public string GetCmd(string cmd)
	{
		var gen_result = new gplat.Result().setOk();

		string output = "";
		var param_count = Request.Query["count"];

		switch (cmd)
		{
			case "server_info":
				gen_result = handlerServerInfo();
				output = gen_result.toJson();
				break;
			case "categories":
				gen_result = handlerCategories();
				output = gen_result.toJson();
				break;

			case "file_list":
				gen_result = handlerFilelist();
				output = gen_result.toJson();
				break;

			case "file_create":
				gen_result = handlerFileCreate();
				output = gen_result.toJson();
				break;

			case "file_open": //result객체로 보내야 함 
				gen_result = handleFileOpen();
				output = gen_result.toJson();
				break;


			case "row_list_link": //result객체로 보내야 함 
				gen_result = handleLinkTable();
				output = gen_result.toJson();
				break;

			case "row_list": //result객체로 보내야함. 
				output = handleTableRows();
				break;

			case "row_add":
				output = handlerRowAdd();
				break;

			case "file_save":
				gen_result = handlerFileSave();
				output = gen_result.toJson();
				break;

			case "pack":
				output = handlerPack();
				break;

			case "next_id":
				output = handlerNextMetaId();
				break;

			case "next_ids":
				StringBuilder sb = new StringBuilder();
				if (int.TryParse(param_count, out int cnt))
				{
					for (int i = 0; i < cnt; ++i)
					{
						output = handlerNextMetaId();
						sb.Append(output);
						sb.Append("\n");
					}
				}
				output = sb.ToString();
				break;

			default:
				break;
		}

		if (gen_result.exceptionOccurred())
		{
			m_log.FatalFormat(gen_result.ToString());
		}
		else if (gen_result.fail())
		{
			m_log.ErrorFormat(gen_result.ToString());
		}
		return output;
	}

	[Route("/meta/{cmd}")]
	[HttpPost]
	public string PostCmd(string cmd)
	{
		var gen_result = new gplat.Result();
		string output = "";
		switch (cmd)
		{
			case "file_save":
				gen_result = handlerFileSave();
				output = gen_result.toJson();
				break;
		}

		if (gen_result.exceptionOccurred())
		{
			m_log.FatalFormat(gen_result.ToString());
		}
		else if (gen_result.fail())
		{
			m_log.ErrorFormat(gen_result.ToString());
		}
		return output;
	}

	[Route("/policy")]
	[HttpPost]
	public string PostPolicy()
	{
		//Post("/policy/", args => //정책서버
		string gen_packet_json = this.Request.Form["packet"];

		var req_msg = GenPacket.toNetMsgFrom<msg_gen_network.req_policy>(gen_packet_json);
		if (null == req_msg)
		{
			m_log.Error($"can not convert to req_msg from {gen_packet_json}");
		}
		m_log.Warn($"DEVICE LOCATION:{req_msg.DeviceInfo.Location}");

		//서버에 저장된 정책을 내려주도록 한다. (정책데이터를 가져오거나 지정하는 함수는 쓰레드 세이프)
		var policy_infos = GplatPolicyProcessor.cloneInfo();

		//todo: filter with deviceInfo  

		// convert to PolicyInfos
		var ack = new msg_gen_network.ack_policy();
		ack.PolicyInfos = policy_infos;
		//foreach (var db_info in policy_infos)
		//{
		//	var target_policy_info = new gplat_define.PolicyInfo();
		//	target_policy_info.Id = db_info.Id;
		//	target_policy_info.Filter = db_info.Policy_filter;
		//	target_policy_info.Location = db_info.Policy_location;
		//	target_policy_info.Name = db_info.Policy_name;
		//	target_policy_info.Platform = db_info.Policy_platform;
		//	target_policy_info.Value = db_info.Policy_value;
		//	ack.PolicyInfos.Add(target_policy_info);
		//}
		m_gen_result = GenPacket.makeGenPacketJson(ack, ++WebLogic.m_web_packet_sequence);
		return m_gen_result.m_desc;
	}

	gplat.Result makeFilelist(System.IO.DirectoryInfo root, ref JObject parent_json)
	{
		var gen_result = new gplat.Result();

		System.IO.FileInfo[] files = null;
		System.IO.DirectoryInfo[] sub_dirs = null;
		try
		{
			var file_pattern = "*@" + m_project_namespace + ".xlsx";
			files = root.GetFiles(file_pattern);
		}
		catch (UnauthorizedAccessException e)
		{
			return gen_result.setFail(e.Message);
		}
		catch (System.IO.DirectoryNotFoundException e)
		{
			return gen_result.setFail(e.Message);
		}

		if (files != null)
		{
			sub_dirs = root.GetDirectories();
			foreach (System.IO.DirectoryInfo dir_info in sub_dirs)
			{
				var dir_json_obj = new JObject();
				dir_json_obj["id"] = dir_info.FullName;
				dir_json_obj["value"] = dir_info.Name + "/";
				dir_json_obj["data"] = new JArray();
				((JArray)parent_json["data"]).Add(dir_json_obj);

				makeFilelist(dir_info, ref dir_json_obj);
			}

			foreach (System.IO.FileInfo file_info in files)
			{
				var file_json_obj = new JObject();
				file_json_obj["id"] = file_info.FullName;
				file_json_obj["value"] = file_info.Name;
				((JArray)parent_json["data"]).Add(file_json_obj);
			}
		}
		return gen_result.setOk();
	}

	// [ {id:, value:, data: []} 
	gplat.Result handlerFilelist()
	{
		var path = new DirectoryInfo(MetaExcelConfig.m_meta_excel_dir); // 별도 경로로 변경

		var root_json_obj = new JObject();
		root_json_obj["id"] = "/";
		root_json_obj["value"] = "/";
		root_json_obj["open"] = true;
		root_json_obj["data"] = new JArray();

		var gen_result = makeFilelist(path, ref root_json_obj);
		if (gen_result.fail())
		{
			return gen_result;
		}

		var result_json_obj = new JArray();
		result_json_obj.Add(root_json_obj);
		return gen_result.setOk(JsonConvert.SerializeObject(result_json_obj));
	}

	gplat.Result handlerCategories()
	{
		var gen_result = new gplat.Result();
		var root_path = Crossplatform.MakePath(WebLogic.m_tool_dir + "/meta_schema");
		var path = new DirectoryInfo(root_path);

		System.IO.FileInfo[] files = null;
		try
		{
			files = path.GetFiles("*.json");
		}
		catch (UnauthorizedAccessException e)
		{
			return gen_result.setFail(e.Message);
		}
		catch (System.IO.DirectoryNotFoundException e)
		{
			return gen_result.setFail(e.Message);
		}

		var result_json = new List<JObject>();
		Int32 idx = 1;
		if (files != null)
		{
			foreach (System.IO.FileInfo file_info in files)
			{
				if (-1 == file_info.Name.IndexOf("_meta"))
				{
					continue;
				}
				if (-1 != file_info.Name.IndexOf("_gen"))
				{
					continue;
				}

				var name = file_info.Name;
				name = name.Remove(name.Length - 10); //meta_tail_length : ("_meta.json").Length = 10 

				var file_json_obj = new JObject();
				file_json_obj["id"] = idx++;
				file_json_obj["value"] = name;
				result_json.Add(file_json_obj);
			}
		}

		return gen_result.setOk(JsonConvert.SerializeObject(result_json));
	}

	// 스키마 디렉토리를 변경하도록 한다.
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

	//private IntPtr handle;
	//[DllImport("User32.dll")]
	//private static extern bool SetForegroundWindow(IntPtr hWnd);
	gplat.Result handleFileOpen()
	{
		gplat.Result gplat_result = new gplat.Result().setOk();

		var file_path = this.Request.Query["path"];
		var category = this.Request.Query["category"];
		try
		{
			var start_info = new ProcessStartInfo();
			start_info.CreateNoWindow = false;
			start_info.UseShellExecute = true;
			start_info.FileName = file_path;
			var p = System.Diagnostics.Process.Start(start_info);

			//handle = p.MainWindowHandle;
			//SetForegroundWindow(handle);
		}
		catch (Exception ex)
		{
			gplat_result.setExceptionOccurred($"{category}:{file_path}\n{ex.Message}");
		}
		return gplat_result;
	}

	gplat.Result handlerFileCreate()
	{
		gplat.Result gen_result = new gplat.Result();

		var file_path = this.Request.Query["path"];
		var category = this.Request.Query["category"];

		//use resource_path
		string meta_file_path = Crossplatform.MakePath(MetaExcelConfig.m_meta_data_dir + "/" + file_path);

		FileInfo file_info = new FileInfo(meta_file_path);

		MetaExcelLogic logic = new MetaExcelLogic();

		logic.loadMetaSchema(category);

		// make thrfit json string 
		var meta_container = MetaManager.it.metaContainer(category);
		if (null == meta_container)
		{
			return gen_result.setFail($"meta container:{category} not exist");
		}
		var memory_stream = new System.IO.MemoryStream();
		var transport = new Thrift.Transport.TStreamTransport(memory_stream, memory_stream);
		var protocol = new Thrift.Protocol.TJSONProtocol(transport);
		meta_container.Write(protocol);
		var json_str = Encoding.UTF8.GetString(memory_stream.ToArray());


		gen_result = logic.makeExcelData(json_str);
		if (gen_result.fail())
		{
			return gen_result;
		}

		gen_result = logic.saveToExcel(meta_file_path);
		if (gen_result.fail())
		{
			return gen_result;
		}

		//성공시 결과값 미리 생성 
		var result_data_obj = new JObject();
		result_data_obj["id"] = file_info.FullName;
		result_data_obj["value"] = file_info.Name;
		return gen_result.setOk(JsonConvert.SerializeObject(result_data_obj));
	}
	gplat.Result handlerFileSave()
	{
		var gen_result = new gplat.Result();

		string meta_file_path;
		string category;
		string table_data;

		if ("POST" == this.Request.Method)
		{
			meta_file_path = this.Request.Form["path"];
			category = this.Request.Form["category"];
			table_data = this.Request.Form["data"];
		}
		else
		{
			meta_file_path = this.Request.Query["path"];
			category = this.Request.Query["category"];
			table_data = this.Request.Query["data"];
		}

		FileInfo file_info = null;
		try
		{
			file_info = new FileInfo(meta_file_path);
			MetaExcelLogic logic = new MetaExcelLogic();
			gen_result = logic.updateToExcel(category, table_data, meta_file_path);
		}
		catch (Exception ex)
		{
			string err_msg = String.Format("file update failed:\n[{0}]\n{1}", meta_file_path, ex.Message);
			m_log.ErrorFormat(err_msg);
			return gen_result.setExceptionOccurred(err_msg);
		}

		if (gen_result.fail())
		{
			return gen_result;
		}

		var result_json_obj = new JObject();
		result_json_obj["id"] = file_info.FullName;
		result_json_obj["value"] = file_info.Name;

		return gen_result.setOk(JsonConvert.SerializeObject(result_json_obj));
	}
	string handlerPack()
	{
		//partial 
		MetaExcelLogic logic = new MetaExcelLogic();
		var gen_result = logic.packFileSystem(new DirectoryInfo(MetaExcelConfig.m_meta_data_dir), gplat.Server.Config.m_meta_version, gplat.Server.Config.m_meta_filename);
		return gen_result.toJson();
	}
	string handlerNextMetaId()
	{
		var gen_result = new gplat.Result();

		var next_db_id = gplat.Singleton<gplat.DbIdManager>.it.nextDbId(gplat_define.db_id_e.GAME_META_ID);
		if (0 == next_db_id)
		{
			gen_result.setFail("next meta id can not accuired.");
			return gen_result.ToString();
		}

		// category and id
		return next_db_id.ToString();
	}

	gplat.Result createNewThriftFile(string category, string file_path)
	{
		var gen_result = new gplat.Result();
		var meta_container = MetaManager.it.metaContainer(category);
		if (null == meta_container)
		{
			return gen_result.setFail(string.Format("meta container:{0} not exist", category));
		}

		var resource_file_path = Crossplatform.MakePath(WebLogic.m_resource_dir + "\\" + file_path);
		FileInfo file_info = new FileInfo(resource_file_path);
		Stream file_stream = file_info.Create();
		try
		{
			//var memory_stream = new System.IO.MemoryStream();
			var transport = new Thrift.Transport.TStreamTransport(file_stream, file_stream);
			var protocol = new Thrift.Protocol.TJSONProtocol(transport);
			meta_container.Write(protocol);
		}
		catch (Exception)
		{

		}
		file_stream.Close();

		return gen_result.setOk();
	}
	string handleTableRows()
	{
		var file_path = this.Request.Query["path"];

		var gen_result = readExcel(file_path);
		// json으로 결과내용 나오도록 수정 
		return gen_result.toJson();
	}

	gplat.Result handleLinkTable()
	{
		string meta_category = this.Request.Query["path"];
		var gen_result = new gplat.Result();

		//카테고리 가져오기 
		gen_result.m_desc = MetaManager.link_it.toThriftJsonString(meta_category);
		return gen_result;
	}


	gplat.Result readExcel(string in_file_path)
	{
		var meta_logic = new MetaExcelLogic();

		String metacategory = MetaManager.metaCategory(in_file_path);

		return meta_logic.loadExcelToThrift(in_file_path);
	}
	string readJsonThrift(string in_file_path)
	{
		string metacategory = MetaManager.metaCategory(in_file_path);
		var meta_container = MetaManager.it.metaContainer(metacategory);
		if (null == meta_container)
		{
			//log error
			// meta can not create or error
			return "";
		}
		//thrift 
		//read from file 
		{
			var file_stream = System.IO.File.Open(in_file_path, FileMode.Open);
			if (file_stream == null)
			{
				// process error
				return "";
			}
			var transport = new Thrift.Transport.TStreamTransport(file_stream, file_stream);
			var protocol = new Thrift.Protocol.TJSONProtocol(transport);
			meta_container.Read(protocol);
			file_stream.Close();
		}
		//thrift
		//write to memory buffer 
		var return_json = "";
		{
			var mem_stream = new MemoryStream();
			var transport = new Thrift.Transport.TStreamTransport(mem_stream, mem_stream);
			var protocol = new Thrift.Protocol.TJSONProtocol(transport);
			meta_container.Write(protocol);
			return_json = Encoding.UTF8.GetString(mem_stream.ToArray());
		}
		return return_json;
	}

	string handlerRowAdd()
	{
		var result_json = new JObject();
		result_json["result"] = "fail";
		var meta_rows = new JArray();
		result_json["meta_rows"] = meta_rows;

		var file_path = Request.Query["path"];
		var category = Request.Query["category"];
		Int32 count = Int32.Parse(Request.Query["count"]);
		if (0 == count)
		{
			count = 1;
		}
		string meta_data_name = category + "_meta.data, csThriftDefine";
		Type meta_data_type = Type.GetType(meta_data_name);
		var meta_data = (Thrift.Protocol.TBase)Activator.CreateInstance(meta_data_type);

		for (Int32 i = 0; i < count; ++i)
		{
			var row_json = new JObject();
			var next_db_id = gplat.Singleton<gplat.DbIdManager>.it.nextDbId(gplat_define.db_id_e.GAME_META_ID);
			m_log.InfoFormat("add new row :{0}", next_db_id);

			PropertyInfo meta_id_property = meta_data_type.GetProperty("Meta_id");
			meta_id_property.SetValue(meta_data, next_db_id); // new guid

			// make thrift data 
			var meta_row = "";
			{
				var mem_stream = new MemoryStream();
				var transport = new Thrift.Transport.TStreamTransport(mem_stream, mem_stream);
				var protocol = new Thrift.Protocol.TJSONProtocol(transport);
				meta_data.Write(protocol);
				meta_row = Encoding.UTF8.GetString(mem_stream.ToArray());
			}
			// make json string
			row_json["row_id"] = next_db_id;
			row_json["meta_row"] = meta_row;
			meta_rows.Add(row_json);

		}
		result_json["result"] = "success";
		var return_json = JsonConvert.SerializeObject(result_json);

		m_log.InfoFormat("Row Add: return_json :{0}", return_json);
		return return_json;
	}
}
