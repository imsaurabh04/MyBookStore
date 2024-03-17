using BulkyWeb.DataAccess.Data;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Models.ViewModels;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MyBookStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult RoleManagement(string userId)
        {

            RoleManagementVM roleManagementVM = new()
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u=>u.Id==userId, includeProperties: "Company"),
                CompanyList = _unitOfWork.Company.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                RoleList = _roleManager.Roles.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Name
                })

            };

            roleManagementVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser
                .Get(u=>u.Id==userId)).GetAwaiter().GetResult().FirstOrDefault();

            return View(roleManagementVM);
        }


        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
        {

            var oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u=>u.Id==roleManagementVM.ApplicationUser.Id))
                .GetAwaiter().GetResult().FirstOrDefault();

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser
                .Get(u => u.Id == roleManagementVM.ApplicationUser.Id);

            if (roleManagementVM.ApplicationUser.Role !=  oldRole)
            {
                // role was updated

                if(roleManagementVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                }
                if(oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();

                TempData["success"] = "User role updated successfully!";
            }
            else
            {
                if(roleManagementVM.ApplicationUser.Role==SD.Role_Company && 
                    roleManagementVM.ApplicationUser.CompanyId!=applicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUser.Update(applicationUser);
                    _unitOfWork.Save(); 
                }
            }

            return RedirectToAction(nameof(Index));
        }




        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _unitOfWork.ApplicationUser
                .GetAll(includeProperties: "Company").ToList();

            foreach (var user in objUserList)
            {
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
            }

            return Json(new { data = objUserList });
        }


        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
            if(userFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking!" });
            }

            if(userFromDb.LockoutEnd!=null && userFromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock them
                userFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                userFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }

            _unitOfWork.ApplicationUser.Update(userFromDb);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Operation successful!" });
        }

        #endregion

    }
}
