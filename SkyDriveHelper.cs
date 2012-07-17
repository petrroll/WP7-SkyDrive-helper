using Microsoft.Live;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace SkyDriveHelper
{
    public class SkyDriveHelperConnect
    {
        #region ChceckConnectionAndReturnSession
        public static LiveConnectSession GetSession(Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                return e.Session;
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
    public class SkyDriveHelperFolder
    {
        #region CheckAndCreateFolder
        public delegate void SkyHelperFolderDelegate(bool result, string FolderID);
        public event SkyHelperFolderDelegate FolderReady;

        private LiveConnectClient client;
        private string folderName = string.Empty;

        public SkyDriveHelperFolder()
        {

        }

        public void InitClient (LiveConnectSession session)
        {
            client = new LiveConnectClient(session);
            client.GetCompleted += (object sender, LiveOperationCompletedEventArgs e) =>
            { getAllFolders_GetCompleted(sender, e, folderName, client); };  //Return data of all folders

            client.PostCompleted += new EventHandler<LiveOperationCompletedEventArgs>(createFolder_Completed);  //Called when folder is created
        }

        public void CheckAndIfNotExistsCreateFolder(string folderName)
        {
            this.folderName = folderName;
            client.GetAsync("me/skydrive/files?filter=folders");
        }



        private void getAllFolders_GetCompleted(object sender, LiveOperationCompletedEventArgs e, string nazevSlozky, LiveConnectClient client)
        {
            if (e.Error == null)
            {
                string skyDriveFolderID = string.Empty;

                //check all the folders in their skydrive
                Dictionary<string, object> folderData = (Dictionary<string, object>)e.Result;
                List<object> folders = (List<object>)folderData["data"];

                //go through each folder and see if the isolatedstoragefolder exists
                foreach (object item in folders)
                {
                    Dictionary<string, object> folder = (Dictionary<string, object>)item;
                    if (folder["name"].ToString() == nazevSlozky)
                        skyDriveFolderID = folder["id"].ToString();
                }


                //if the IsolatedStorageFolder does NOT exist, create it
                if (skyDriveFolderID == string.Empty)
                {
                    Dictionary<string, object> skyDriveFolderData = new Dictionary<string, object>();
                    skyDriveFolderData.Add("name", nazevSlozky);
                    client.PostAsync("me/skydrive", skyDriveFolderData); //creating the IsolatedStorageFolder in Skydrive
                }

                //otherwise check if the backup file is in the IsolatedStorageFile
                else
                {
                    if (FolderReady != null) { FolderReady(true, skyDriveFolderID); }
                }
            }
            else
            {
                if (FolderReady != null)
                {
                    if (FolderReady != null) { FolderReady(false, e.Error.Message); }
                }
            }
        }

        private void createFolder_Completed(object sender, LiveOperationCompletedEventArgs e)
        {
            string skyDriveFolderID;
            if (e.Error == null)
            {
                Dictionary<string, object> folder = (Dictionary<string, object>)e.Result;
                skyDriveFolderID = folder["id"].ToString(); //grab the folder ID
                if (FolderReady != null) { FolderReady(true, skyDriveFolderID); }
            }
            else
            {
                if (FolderReady != null) { FolderReady(false, e.Error.Message); }
            }
        }

        #endregion
    }

    public class SkyDriveHelperFile
    {
        #region init
        private LiveConnectClient client;

        public SkyDriveHelperFile()
        {

        }

        public void InitClient(LiveConnectSession _session)
        {
            client = new LiveConnectClient(_session);
            client.UploadCompleted += (object sender, LiveOperationCompletedEventArgs args) => { iSFile_UploadCompleted(sender, args, fileNameInSkyDrive); };
            client.GetCompleted += (object sender, LiveOperationCompletedEventArgs e) => { getFiles_GetCompleted(sender, e, fileNameID); };
            client.DownloadCompleted += (object sender, LiveDownloadCompletedEventArgs e) => client_DownloadCompleted(sender, e, fileNameInIsolatedStorage);
        }
        #endregion

        #region UploadFile
        public delegate void SkyHelperUploadDelegate(bool result, string fileName);
        public event SkyHelperUploadDelegate UploadComplete;

        IsolatedStorageFileStream readStream;
        MemoryStream memoryStream;

        string fileNameInSkyDrive = string.Empty;

        public void UploadFile<T>(string skyDriveFolderID, T data, string fileNameInSkyDrive)
        {
            this.fileNameInSkyDrive = fileNameInSkyDrive;
            if (skyDriveFolderID != string.Empty)
            {
                try
                {
                    memoryStream = new MemoryStream();

                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                    xmlWriterSettings.Indent = true;

                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings))
                    {
                        serializer.Serialize(xmlWriter, data);
                    }

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    client.UploadAsync(skyDriveFolderID, fileNameInSkyDrive, true, memoryStream, null);
                }

                catch (Exception ex)
                {
                    if (memoryStream != null) { memoryStream.Dispose(); }
                    if (UploadComplete != null) { UploadComplete(false, ex.Message.ToString()); }
                }
            }
            else
            {
                if (UploadComplete != null) { UploadComplete(false, fileNameInSkyDrive); }
            }
        }

        public void UploadFile(string skyDriveFolderID, string fileNameInIsolatedStorage, string fileNameInSkyDrive)
        {
            this.fileNameInSkyDrive = fileNameInSkyDrive;
            if (skyDriveFolderID != string.Empty)
            {


                try
                {
                    using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        readStream = myIsolatedStorage.OpenFile(fileNameInIsolatedStorage, FileMode.Open);
                        client.UploadAsync(skyDriveFolderID, fileNameInSkyDrive, true, readStream, null);
                    }
                }

                catch(Exception ex)
                {
                    if (readStream != null) { readStream.Dispose(); }
                    if (UploadComplete != null) { UploadComplete(false, ex.Message); }
                }
            }
            else
            {
                if (UploadComplete != null) { UploadComplete(false, fileNameInSkyDrive); }
            }
        }

        private void iSFile_UploadCompleted(object sender, LiveOperationCompletedEventArgs args, string fileName)
        {
            if (readStream != null) { readStream.Dispose(); }
            if (memoryStream != null) { memoryStream.Dispose(); }

            if (args.Error == null)
            {
                if (UploadComplete != null) { UploadComplete(true, fileName); }
            }
            else
            {
                if (UploadComplete != null) { UploadComplete(false, fileName); }
            }
        }

        #endregion 

        #region GetFileIDFromSkyDrive
        public delegate void SkyHelperFileDelegate(bool result, string fileID, string fileName, DateTimeOffset date);
        public event SkyHelperFileDelegate IsFileThere;

        private string fileNameID = string.Empty;

        public void GetFileID(string fileName, string skyDriveFolderID)
        {
            this.fileNameID = fileName;
            client.GetAsync(skyDriveFolderID + "/files");
        }

        void getFiles_GetCompleted(object sender, LiveOperationCompletedEventArgs e, string fileNameID)
        {
            string fileID = string.Empty;

            List<object> data = (List<object>)e.Result["data"];
            DateTimeOffset date = DateTime.MinValue;

            foreach (IDictionary<string, object> content in data)
            {
                if (((string)content["name"]).Equals(fileNameID))
                {
                    fileID = (string)content["id"];
                    try
                    {
                        date = DateTimeOffset.Parse(((string)content["updated_time"]).Substring(0, 24));
                    }

                    catch { }

                    break;
                }
            }

            if (fileID != string.Empty)
            {
                if (IsFileThere != null) { IsFileThere(true, fileID, fileNameID, date); }
            }

            else
            {
                if (IsFileThere != null) { IsFileThere(false, fileID, fileNameID, date); }
            }
        }
        #endregion

        #region DownloadFile
        public delegate void SkyHelperDownloadDelegate(bool result, string fileName);
        public  event SkyHelperDownloadDelegate DownloadComplete;

        private string fileNameInIsolatedStorage = string.Empty;

        public  void DownloadFile(string fileID, string fileNameInIsolatedStorage)
        {
            this.fileNameInIsolatedStorage = fileNameInIsolatedStorage;
            if (fileID != null)
            {
                client.DownloadAsync(fileID + "/content");
                
            }

            else
            {
                if (DownloadComplete != null) { DownloadComplete(false, string.Empty); }
            }
        }

        void client_DownloadCompleted(object sender, LiveDownloadCompletedEventArgs e, string saveToPath)
        {
            Stream stream = e.Result;

            try
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(saveToPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);  //And now save it somehow doesn't work
                    }
                }
                stream.Dispose();
                if (DownloadComplete != null) { DownloadComplete(true, saveToPath); }
            }

            catch(Exception ex)
            {
                if (DownloadComplete != null) { DownloadComplete(false, ex.Message); }
            }
        }

        #endregion      //Not done
    }
}
