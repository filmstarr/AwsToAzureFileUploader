using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AwsToAzureFileUploader
{
    public class Variables
    {
        private const string _FilePartSizeVariableName = "FilePartSizeMB";
        private const string _FlattenFilePathsVariableName = "FlattenFilePaths";
        private const string _OutputFolderPathVariableName = "OutputFolderPath";
        private const string _AzureAccessKeyVariableName = "AzureAccessKey";
        private const string _StorageAccountVariableName = "StorageAccount";
        private const string _OutputContainerVariableName = "OutputContainer";

        private const int DefaultFilePartSizeMB = 100;
        private const int MinimumFilePartSizeMB = 5;
        private const int MaximumFilePartSizeMB = 100;

        private readonly IDictionary EnvironmentVariables = Environment.GetEnvironmentVariables();
        private readonly int MB = (int)Math.Pow(2, 20);

        public int FilePartSize
        {
            get
            {
                var environmentVariableString = this.GetEnvironmentVariableString(_FilePartSizeVariableName);
                if (int.TryParse(environmentVariableString, out int value))
                {
                    return Math.Min(Math.Max(value, MinimumFilePartSizeMB), MaximumFilePartSizeMB) * MB;
                }
                return DefaultFilePartSizeMB * MB;
            }
        }

        public bool FlattenFilePaths
        {
            get
            {
                var environmentVariableString = this.GetEnvironmentVariableString(_FlattenFilePathsVariableName);
                if (bool.TryParse(environmentVariableString, out bool value))
                {
                    return value;
                }
                return false;
            }
        }

        public string OutputFolderPath
        {
            get
            {
                var environmentVariableString = this.GetEnvironmentVariableString(_OutputFolderPathVariableName);
                if (!string.IsNullOrEmpty(environmentVariableString))
                {
                    var outputfolderPath = environmentVariableString.Replace('\\', '/').TrimStart('/').TrimEnd('/');
                    return outputfolderPath + "/";
                }
                return "";
            }
        }

        public string AzureAccessKey
        {
            get
            {
                var environmentVariableString = this.GetEnvironmentVariableString(_AzureAccessKeyVariableName);
                if (!string.IsNullOrEmpty(environmentVariableString))
                {
                    return environmentVariableString;
                }
                throw new ArgumentException($"No value provided for the required {_AzureAccessKeyVariableName} variable. Unable to proceed.");
            }
        }

        public string StorageAccount
        {
            get
            {
                var environmentVariableString = this.GetEnvironmentVariableString(_StorageAccountVariableName);
                if (!string.IsNullOrEmpty(environmentVariableString))
                {
                    return environmentVariableString;
                }
                throw new ArgumentException($"No value provided for the required {_StorageAccountVariableName} variable. Unable to proceed.");
            }
        }

        public string OutputContainer
        {
            get
            {
                var environmentVariableString = this.GetEnvironmentVariableString(_OutputContainerVariableName);
                if (!string.IsNullOrEmpty(environmentVariableString))
                {
                    return environmentVariableString;
                }
                throw new ArgumentException($"No value provided for the required {_OutputContainerVariableName} variable. Unable to proceed.");
            }
        }

        private string GetEnvironmentVariableString(string key)
        {
            //For some reason Environment.GetEnvironmentVariable is returning random data when variables are empty.
            //Work around this by using Environment.GetEnvironmentVariables instead.
            if (this.EnvironmentVariables.Contains(key))
            {
                var environmentVariable = this.EnvironmentVariables[key].ToString();
                if (!string.IsNullOrWhiteSpace(environmentVariable))
                {
                    return environmentVariable;
                }
            }
            return null;
        }
    }
}
