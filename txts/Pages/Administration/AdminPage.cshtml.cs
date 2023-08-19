﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using txts.Database;
using txts.Pages.Layouts;
using txts.Types.Entities;

namespace txts.Pages.Administration;

public class AdminPage : PageLayout
{
    public AdminPage(DatabaseContext database) : base(database)
    { }

    public List<PageEntity> Pages { get; set; } = null!;
    public List<BanEntity> Bans { get; set; } = null!;

    public string? Callback { get; set; }

    public async Task<IActionResult> OnGet([FromQuery] string? callback, [FromQuery] string? search, [FromQuery] string action, [FromQuery] int id)
    {
        AdminUserEntity? adminUser = await this.Database.UserFromWebRequest(this.Request);
        if (adminUser == null) return this.Redirect("/admin/login");

        if (search != null)
        {
            this.Pages = await this.Database.Pages.Where(p => p.Username.Contains(search))
                .OrderByDescending(p => p.PageId)
                .Take(20)
                .ToListAsync();
            this.Bans = await this.Database.Bans.Where(b => b.Page.Username.Contains(search))
                .OrderByDescending(p => p.PageId)
                .Take(20)
                .ToListAsync();
        }
        else
        {
            this.Pages = await this.Database.Pages.OrderByDescending(p => p.PageId).Take(20).ToListAsync();
            this.Bans = await this.Database.Bans.OrderByDescending(p => p.PageId).Take(20).ToListAsync();
        }

        if (callback != null) this.Callback = callback;

        switch (action)
        {
            case "ban":
            {
                PageEntity? page = await this.Database.Pages.FirstOrDefaultAsync(p => p.PageId == id);

                if (page == null) return this.NotFound();

                BanEntity ban = new()
                {
                    PageId = page.PageId,
                    Reason = "Banned for violating site rules.",
                };

                page.IsBanned = true;
                await this.Database.Bans.AddAsync(ban);

                await this.Database.SaveChangesAsync();
                return this.Redirect("/admin?callback=ban");
            }
            case "unban":
            {
                PageEntity? page = await this.Database.Pages.FirstOrDefaultAsync(p => p.PageId == id);
                BanEntity? ban = await this.Database.Bans.FirstOrDefaultAsync(b => b.PageId == id);

                if (page == null || ban == null) return this.NotFound();

                page.IsBanned = false;
                this.Database.Bans.Remove(ban);

                await this.Database.SaveChangesAsync();
                return this.Redirect("/admin?callback=unban");
            }
            case "cleanSessions":
            {
                this.Database.WebSessions.RemoveRange(this.Database.WebSessions);
                await this.Database.SaveChangesAsync();

                return this.Redirect("/admin/login?callback=cleanSessions");
            }
        }

        return this.Page();
    }
}