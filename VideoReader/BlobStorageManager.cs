using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;
using System;

namespace AVRecordManager
{
    public class BlobStorageManager : IDisposable
    {
        
        private CloudStorageAccount _storageAccount;
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _container;
        private CloudBlockBlob _blobBlock;
        private CloudBlobStream _stream;

        public BlobStorageManager(string filename)
        {
           
            _storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureBlobStorage.ConnectionString"));
            _blobClient = _storageAccount.CreateCloudBlobClient();
            _container = _blobClient.GetContainerReference("webrtcpoccontainer");
            _container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            _blobBlock = _container.GetBlockBlobReference(filename);
            
        

        }

        internal Stream OpenRead()
        {
            return _blobBlock.OpenRead();
        }

        public void OpenWrite()
        {
            _stream = _blobBlock.OpenWrite();
        }

        public async Task UploadAsync(MemoryStream buffer)
        {
            await _stream.WriteAsync(buffer.ToArray(), 0, buffer.ToArray().Length);
            buffer.Clear();
        }

        public void Commit()
        {
            _stream.Commit();
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}