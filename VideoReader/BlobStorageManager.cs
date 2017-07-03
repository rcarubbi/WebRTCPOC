using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;
using System;

namespace AVRecordManager
{
    public class BlobStorageManager 
    {
        
        private CloudStorageAccount _storageAccount;
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _container;
        private CloudBlockBlob _blobBlock;
      

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

        public Stream OpenWrite()
        {
            return _blobBlock.OpenWrite(); 
        }

        public void Commit(Stream stream)
        {
            if (stream is CloudBlobStream)
            {
                (stream as CloudBlobStream).Commit();
            }
        }
        
        
    }
}