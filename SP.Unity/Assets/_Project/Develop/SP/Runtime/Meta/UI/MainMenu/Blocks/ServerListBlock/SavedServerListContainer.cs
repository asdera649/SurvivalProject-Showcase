using System;
using System.Collections.Generic;
using CI.QuickSave;
using CI.QuickSave.Core.Storage;
using UnityEngine;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.ServerListBlock
{
    public class SavedServerListContainer
    {
        private readonly List<string> _visitedServersNameHistory = new();
        public IEnumerable<string> VisitedServersNameHistory => _visitedServersNameHistory;
        
        private readonly List<string> _favoriteServersNameHistory = new();
        public IEnumerable<string> FavoriteServersNameHistory => _favoriteServersNameHistory;
        
        private const string _serversHistoryFileName = "VisitedServersHistory";
        private const string _favoriteServersFileName = "FavoriteServersHistory";

        private bool _isInitialized;
        
        public void Initialize()
        {
            Load();

            _isInitialized = true;
        }

        private void Load()
        {
            if (FileAccess.Exists(_serversHistoryFileName, false))
            {
                var reader = QuickSaveReader.Create(_serversHistoryFileName);

                foreach (var k in reader.GetAllKeys())
                {
                    if (reader.TryRead<string>(k, out var result))
                    {
                        _visitedServersNameHistory.Add(result);
                    }
                }
            }

            if (FileAccess.Exists(_favoriteServersFileName, false))
            {
                var reader = QuickSaveReader.Create(_favoriteServersFileName);

                foreach (var k in reader.GetAllKeys())
                {
                    if (reader.TryRead<string>(k, out var result))
                    {
                        _favoriteServersNameHistory.Add(result);
                    }
                }
            }
        }

        public void AddToHistory(string serverName)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("Before calling this method, you need to call Initialize()!");
                return;
            }

            if (_visitedServersNameHistory.Contains(serverName))
            {
                _visitedServersNameHistory.Remove(serverName);
            }
            
            _visitedServersNameHistory.Insert(0, serverName);

            SaveVisitedServers();
        }

        public void AddToFavorite(string serverName)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("Before calling this method, you need to call Initialize()!");
                return;
            }
            
            if (_favoriteServersNameHistory.Contains(serverName))
            {
                _favoriteServersNameHistory.Remove(serverName);
            }
            
            _favoriteServersNameHistory.Insert(0, serverName);
            
            SaveFavoriteServers();
        }
        
        public void RemoveFromFavorite(string serverName)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("Before calling this method, you need to call Initialize()!");
                return;
            }
            
            if (_favoriteServersNameHistory.Contains(serverName))
            {
                _favoriteServersNameHistory.Remove(serverName);
            }
            
            SaveFavoriteServers();
        }

        private void SaveVisitedServers()
        {
            var writer = QuickSaveWriter.Create(_serversHistoryFileName);

            foreach (var k in writer.GetAllKeys())
            {
                writer.Delete(k);
            }

            foreach (var n in _visitedServersNameHistory)
            {
                writer.Write(Guid.NewGuid().ToString(), n);
            }
            
            writer.Commit();
        }
        
        private void SaveFavoriteServers()
        {
            var writer = QuickSaveWriter.Create(_favoriteServersFileName);

            foreach (var k in writer.GetAllKeys())
            {
                writer.Delete(k);
            }

            foreach (var n in _favoriteServersNameHistory)
            {
                writer.Write(Guid.NewGuid().ToString(), n);
            }
            
            writer.Commit();
        }
    }
}