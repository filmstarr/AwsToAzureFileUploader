# AwsToAzureFileUploader

A .NET Core, AWS Lambda function to upload files from S3 to Azure Blob storage.

This AWS Lambda function can be configured to listen to S3 object creation events (Put, Post, Copy, Complete Multipart Upload) and will upload a version of the file to the specified location on Azure.

AwsToAzureFileUploader has been build to memory efficient, streaming and writing data via multipart upload in a continuous process. As such, large files can be written without the need to read the whole file into memory, and memory usage can be tightly constrained.

## Prerequisits & Configuration
The prerequisits and configuration of this Lambda function are no different to any other. This project contains a set of default configuration values in the 
[aws-lambda-tools-defaults.json file](AwsToAzureFileUploader/aws-lambda-tools-defaults.json), which define the region the AwsToAzureFileUploader function is to run in, the length of time that the function can run for, the maximum memory that it can use, the IAM role under which the function is to run and the function variables (as detailed below). These should be changed to suitable values for your requirements and production enviroment.

The IAM role used will need to have a policy which can get/put objects from/to the S3 bucket(s) that you wish to compress files in, as well as suitable CloudWatch permissions. The AWSLambdaExecute managed policy is a good place to start:

```
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:*"
      ],
      "Resource": "arn:aws:logs:*:*:*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject"
      ],
      "Resource": "arn:aws:s3:::*"
    }
  ]
}
```


## Deployment

Deployment of the AwsToAzureFileUploader function can be done through the AWS CLI, the AWS Toolkit for Visual Studio or through the AWS web interface. There are again no special requirements for this function compared to any other.

Once the Lambda function has been created the trigger needs to be configured, either via the AWS S3 CLI or through the web interface. Specify the bucket you want the function to apply to, the event types to trigger the function (e.g. "Object Created (All)") and any prefix or suffix filters.


## Function Variables

AwsToAzureFileUploader has two variables that can be set when deploying the function:
* FilePartSizeMB - the part size, in MB, that is read from the input file and uploaded to Azure. The value must be between 5MB and 100MB.
* OutputFolderPath - write all files into this this top level folder within the container. Leave this empty to write back to the same folder.
* FlattenFilePaths - flatten all file paths within the container, or within the output folder if it's specified, when uploading the file. E.g. my.bucket/folder1/folder2/file.txt would be written to my.bucket/file.txt.gz
* AzureAccessKey - the Azure Storage Account Access key. This is a required variable.
* StorageAccount - the Azure Storage Account name to upload the file to. This is a required variable.
* OutputContainer - the Azure Container name to upload the file to. This is a required variable.
