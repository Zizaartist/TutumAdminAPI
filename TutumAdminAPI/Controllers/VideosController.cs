using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TutumAdminAPI.Controllers.FrequentlyUsed;
using TutumAdminAPI.Models;

namespace TutumAdminAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VideosController : Controller
    {
        private readonly ConfigWrapper _config;
        private readonly IConfiguration _configuration;
        private readonly VideoFileHelpers _helper;

        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly long _fileSizeLimit = 100000000000;
        private string fileName;
        private string extension;

        public VideosController(ConfigWrapper config, IConfiguration configuration, VideoFileHelpers helper)
        {
            _config = config;
            _configuration = configuration;
            _helper = helper;
        }

        // GET: Videos
        public async Task<IActionResult> Index()
        {
            var client = await AzureHelper.CreateMediaServicesClientAsync(_config);
            var sLocators = await AzureHelper.ListAllAssets(client, _config.ResourceGroup, _config.AccountName);

            var videoVMs = new List<VideoViewModel>();
            foreach (var sLocator in sLocators)
            {
                videoVMs.Add(await _helper.ModelFromLocatorAsync(sLocator, client));
            }

            return View(videoVMs);
        }

        // GET: Videos/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound(); 
            }

            var videoViewModel = await _helper.ModelFromAssetName(id);

            if (videoViewModel == null) 
            {
                return NotFound();
            }

            return View(videoViewModel);
        }

        // GET: Videos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Videos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> CreateBigFile()
        {
            //Какая-то проверка
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            //Проверяем размер и тип файла из content header-a запроса
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            //Создаем читателя и читаем 1й фрагмент
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            MultipartSection section = await reader.ReadNextSectionAsync();

            List<string> urls = new List<string>();

            //Читаем пока не кончится
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        ModelState.AddModelError("File",
                            $"The request couldn't be processed (Error 2).");
                        // Log error

                        return BadRequest(ModelState);
                    }
                    else
                    {
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                contentDisposition.FileName.Value);
                        extension = Path.GetExtension(contentDisposition.FileName.Value).ToLowerInvariant();
                        var trustedFileNameForFileStorage = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + extension; //Удобно, уже предусмотрели :)
                        fileName = trustedFileNameForFileStorage;

                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState, _fileSizeLimit);

                        ModelState.Values.SelectMany(e => e.Errors).ToList().ForEach(e => Debug.WriteLine(e.ErrorMessage));
                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        var client = await AzureHelper.CreateMediaServicesClientAsync(_config);
                        //Отправляем поток файла-результата в облачное хранилище
                        using (var memStream = new MemoryStream(streamedFileContent))
                        {
                            await AzureUpload(trustedFileNameForFileStorage, memStream);
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }
            return Ok();
        }

        // GET: Videos/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var videoViewModel = await _helper.ModelFromAssetName(id);

            if (videoViewModel == null)
            {
                return NotFound();
            }

            return View(videoViewModel);
        }

        // GET: Videos/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var videoViewModel = await _helper.ModelFromAssetName(id);

            if (videoViewModel == null)
            {
                return NotFound();
            }

            return View(videoViewModel);
        }

        // POST: Videos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var client = await AzureHelper.CreateMediaServicesClientAsync(_config);
            await client.Assets.DeleteAsync(_config.ResourceGroup, _config.AccountName, id);

            return RedirectToAction(nameof(Index));
        }

        private async Task AzureUpload(string fileName, MemoryStream stream)
        {
            //Отправляем полученный файл в blob
            string resourceGroup = _configuration["ResourceGroup"];
            string accountName = _configuration["AccountName"];
            string transformName = _configuration["VideoEncoderName"];

            var client = await AzureHelper.CreateMediaServicesClientAsync(_config);

            var inputAsset = await AzureHelper.CreateInputAssetAsync(client, resourceGroup, accountName, fileName, stream);
            var outputAsset = await AzureHelper.CreateOutputAssetAsync(client, resourceGroup, accountName, $"{fileName}output");

            var transform = await AzureHelper.GetOrCreateTransformAsync(client, resourceGroup, accountName, transformName);

            await AzureHelper.SubmitJobAsync(client, resourceGroup, accountName, transform.Name, $"{fileName}encoding", inputAsset.Name, outputAsset.Name);
        }
    }
}
