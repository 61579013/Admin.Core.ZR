﻿using Infrastructure.Attribute;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using ZR.Model.System.Dto;
using ZR.Model.System;

namespace ZR.Repository.System
{
    /// <summary>
    /// 系统菜单
    /// </summary>
    [AppService(ServiceLifetime = LifeTime.Transient)]
    public class SysMenuRepository : BaseRepository<SysMenu>
    {
        /// <summary>
        /// 获取所有菜单（菜单管理）
        /// </summary>
        /// <returns></returns>
        public List<SysMenu> SelectTreeMenuList(MenuQueryDto menu)
        {
            int parentId = 0;
            if (menu.ParentId != null)
            {
                parentId = (int)menu.ParentId;
            }
            var list = Queryable()
                .WhereIF(!string.IsNullOrEmpty(menu.MenuName), it => it.MenuName.Contains(menu.MenuName))
                .WhereIF(!string.IsNullOrEmpty(menu.Visible), it => it.Visible == menu.Visible)
                .WhereIF(!string.IsNullOrEmpty(menu.Status), it => it.Status == menu.Status)
                .WhereIF(!string.IsNullOrEmpty(menu.MenuTypeIds), it => menu.MenuTypeIdArr.Contains(it.MenuType))
                .WhereIF(menu.ParentId != null, it => it.ParentId == menu.ParentId)
                .OrderBy(it => new { it.ParentId, it.OrderNum })
                .ToTree(it => it.Children, it => it.ParentId, parentId);

            return list;
        }

        /// <summary>
        /// 根据用户查询系统菜单列表
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="roles">用户角色集合</param>
        /// <returns></returns>
        public List<SysMenu> SelectTreeMenuListByRoles(MenuQueryDto menu, List<long> roles)
        {
            var roleMenus = Context.Queryable<SysRoleMenu>()
                .Where(r => roles.Contains(r.Role_id))
                .Select(f => f.Menu_id).Distinct().ToList();

            return Queryable()
                .Where(c => roleMenus.Contains(c.MenuId))
                .WhereIF(!string.IsNullOrEmpty(menu.MenuName), (c) => c.MenuName.Contains(menu.MenuName))
                .WhereIF(!string.IsNullOrEmpty(menu.Visible), (c) => c.Visible == menu.Visible)
                .WhereIF(!string.IsNullOrEmpty(menu.Status), (c) => c.Status == menu.Status)
                .WhereIF(!string.IsNullOrEmpty(menu.MenuTypeIds), c => menu.MenuTypeIdArr.Contains(c.MenuType))
                .OrderBy((c) => new { c.ParentId, c.OrderNum })
                .Select(c => c)
                .ToTree(it => it.Children, it => it.ParentId, 0);
        }

        /// <summary>
        /// 获取所有菜单
        /// </summary>
        /// <returns></returns>
        public List<SysMenu> SelectMenuList(MenuQueryDto menu)
        {
            return Queryable()
                .WhereIF(!string.IsNullOrEmpty(menu.MenuName), it => it.MenuName.Contains(menu.MenuName))
                .WhereIF(!string.IsNullOrEmpty(menu.Visible), it => it.Visible == menu.Visible)
                .WhereIF(!string.IsNullOrEmpty(menu.Status), it => it.Status == menu.Status)
                .WhereIF(menu.ParentId != null, it => it.ParentId == menu.ParentId)
                .OrderBy(it => new { it.ParentId, it.OrderNum })
                .ToList();
        }

        /// <summary>
        /// 根据用户查询系统菜单列表
        /// </summary>
        /// <param name="sysMenu"></param>
        /// <param name="roles">用户角色集合</param>
        /// <returns></returns>
        public List<SysMenu> SelectMenuListByRoles(MenuQueryDto sysMenu, List<long> roles)
        {
            var roleMenus = Context.Queryable<SysRoleMenu>()
                .Where(r => roles.Contains(r.Role_id));

            return Queryable()
                .InnerJoin(roleMenus, (c, j) => c.MenuId == j.Menu_id)
                .Where((c, j) => c.Status == "0")
                .WhereIF(!string.IsNullOrEmpty(sysMenu.MenuName), (c, j) => c.MenuName.Contains(sysMenu.MenuName))
                .WhereIF(!string.IsNullOrEmpty(sysMenu.Visible), (c, j) => c.Visible == sysMenu.Visible)
                .OrderBy((c, j) => new { c.ParentId, c.OrderNum })
                .Select(c => c)
                .ToList();
        }

        /// <summary>
        /// 获取菜单详情
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public SysMenu SelectMenuById(int menuId)
        {
            return GetFirst(it => it.MenuId == menuId);
        }

        /// <summary>
        /// 添加菜单
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public int AddMenu(SysMenu menu)
        {
            menu.Create_time = DateTime.Now;
            menu.MenuId = InsertReturnIdentity(menu);
            return 1;
        }

        /// <summary>
        /// 编辑菜单
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public int EditMenu(SysMenu menu)
        {
            return Update(menu, false);
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public int DeleteMenuById(int menuId)
        {
            return Delete(menuId);
        }

        /// <summary>
        /// 菜单排序
        /// </summary>
        /// <param name="menuDto">菜单Dto</param>
        /// <returns></returns>
        public int ChangeSortMenu(MenuDto menuDto)
        {
            var result = Context.Updateable(new SysMenu() { MenuId = menuDto.MenuId, OrderNum = menuDto.OrderNum })
                .UpdateColumns(it => new { it.OrderNum }).ExecuteCommand();
            return result;
        }

        /// <summary>
        /// 查询菜单权限
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<SysMenu> SelectMenuPermsByUserId(long userId)
        {
            return Context.Queryable<SysMenu, SysRoleMenu, SysUserRole, SysRole>((m, rm, ur, r) => new JoinQueryInfos(
                JoinType.Left, m.MenuId == rm.Menu_id,
                JoinType.Left, rm.Role_id == ur.RoleId,
                JoinType.Left, ur.RoleId == r.RoleId
                ))
                //.Distinct()
                .Where((m, rm, ur, r) => m.Status == "0" && r.Status == "0" && ur.UserId == userId)
                .Select((m, rm, ur, r) => m).ToList();
        }

        /// <summary>
        /// 校验菜单名称是否唯一
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public SysMenu CheckMenuNameUnique(SysMenu menu)
        {
            return GetFirst(it => it.MenuName == menu.MenuName && it.ParentId == menu.ParentId);
        }

        /// <summary>
        /// 是否存在菜单子节点
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public int HasChildByMenuId(long menuId)
        {
            return Count(it => it.ParentId == menuId);
        }

        #region RoleMenu

        /// <summary>
        /// 查询菜单使用数量
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public int CheckMenuExistRole(long menuId)
        {
            return Context.Queryable<SysRoleMenu>().Where(it => it.Menu_id == menuId).Count();
        }

        #endregion
    }
}
