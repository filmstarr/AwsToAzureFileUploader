using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.Storage.Auth;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AwsToAzureFileUploader
{
    public class Function
    {
        private readonly Variables variables = new Variables();
        private readonly Logger logger = new Logger();

        private IAmazonS3 S3Client { get; set; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            this.S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }
        
        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            //Set logging context
            this.logger.Context = context;

            var s3Event = evnt.Records?[0].S3;
            if(s3Event == null)
            {
                return null;
            }

            //Decode object key from event
            var s3EventObjectKey = Uri.UnescapeDataString(s3Event.Object.Key);

            try
            {
                logger.LogLineWithId($"S3 event received. Uploading object to Azure: {s3Event.Bucket.Name}/{s3EventObjectKey}");
                var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3EventObjectKey);
                await UploadToAzure(s3Event.Bucket.Name, s3EventObjectKey);
                return response.Headers.ContentType;
            }
            catch(Exception e)
            {
                logger.LogLineWithId($"Error uploading object {s3EventObjectKey} in bucket {s3Event.Bucket.Name} to Azure");
                logger.LogLine(e.Message);
                logger.LogLine(e.StackTrace);
                throw;
            }
        }

        private async Task UploadToAzure(string inputBucketName, string inputKey)
        {
            //Retrieve input stream
            var obj = await S3Client.GetObjectAsync(new GetObjectRequest { BucketName = inputBucketName, Key = inputKey });
            var inputStream = obj.ResponseStream;
            logger.LogLineWithId("Object response stream obtained");

            //Initiate multipart upload
            string outputContainer = this.variables.OutputContainer;
            string outputKey = this.GetOutputKey(inputKey);
            StorageCredentials credentials = new StorageCredentials(this.variables.StorageAccount, this.variables.AzureAccessKey);
            CloudStorageAccount storageAccount = new CloudStorageAccount(credentials, useHttps: true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(outputContainer);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(outputKey);
            List<string> blockIDs = new List<string>();
            logger.LogLineWithId($"Upload initiated. Output object: {outputContainer}/{outputKey}");

            var partNumber = 1;
            var filePartSize = this.variables.FilePartSize;

            //Process input file
            while (true)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    //Copy a set number of bytes from the input stream to the memory stream
                    var bytesCopied = CopyBytes(filePartSize, inputStream, memoryStream);
                    if (bytesCopied == 0)
                    {
                        break;
                    }
                    logger.LogLineWithId($"Part {partNumber} read");

                    //Create a block ID from the part number
                    string blockId = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("BlockId{0}", partNumber.ToString("0000000"))));
                    blockIDs.Add(blockId);

                    //Create a block hash from the stream, remembering to rewind the stream back to the start
                    memoryStream.Position = 0;
                    string blockHash;
                    using (MD5 md5 = MD5.Create())
                    {
                        byte[] hash = md5.ComputeHash(memoryStream);
                        blockHash = Convert.ToBase64String(hash, 0, 16);
                    }

                    //Upload memory stream to Azure
                    memoryStream.Position = 0;
                    await blockBlob.PutBlockAsync(blockId, memoryStream, blockHash);
                    logger.LogLineWithId($"Part {partNumber} uploaded");

                    partNumber++;
                }

                //Run the garbage collector so that Lambda doesn't run out of memory for large files
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            //Complete multipart upload request
            logger.LogLineWithId($"Completing upload");
            await blockBlob.PutBlockListAsync(blockIDs);
            logger.LogLineWithId($"Upload completed");

            inputStream.Dispose();
        }

        private string GetOutputKey(string key)
        {
            var outputKey = key;

            if (this.variables.FlattenFilePaths)
            {
                outputKey = outputKey.Substring(outputKey.IndexOf('/') + 1);
            }
            return this.variables.OutputFolderPath + outputKey;
        }

        private static long CopyBytes(long bytesToRead, Stream fromStream, Stream toStream)
        {
            var buffer = new byte[81920];
            long bytesRead = 0;
            int read;
            while ((read = fromStream.Read(buffer, 0, (int)Math.Min((bytesToRead - bytesRead), buffer.Length))) != 0)
            {
                bytesRead += read;
                toStream.Write(buffer, 0, read);
            }
            return bytesRead;
        }
    }
}
