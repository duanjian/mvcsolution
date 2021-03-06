﻿using System;
using System.Collections.Generic;
using System.Linq;
using MvcSolution.Data.Entities;
using MvcSolution.Infrastructure;

namespace MvcSolution.Services.Admin
{
    public class UserService : ServiceBase, IUserService
    {
        public List<ManageUserListDto> GetManageUserListDtos(Guid? departmentId, string role, string name)
        {
            using (var db = base.NewDB())
            {
                return db.Users.WhereByDepartment(departmentId)
                    .WhereByRole(role)
                    .WhereByName(name)
                    .ToManageUserListDtos()
                    .OrderBy(x => x.Name)
                    .ToList()
                    .BuildRoles(db);
            }
        }

        public Guid? GetDepartmentId(Guid userId)
        {
            using (var db = base.NewDB())
            {
                return db.Users
                    .Where(x => x.Id == userId)
                    .Select(x => x.DepartmentId)
                    .FirstOrDefault();
            }
        }

        public string GetDepartmentName(Guid userId)
        {
            using (var db = base.NewDB())
            {
                return db.Users
                    .Where(x => x.Id == userId)
                    .Select(x => x.Department.Name)
                    .FirstOrDefault();
            }
        }

        public void Save(User user, string[] roles)
        {
            using (var db = base.NewDB())
            {
                if (user.IsNew)
                {
                    if (db.Users.Duplicates(null, user.Username))
                    {
                        throw new KnownException("用户名已经存在了。");
                    }
                    user.NewId();
                    user.Password = CryptoService.MD5Encrypt(user.Password);
                    if (roles != null && roles.Length > 0)
                    {
                        var dbRoles = db.Roles.Where(x => roles.Contains(x.Name)).ToList();
                        user.UserRoleRls = new List<UserRoleRL>();
                        foreach (var role in dbRoles)
                        {
                            db.UserRoleRls.Add(new UserRoleRL(user.Id, role.Id));
                        }
                    }
                    db.Users.Add(user);
                }
                else
                {
                    if (db.Users.Duplicates(user.Id, user.Username))
                    {
                        throw new KnownException("用户名已经存在了。");
                    }
                    var dbUser = db.Users.Get(user.Id);
                    dbUser.Update(user);
                    if (!string.IsNullOrWhiteSpace(user.Password))
                    {
                        dbUser.Password = CryptoService.MD5Encrypt(user.Password);
                    }
                    dbUser.UserRoleRls.ToList().ForEach(x => db.UserRoleRls.Remove(x));
                    if (roles != null && roles.Length > 0)
                    {
                        var dbRoles = db.Roles.Where(x => roles.Contains(x.Name)).ToList();
                        foreach (var role in dbRoles)
                        {
                            db.UserRoleRls.Add(new UserRoleRL(user.Id, role.Id));
                        }
                    }
                }
                db.SaveChanges();
            }
        }
    }
}
