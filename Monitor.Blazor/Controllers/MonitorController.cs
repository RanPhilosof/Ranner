﻿using AppMonitoring.SharedTypes;
using Microsoft.AspNetCore.Mvc;
using Monitor.Blazor.Interfaces;
using Monitor.Infra;
using Montior.Blazor.Data;
using Newtonsoft.Json.Linq;

namespace Monitor.Blazor.Controllers
{
    [ApiController]
    [Route("/api/Rest")]
    public class MonitorController : ControllerBase
    {
        private readonly IMonitorService _monitorService;
        public MonitorController(IMonitorService monitorService)
        {
               _monitorService = monitorService;
        }

        [HttpGet("KillAll")]
        public ActionResult KillAll()
        {
            _monitorService.KillAll();
            return Ok();
        }

        [HttpGet("StartAll")]
        public ActionResult StartAll()
        {
            _monitorService.StartAll();
            return Ok();
        }

        [HttpGet("GetAllCsProjs")]
        public ActionResult GetAllCsProjs()
        {
            _monitorService.GetAllCsprojs();
            return Ok();
        }

        [HttpGet("GetAllCsProjForBuild")]
        public ActionResult GetAllCsProjForBuild()
        {
            _monitorService.GetAllCsprojForBuild();
            return Ok();
        }

        [HttpGet("GetAllCsProjForPublish")]
        public ActionResult GetAllCsProjForPublish()
        {
            _monitorService.GetAllCsProjForPublish();
            return Ok();
        }

        [HttpGet("GetAllDeployedFoldersToCopy")]
        public ActionResult GetAllDeployedFoldersToCopy()
        {
            _monitorService.GetAllDeployedFoldersToCopy();
            return Ok();
        }


        // Example: http://localhost:5087/api/Rest/LoadFile?fileName=dadsaas&isForceRunAllOnLocalHost=true&overrides[0].key=111&overrides[0].value=222&overrides[1].key=333&overrides[1].value=444
        [HttpGet("LoadFile")]
        public ActionResult LoadFile(
            string fileName, 
            bool isForceRunAllOnLocalHost,
            [FromQuery] Entry[] overrides)
        {
            var jsonText = System.IO.File.ReadAllText(fileName);
            var jObj = JObject.Parse(jsonText);

            foreach (var @override in overrides)
                jObj.SelectToken(@override.Key)?.Replace(@override.Value);

            // Examples:
            //jObj.SelectToken("$.ImagesVariables.SourcesVariables[?(@.UniqueName=='myZips1')].Url")?.Replace(@"C:\myzips");
            //jObj.SelectToken("$.ImagesVariables.SourcesVariables[?(@.UniqueName=='myZips1')].FileNameFilter")?.Replace(@"Console1");
            //jObj.SelectToken("$.ImagesVariables.SelectedImagesVariables[?(@.UniqueName=='zips1')].ZipFileInfo.FileName")?.Replace(@"Console5-master-1.1.1.zip");
            //jObj.SelectToken("$.ImagesVariables.SelectedImagesVariables[?(@.UniqueName=='zips1')].ZipFileInfo.FullPath")?.Replace(@"C:\myzips\Console5-master-1.1.1.zip");

            var newFileName = fileName + ".tmp";

            System.IO.File.WriteAllText(newFileName, jObj.ToString());

            var settings = IMonitorService.GetSettingsFromFullPath(newFileName);
            settings.CurrentFileName = "LoadFromRestApi";

            _monitorService.SetAllAgentsSettings(settings, false, true);

            return Ok();
        }

        private void ForceRunOnLocalHost(MonitorPageSettings monitorPageSettings)
        {

        }

        public class Entry
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }

	[ApiController]
	[Route("download")]
	public class DownloadController : ControllerBase
	{
		private readonly IMonitorService _monitorService;
		public DownloadController(IMonitorService monitorService)
		{
			_monitorService = monitorService;
		}

		[HttpGet("{fileName}")]
		public IActionResult DownloadFile(string fileName, [FromQuery] string uniqueImageName)
		{
            var curSets = _monitorService.GetCurrentSettings();
            var cacheFolder = curSets.Configuration.Configuration.Where(x => x.Key == UI_Configuration.ImagesCacheFolder).FirstOrDefault().Value;

            var filePath = Path.Combine(cacheFolder, fileName);

			if (!System.IO.File.Exists(filePath))
            {                
                var iv = curSets.ImagesVariables.SelectedImagesVariables.Where(x => x.Active && x.UniqueName == uniqueImageName).FirstOrDefault();
                if (iv == null)
                    return NotFound($"The file {fileName} not found in settings");

                var folderPath = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                if (iv.ZipFileInfo.FullPath.ToLowerInvariant().StartsWith("http"))
                {
					var webFileDownloader = new WebFileDownloader();
                    var response = webFileDownloader.DownloadFileAsync(iv.ZipFileInfo.FullPath, "", filePath);
                    response.Wait();
				}
                else
                {
					System.IO.File.Copy(iv.ZipFileInfo.FullPath, filePath);
				}
			}

			//Console.WriteLine($"Downloading file {fileName} with unique name {uniqueImageName}");

			var contentType = "application/octet-stream";
			var fileBytes = System.IO.File.ReadAllBytes(filePath);

			return File(fileBytes, contentType, fileName);
		}
	}
}
