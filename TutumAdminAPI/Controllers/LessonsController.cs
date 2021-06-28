using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TutumAdminAPI.Controllers.FrequentlyUsed;
using TutumAdminAPI.Models;

namespace TutumAdminAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LessonsController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly ConfigWrapper _config;
        private readonly IConfiguration _configuration;
        private readonly VideoFileHelpers _helper;

        public LessonsController(DatabaseContext context, ConfigWrapper config, IConfiguration configuration, VideoFileHelpers helper)
        {
            _context = context;
            _config = config;
            _configuration = configuration;
            _helper = helper;
        }

        // GET: Lessons
        public async Task<IActionResult> Index()
        {
            var databaseContext = _context.Lessons.Include(l => l.Course);
            return View(await databaseContext.ToListAsync());
        }

        // GET: Lessons/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(m => m.LessonId == id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        // GET: Lessons/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title");

            var vids = await GetVideoCollection();

            ViewData["VideoFile"] = new SelectList(vids, "FileName", "FileName");

            return View();
        }

        private async Task<IEnumerable<VideoViewModel>> GetVideoCollection()
        {
            var client = await AzureHelper.CreateMediaServicesClientAsync(_config);
            var sLocators = await AzureHelper.ListAllAssets(client, _config.ResourceGroup, _config.AccountName);

            var videoVMs = new List<VideoViewModel>();
            foreach (var sLocator in sLocators)
            {
                videoVMs.Add(await _helper.ModelFromLocatorAsync(sLocator, client));
            }
            return videoVMs;
        }

        // POST: Lessons/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LessonId,CourseId,Title,Text,VideoFileName")] Lesson lesson)
        {
            if (ModelState.IsValid)
            {
                var client = await AzureHelper.CreateMediaServicesClientAsync(_config);
                var videoVM = await _helper.ModelFromAssetName(lesson.VideoFileName);

                lesson.PreviewPath = videoVM.PreviewPath;
                lesson.VideoPath = videoVM.VideoPath;

                _context.Add(lesson);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title", lesson.CourseId);

            var vids = await GetVideoCollection();

            ViewData["VideoFile"] = new SelectList(vids, "FileName", "FileName", lesson.VideoFileName);
            return View(lesson);
        }

        // GET: Lessons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                return NotFound();
            }

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Title");

            var vids = await GetVideoCollection();

            ViewData["VideoFile"] = new SelectList(vids, "FileName", "FileName");

            //Выбираем имя того файла, у которого тот же путь
            lesson.VideoFileName = vids.FirstOrDefault(vid => vid.VideoPath == lesson.VideoPath).FileName;

            return View(lesson);
        }

        // POST: Lessons/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LessonId,CourseId,Title,Text,VideoPath,PreviewPath,VideoFileName")] Lesson lesson)
        {
            if (id != lesson.LessonId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var client = await AzureHelper.CreateMediaServicesClientAsync(_config);
                    var videoVM = await _helper.ModelFromAssetName(lesson.VideoFileName);

                    lesson.PreviewPath = videoVM.PreviewPath;
                    lesson.VideoPath = videoVM.VideoPath;

                    _context.Update(lesson);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LessonExists(lesson.LessonId))
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

            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "Description", lesson.CourseId);
            var vids = await GetVideoCollection();
            ViewData["VideoFile"] = new SelectList(vids, "FileName", "FileName");

            return View(lesson);
        }

        // GET: Lessons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(m => m.LessonId == id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        // POST: Lessons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.LessonId == id);
        }
    }
}
