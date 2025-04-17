// GymBranchController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagement.Data;
using GymManagement.Models;

[Authorize(Roles = "Admin")]  // 仅管理员可访问
public class GymBranchController : Controller
{
  private readonly AppDbContext _dbContext;

  public GymBranchController(AppDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  // 🔹 查看所有健身房分店
  public IActionResult Index()
  {
    var branches = _dbContext.GymBranches.ToList();
    return View(branches);
  }

  // 🔹 查看单个分店详情
  public IActionResult Details(int id)
  {
    var branch = _dbContext.GymBranches
        .Include(b => b.Trainers)
        .Include(b => b.Receptionists)
        .Include(b => b.Rooms)
        .FirstOrDefault(b => b.BranchId == id);

    if (branch == null)
    {
      return NotFound("Gym branch not found.");
    }

    return View(branch);
  }

  // 🔹 创建分店
  [HttpGet]
  public IActionResult Create()
  {
    return View();
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public IActionResult Create(GymBranch branch)
  {
    if (ModelState.IsValid)
    {
      _dbContext.GymBranches.Add(branch);
      _dbContext.SaveChanges();
      return RedirectToAction("Index");
    }

    return View(branch);
  }

  // 🔹 修改分店信息
  [HttpGet]
  public IActionResult Edit(int id)
  {
    var branch = _dbContext.GymBranches.FirstOrDefault(b => b.BranchId == id);
    if (branch == null)
    {
      return NotFound("Gym branch not found.");
    }

    return View(branch);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public IActionResult Edit(GymBranch updatedBranch)
  {
    var branch = _dbContext.GymBranches.FirstOrDefault(b => b.BranchId == updatedBranch.BranchId);
    if (branch == null)
    {
      return NotFound("Gym branch not found.");
    }

    branch.BranchName = updatedBranch.BranchName;
    branch.Address = updatedBranch.Address;
    branch.ContactNumber = updatedBranch.ContactNumber;

    _dbContext.SaveChanges();
    return RedirectToAction("Index");
  }

  // 🔹 删除分店
  [HttpPost]
  [ValidateAntiForgeryToken]
  public IActionResult Delete(int id)
  {
    var branch = _dbContext.GymBranches
        .Include(b => b.Trainers)
        .Include(b => b.Receptionists)
        .Include(b => b.Rooms)
        .FirstOrDefault(b => b.BranchId == id);

    if (branch == null)
    {
      return NotFound("Gym branch not found.");
    }

    // 确保没有 Trainer、Receptionist 或 Room 依赖此分店
    if (branch.Trainers.Any() || branch.Receptionists.Any() || branch.Rooms.Any())
    {
      ViewBag.Error = "Cannot delete a branch that has associated trainers, receptionists, or rooms.";
      return RedirectToAction("Index");
    }

    _dbContext.GymBranches.Remove(branch);
    _dbContext.SaveChanges();
    return RedirectToAction("Index");
  }
}
