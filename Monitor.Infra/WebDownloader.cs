using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Monitor.Infra
{
	public class WebFileDownloader
	{
		private readonly HttpClient _httpClient;

		public WebFileDownloader()
		{
			_httpClient = new();
			_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeBot/1.0)");
		}

		public async Task<List<ZipFileInfo>> GetFileNamesAsync(string baseUrl, string nameFilter = null, string versionFilter = null, string extensionFilter = null)
		{
			var files = new List<ZipFileInfo>(); // new List<string>();

			if (string.IsNullOrEmpty(baseUrl))
			{

			}
			else if (baseUrl.ToLowerInvariant().StartsWith("http"))
			{
				var fls = new List<string>();

				var html = await _httpClient.GetStringAsync(baseUrl);

				var matches = Regex.Matches(html, @"href=""([^""]+?-[^""]+)"""); //var matches = Regex.Matches(html, @"href=""(wget-[^""]+)""");

				var matchFiles = matches.Select(m => m.Groups[1].Value);
				fls = FilterFiles(nameFilter, versionFilter, extensionFilter, matchFiles);
				var flsFullName = fls.Select(file => $"{baseUrl}/{file}").ToList();
				files.AddRange(fls.Zip(flsFullName).Select(x => new ZipFileInfo() { FileName = x.First, FullPath = x.Second }));
			}
			else // Files
			{
				var allFiles = Directory.GetFiles(baseUrl);
				var flsFullName = FilterFiles(nameFilter, versionFilter, extensionFilter, allFiles);
				
				files.AddRange(flsFullName.Select(x => new ZipFileInfo() { FileName = Path.GetFileName(x), FullPath = x })); //.Select(f => (filename: Path.GetFileName(f), fullname: f)));
			}

			return files;
		}

		private List<string> FilterFiles(string nameFilter, string versionFilter, string extensionFilter, IEnumerable<string> matchFiles)
		{
			return matchFiles
					.Where(name =>
						(string.IsNullOrEmpty(nameFilter) || name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)) &&
						(string.IsNullOrEmpty(versionFilter) || name.Contains(versionFilter, StringComparison.OrdinalIgnoreCase)) &&
						(string.IsNullOrEmpty(extensionFilter) || name.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase))
					)
					.Select(name => new { Name = name, Version = ExtractVersion(name) })
					.Where(x => x.Version != null)
					.OrderByDescending(x => x.Version)
					.Select(x => x.Name)
					.ToList();
		}

		public async Task DownloadFileAsync(string baseUrl, string fileName, string destinationPath)
		{
			//if (string.IsNullOrWhiteSpace(fileName))
			//	throw new ArgumentException("Filename cannot be null or empty.");

			var fileUrl = baseUrl + fileName;

			using var response = await _httpClient.GetAsync(fileUrl);
			response.EnsureSuccessStatusCode();

			await using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await response.Content.CopyToAsync(fs);
		}

		public async Task DownloadFileWithExtraInfoAsync(string baseUrl, string fileName, string uniqueImageName, string destinationPath)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				throw new ArgumentException("Filename cannot be null or empty.");

			var fileUrl = $"{baseUrl}{fileName}?uniqueImageName={Uri.EscapeDataString(uniqueImageName)}";

			using var response = await _httpClient.GetAsync(fileUrl);
			response.EnsureSuccessStatusCode();

			await using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await response.Content.CopyToAsync(fs);
		}

		/// <summary>
		/// Extract version from filename like "wget-1.21.4.tar.gz"
		/// </summary>
		private Version ExtractVersion(string fileName)
		{
			var match = Regex.Match(fileName, @"[^""]+?-(\d+)\.(\d+)\.(\d+)");
			if (match.Success)
			{
				return new Version(
					int.Parse(match.Groups[1].Value),
					int.Parse(match.Groups[2].Value),
					int.Parse(match.Groups[3].Value)
				);
			}

			return null;
		}
	}

	public class ZipFileInfo
	{
		public string FileName;
		public string FullPath;
	}
}
