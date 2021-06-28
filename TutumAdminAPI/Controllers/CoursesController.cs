using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TutumAdminAPI.Models;

namespace TutumAdminAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CoursesController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly IConfiguration _config;

        public CoursesController(DatabaseContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            return View(await _context.Courses.ToListAsync());
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseId,Title,Description,PreviewFile,IsPremiumOnly")] Course course)
        {
            if (ModelState.IsValid)
            {
                if (course.PreviewFile == null)
                {
                    ModelState.AddModelError("PreviewFile", "Файл отсутствует");
                    return BadRequest(ModelState);
                }
                if (course.PreviewFile.Length <= 0)
                {
                    ModelState.AddModelError("PreviewFile", "Ошибочный размер файла");
                    return BadRequest(ModelState); 
                }
                if (!course.PreviewFile.ContentType.Contains("image"))
                {
                    ModelState.AddModelError("PreviewFile", "Файл не является изображением");
                    return BadRequest(ModelState); 
                }

                course.PreviewPath = await UploadPreviewToAzure(course.PreviewFile);

                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseId,Title,Description,PreviewFile,PreviewPath,IsPremiumOnly")] Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (course.PreviewFile != null)
                    {
                        if (course.PreviewFile.Length <= 0)
                        {
                            ModelState.AddModelError("PreviewFile", "Ошибочный размер файла");
                            return BadRequest(ModelState);
                        }
                        if (!course.PreviewFile.ContentType.Contains("image"))
                        {
                            ModelState.AddModelError("PreviewFile", "Файл не является изображением");
                            return BadRequest(ModelState);
                        }
                        course.PreviewPath = await UploadPreviewToAzure(course.PreviewFile);

                        var oldPreview = _context.Courses.AsNoTracking().FirstOrDefault(c => c.CourseId == id).PreviewPath;
                        await RemovePreviewFromBlob(oldPreview);
                    }

                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            await RemovePreviewFromBlob(course.PreviewPath);

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }

        public async Task<string> UploadPreviewToAzure(IFormFile uploadedFile)
        {
            // путь к папке Files, ЗАМЕНИТЬ Path.GetTempFileName на более надежный генератор
            string newFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".jpg";

            await UploadToAzure(newFileName, uploadedFile.OpenReadStream());

            return newFileName;
        }

        private async Task UploadToAzure(string blobName, Stream stream)
        {
            // Construct the blob container endpoint from the arguments.
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/images/",
                                                        _config["BlobStorageName"]);

            // Get a credential and create a client object for the blob container.
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                                                new DefaultAzureCredential());

            await containerClient.UploadBlobAsync(blobName, stream);
        }

        private async Task RemovePreviewFromBlob(string imageName) 
        {
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/images/",
                                                           _config["BlobStorageName"]);

            // Get a credential and create a client object for the blob container.
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                                                new DefaultAzureCredential());

            await containerClient.DeleteBlobAsync(imageName);
        }
    }
}
