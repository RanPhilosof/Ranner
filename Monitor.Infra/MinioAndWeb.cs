using Minio.DataModel.Args;
using Minio;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Monitor.Infra
{
    public class MinioAndWeb
    {
        public async void A()
        {

            Console.WriteLine("Hello, World!");
            await DownloadFromWeb();

            var minio = new MinioClient()
                .WithEndpoint("localhost", 9000)
                .WithCredentials("minioadmin", "minioadmin")
                .WithSSL(false)
                .Build();

            await UploadFile(minio, "ran", "e.zip", @"C:\GitHub\ToZip\Examples.zip");
            await DownloadFile(minio, "ran", "e.zip", @"C:\GitHub\ToZip\E1.zip");

            var zipExtractor = new ZipExtractor();

			zipExtractor.ExtractAllFiles(@"C:\GitHub\ToZip\E1.zip", @"C:\GitHub\ToZip\E1");
        }

        public async Task UploadFile(IMinioClient minio, string bucketName, string objectName, string fileToUpload)
        {
            //var bucketName = "mybucket";
            //var objectName = "myfile.txt";
            //var filePath = @"C:\path\to\file.txt";
            var contentType = "application/x-7z-compressed";

            // Ensure bucket exists
            bool found = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
                await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));

            // Upload file
            await minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFileName(fileToUpload)
                .WithContentType(contentType));

            Console.WriteLine("✅ File uploaded successfully.");
        }

        async Task DownloadFile(IMinioClient minio, string bucketName, string objectName, string downloadPath)
        {
            //var downloadPath = @"C:\path\to\downloaded.7z";

            await minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFile(downloadPath));

            Console.WriteLine("✅ .7z file downloaded.");
        }

        async Task DownloadFromWeb(string fileUrl = "https://www.learningcontainer.com/wp-content/uploads/2020/05/sample-large-zip-file.zip")
        {
            string destinationPath = @"C:\GitHub\ToZip\Downloaded.zip";

            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(fileUrl);
            response.EnsureSuccessStatusCode(); // throws if not 200-299

            using FileStream fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);

            Console.WriteLine("✅ File downloaded successfully.");
        }
    }

    public class ZipExtractor
    {
		public void ExtractAllFiles(string archivePath, string extractPath)
		{
			if (!Directory.Exists(extractPath))
				Directory.CreateDirectory(extractPath);

			using var archive = ArchiveFactory.Open(archivePath);
			var options = new ExtractionOptions
			{
				ExtractFullPath = true,
				Overwrite = true
			};

			foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
			{
				entry.WriteToDirectory(extractPath, options);
			}
		}
	}
}