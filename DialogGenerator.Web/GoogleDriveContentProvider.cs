using DialogGenerator.Model;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DialogGenerator.Web
{
    public class GoogleDriveContentProvider :IContentProvider
    {
        static string[] Scopes = { DriveService.Scope.Drive };
        private string ApplicationName = "DialogGenerator";
        private UserCredential mCredentials;
        private DriveService mDriveService;

        public GoogleDriveContentProvider()
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                mCredentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            mDriveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = mCredentials,
                ApplicationName = ApplicationName,
            });
        }

        public IEnumerable<FileItem> GetCharacters()
        {
            var fileList = new List<FileItem>();
            // Define parameters of request.
            FilesResource.ListRequest listRequest = mDriveService.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var _newFile = new FileItem
                    {
                        Name = file.Name
                    };

                    fileList.Add(_newFile);
                }
            }
            else
            {
            }

            return fileList;
        }
    }
}
