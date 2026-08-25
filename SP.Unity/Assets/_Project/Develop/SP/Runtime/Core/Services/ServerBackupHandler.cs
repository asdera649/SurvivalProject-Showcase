using System;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using CI.QuickSave;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SP.Runtime.Bootstrap.Services.BackendInteractionService;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.LoadingService;
using UnityEngine;
using FileAccess = CI.QuickSave.Core.Storage.FileAccess;

namespace SP.Runtime.Core.Services
{
    [UsedImplicitly]
    public class ServerBackupHandler : ILoadUnit, IDisposable
    {
        public ServerBackupHandler(BackendInteractionService backend, SaveService.SaveService saveService)
        {
            _backendInteractionService = backend;
            _saveService = saveService;
        }

        private BackendInteractionService.ServerRegistrationData _serverRegistrationData;

        private bool _isUnloadingSave;
        
        private readonly BackendInteractionService _backendInteractionService;
        private readonly SaveService.SaveService _saveService;
        
        public async UniTask Load()
        {
            var isLog = true;
            
            start:
            
            if (isLog)
            {
                Debug.Log("[ServerBackupHandler]: Server registration...");
            }
            
            var result = await _backendInteractionService.RegisterServer();
            
            if (result.RequestResult != BackendInteractionService.RequestResult.Successful)
            {
                if (isLog)
                {
                    Debug.LogWarning("[ServerBackupHandler]: Failed to register the server. " +
                                  "Let's keep trying, in case of success a message will be displayed...");
                }
            
                isLog = false;
            
                await UniTask.Delay(5000);
                
                goto start;
            }
            
            _serverRegistrationData = result.ServerRegistrationData;
            
            Debug.Log("Successful server registration.");
            
            await LoadSave();
            
            _saveService.Saved += OnSaved;
        }

        public void Dispose()
        {
            _saveService.Saved -= OnSaved;
        }
        
        private async UniTask LoadSave()
        {
            var isLog = true;
            
            FileAccess.Files(true); // Это для того что бы создать директорию.
            
            _saveService.ClearSave();
            
            start:

            if (isLog)
            {
                Debug.Log("[ServerBackupHandler]: Start loading a save...");
            }
            
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_serverRegistrationData.EndPoint}",
                ForcePathStyle = true // Обязательно для некоторых S3-совместимых хранилищ
            };

            using (var client = new AmazonS3Client(
                       _serverRegistrationData.AccessKey,
                       _serverRegistrationData.SecretKey,
                       config))
            {
                try
                {
                    if (!await CheckIfKeyExists(
                            client,
                            _serverRegistrationData.BucketName,
                            _serverRegistrationData.ObjectKey))
                    {
                        Debug.LogWarning("[ServerBackupHandler]: Failed to load save," +
                                         " the specified key does not exist in S3");
                        return;
                    }
                    
                    var request = new GetObjectRequest
                    {
                        BucketName = _serverRegistrationData.BucketName,
                        Key = _serverRegistrationData.ObjectKey
                    };

                    using var response = await client.GetObjectAsync(request);
                    
                    var savePath = Path.Combine(
                        Path.Combine(QuickSaveGlobalSettings.StorageLocation, "QuickSave"),
                        BaseSaveService.FileName + ".json");
                        
                    await response.WriteResponseStreamToFileAsync(savePath, true, default);
                }
                catch (AmazonS3Exception e)
                {
                    if (isLog)
                    {
                        Debug.LogWarning($"[ServerBackupHandler]: Failed to load save, S3 exception: {e.Message}. " +
                                         "Let's keep trying, in case of success a message will be displayed...");
                    }

                    isLog = false;

                    await UniTask.Delay(5000);
                    
                    goto start;
                }
                catch (Exception e)
                {
                    if (isLog)
                    {
                        Debug.LogWarning($"[ServerBackupHandler]: Failed to load save, exception: {e.Message}. " +
                                         "Let's keep trying, in case of success a message will be displayed...");
                    }

                    isLog = false;
                    
                    await UniTask.Delay(5000);
                    
                    goto start;
                }
            }
            
            Debug.Log("[ServerBackupHandler]: Save successfully loaded.");
        }

        private async UniTask<bool> CheckIfKeyExists(AmazonS3Client client, string bucketName, string key)
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = key,
                    MaxKeys = 1
                };

                var response = await client.ListObjectsV2Async(request);
                
                return response.S3Objects.Count > 0 && response.S3Objects[0].Key == key;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        private async UniTask UnloadSave()
        {
            _isUnloadingSave = true;
            
            var isLog = true;
            
            start:

            if (isLog)
            {
                Debug.Log("[ServerBackupHandler]: Start unloading a save...");
            }
            
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_serverRegistrationData.EndPoint}",
                ForcePathStyle = true
            };

            using (var client = new AmazonS3Client(
                       _serverRegistrationData.AccessKey,
                       _serverRegistrationData.SecretKey,
                       config))
            {
                try
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _serverRegistrationData.BucketName,
                        Key = _serverRegistrationData.ObjectKey,
                        FilePath = Path.Combine(
                            Path.Combine(QuickSaveGlobalSettings.StorageLocation, "QuickSave"),
                            BaseSaveService.FileName + ".json"),
                        ContentType = "application/octet-stream"
                    };
                    
                    await client.PutObjectAsync(putRequest);
                }
                catch (AmazonS3Exception e)
                {
                    if (isLog)
                    {
                        Debug.LogWarning($"[ServerBackupHandler]: Failed to unload save, S3 exception: {e.Message}. " +
                                         "Let's keep trying, in case of success a message will be displayed...");
                    }

                    isLog = false;
                    
                    await UniTask.Delay(5000);
                    
                    goto start;
                }
                catch (Exception e)
                {
                    if (isLog)
                    {
                        Debug.LogWarning($"[ServerBackupHandler]: Failed to unload save, exception: {e.Message}. " +
                                         "Let's keep trying, in case of success a message will be displayed...");
                    }

                    isLog = false;
                    
                    await UniTask.Delay(5000);
                    
                    goto start;
                }
            }

            _isUnloadingSave = false;
            
            Debug.Log("[ServerBackupHandler]: Save successfully unloaded.");
        }
        
        private async void OnSaved()
        {
            if (_isUnloadingSave)
            {
                return;
            }
            
            await UnloadSave();
        }
    }
}