using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TutumAdminAPI.Models;

namespace TutumAdminAPI.Controllers.FrequentlyUsed
{
    public class VideoFileHelpers
    {
        private readonly ConfigWrapper _config;

        public VideoFileHelpers(ConfigWrapper config) 
        {
            _config = config;
        }

        public async Task<VideoViewModel> ModelFromLocatorAsync(StreamingLocator input, IAzureMediaServicesClient _client)
        {
            var paths = await _client.StreamingLocators.ListPathsAsync(_config.ResourceGroup, _config.AccountName, input.Name);
            return ModelFromLocatorAsync(paths, input.AssetName);
        }

        public async Task<VideoViewModel> ModelFromLocatorAsync(AssetStreamingLocator input, IAzureMediaServicesClient _client)
        {
            var paths = await _client.StreamingLocators.ListPathsAsync(_config.ResourceGroup, _config.AccountName, input.Name);
            return ModelFromLocatorAsync(paths, input.AssetName);
        }

        private VideoViewModel ModelFromLocatorAsync(ListPathsResponse paths, string assetName)
        {
            var newVM = new VideoViewModel
            {
                FileName = assetName,
                PreviewPath = paths.DownloadPaths.FirstOrDefault(path => path.EndsWith(".jpg")),
                VideoPath = paths.DownloadPaths.FirstOrDefault(path => path.EndsWith(".mp4"))
            };
            return newVM;
        }

        public async Task<VideoViewModel> ModelFromAssetName(string assetName)
        {
            var client = await AzureHelper.CreateMediaServicesClientAsync(_config);
            var result = await client.Assets.ListStreamingLocatorsAsync(_config.ResourceGroup, _config.AccountName, assetName);

            if (!result.StreamingLocators.Any())
            {
                return null;
            }

            var sLocator = result.StreamingLocators.First();
            var videoViewModel = await ModelFromLocatorAsync(sLocator, client);
            return videoViewModel;
        }
    }
}
