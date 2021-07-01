using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TutumAdminAPI;
using TutumAdminAPI.Models;

namespace TutumAdminAPI.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly DatabaseContext _context;

        public UsersController(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index() 
        {
            return View(await _context.Users.Include(user => user.Subscription).ToListAsync());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleSubscription(int id)
        {
            var user = _context.Users.Include(user => user.Subscription)
                                    .FirstOrDefault(user => user.UserId == id);

            if (user.HasSubscription)
            {
                _context.Subscriptions.Remove(user.Subscription);
            }
            else
            {
                user.Subscription = new Subscription
                {
                    ActivationDate = DateTime.UtcNow.Date,
                    Expires = DateTime.UtcNow.AddMonths(1).Date
                };
            }

            _context.SaveChanges();

            return Ok();
        }
    }
}
