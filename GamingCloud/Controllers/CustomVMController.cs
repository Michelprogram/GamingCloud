using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamingCloud.Data;
using GamingCloud.Models;
using GamingCloud.Tools;

namespace GamingCloud.Controllers
{
    public class CustomVMController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomVMController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CustomVM
        public async Task<IActionResult> Index()
        {
            return _context.DbSet != null ? 
                          View(await _context.DbSet.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.DbSet'  is null.");
        }

        // GET: CustomVM/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id == null || _context.DbSet == null)
            {
                return NotFound();
            }

            var customVM = await _context.DbSet
                .FirstOrDefaultAsync(m => m.AutoID == id);
            if (customVM == null)
            {
                return NotFound();
            }

            return View(customVM);
        }

        // GET: CustomVM/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CustomVM/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("login,password")] CustomVM customVM)
        {
            
            if (customVM.IsValid())
            {
                _context.Add(customVM);

                string userName = GetUserName();
                
                VMTools vmTools = new VMTools(userName);

                await vmTools.SetResourceGroupAsync();
                    
                vmTools.CreateVirtualMachine(customVM.login, customVM.password);

                customVM.name = userName + "-vm";
                customVM.active = true;
                customVM.IP = await vmTools.GetPublicIpAddressAsync();
                
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customVM);
        }

        private string GetUserName()
        {
            var user = HttpContext.User.Identity.Name.Equals("") ? "anonymous@gmail.com" : HttpContext.User.Identity.Name;

            return user.Split("@")[0].Replace(".","");
        }
        
        // GET: Change status
        public async Task<IActionResult> Status(int id, bool status)
        {
            if (id == null || _context.DbSet == null)
            {
                return NotFound();
            }

            var customVM = await _context.DbSet.FindAsync(id);
            if (customVM == null)
            {
                return NotFound();
            }

            customVM.active = status;

            VMTools vmTools = new VMTools(GetUserName());
            await vmTools.SetResourceGroupAsync();

            //Active virtual machine or Disable virtual machine
            if (customVM.active)
                await vmTools.EnableAsync();
            else
                await vmTools.DisableAsync();

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: CustomVM/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null || _context.DbSet == null)
            {
                return NotFound();
            }

            var customVM = await _context.DbSet.FindAsync(id);
            if (customVM == null)
            {
                return NotFound();
            }
            return View(customVM);
        }

        // POST: CustomVM/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("login,password")] CustomVM customVM)
        {
            if (id != customVM.AutoID)
            {
                return NotFound();
            }

            if (customVM.IsValid())
            {
                try
                {
                    _context.Update(customVM);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomVMExists(customVM.AutoID))
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
            return View(customVM);
        }

        // GET: CustomVM/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (id == null || _context.DbSet == null)
            {
                return NotFound();
            }

            var customVM = await _context.DbSet
                .FirstOrDefaultAsync(m => m.AutoID == id);
            if (customVM == null)
            {
                return NotFound();
            }

            return View(customVM);
        }

        // POST: CustomVM/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.DbSet == null)
            {
                return Problem("Entity set 'ApplicationDbContext.DbSet'  is null.");
            }
            var customVM = await _context.DbSet.FindAsync(id);
            if (customVM != null)
            {
                _context.DbSet.Remove(customVM);
            }

            var tool = new VMTools(GetUserName());
            
            await tool.RemoveResourceGroupAsync();
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomVMExists(int id)
        {
          return (_context.DbSet?.Any(e => e.AutoID == id)).GetValueOrDefault();
        }
    }
}
