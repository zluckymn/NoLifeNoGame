using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Yinhe.ProcessingCenter.Common;

namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 区域角色枚举
    /// </summary>
    public abstract class AreaRoleType
    {
        /// <summary>
        /// 区域
        /// </summary>
        public const string Area = "LandArea";

        /// <summary>
        /// 城市
        /// </summary>
        public const string City = "LandCity";

        /// <summary>
        /// 地块
        /// </summary>
        public const string Land = "Land";

        /// <summary>
        /// 项目分期
        /// </summary>
        public const string Project = "Project";



    }
    /// <summary>
    /// 用户权限项验证处理相关
    /// </summary>
    public class Authentication
    {
        private DataOperation _dataOp = new DataOperation();

        private List<BsonDocument> scopeList = new List<BsonDocument>();
        public List<RoleRight> sysRoleRights = new List<RoleRight>();
        public List<RoleRight> projectRights = new List<RoleRight>();
        private List<BsonDocument> roleRights;
        private List<string> roleIdList;
        /// <summary>
        /// 系统管理员
        /// </summary>
        private bool isAdmin = false;

        public List<BsonDocument> AllRoleRights
        {
            get
            {
                return this.roleRights;
            }
        }

        /// <summary>
        /// 获取当前用户的Id
        /// </summary>
        /// <value>当前用户的ID</value>
        public int CurrentUserId
        {
            get
            {
                if (string.IsNullOrEmpty(PageReq.GetSession("UserId").ToString()))
                {
                    return -1;
                }
                else
                {
                    return int.Parse(PageReq.GetSession("UserId"));
                }
            }
        }


        /// <summary>
        /// 传入用户Id，获取该用户的所有权限
        /// </summary>
        /// <param name="userId"></param>
        public Authentication(int userId)
        {
            this.roleIdList = GetUserRoles(userId);//获取用户的所有角色
            //获取角色权限
            this.roleRights = _dataOp.FindAllByKeyValList("SysRoleRight", "roleId", this.roleIdList).OrderBy(s => s.Int("roleId")).ToList();
            InitSysRight();//初始化系统权限
            InitProjectRoleRight();//初始化项目权限
            var curUser = _dataOp.FindAll("SysUser").Where(t => t.Int("userId") == userId).FirstOrDefault();
            if (curUser != null && curUser.Int("type") == 1)
            {
                isAdmin = true;
            }
        }

        /// <summary>
        /// 默认装载当前用户的所有权限
        /// </summary>
        public Authentication()
        {
            this.roleIdList = GetUserRoles(CurrentUserId);//获取用户的所有角色

            //获取角色权限
            this.roleRights = _dataOp.FindAllByKeyValList("SysRoleRight", "roleId", this.roleIdList).OrderBy(s => s.Int("roleId")).ToList();
            InitSysRight();//初始化系统权限
            InitProjectRoleRight();//初始化项目权限
            var curUser = _dataOp.FindAll("SysUser").Where(t => t.Int("userId") == CurrentUserId).FirstOrDefault();
            if (curUser != null && curUser.Int("type") == 1)
            {
                isAdmin = true;
            }
        }

        /// <summary>
        /// 系统角色权限
        /// </summary>
        private void InitSysRight()
        {
            foreach (var r in this.roleRights)
            {
                RoleRight roleRight = new RoleRight { moduleId = r.String("modulId"), code = r.String("code"), isProjectRight = false };
                this.sysRoleRights.Add(roleRight);
            }
            this.sysRoleRights = this.sysRoleRights.Distinct(new SysRoleRightComparer()).ToList();
        }

        /// <summary>
        /// 项目角色权限
        /// </summary>
        private void InitProjectRoleRight()
        {
            //获取项目角色
            GetSysProjectRight();//系统设置的项目权限
            GetInitProjectRight();//内置项目权限
            foreach (var dataScope in this.scopeList.Distinct(new ScopeComparer()))//所有拥有权限的区域
            {
                var tempRoleRights = this.roleRights.Where(s => s.Int("roleId") == dataScope.Int("roleId"));
                foreach (var temp in tempRoleRights)
                {
                    RoleRight roleRight = new RoleRight
                    {
                        code = temp.String("code"),
                        isProjectRight = true,
                        areaRoleType = dataScope.String("dataTableName"),
                        dataId = dataScope.String("dataId")
                    };
                    this.projectRights.Add(roleRight);
                }
            }
        }

        /// <summary>
        /// 获取项目权限
        /// </summary>
        private void GetSysProjectRight()
        {
            //获取项目角色
            var sysRoleList = _dataOp.FindAllByKeyValList("SysRole", "roleId", roleIdList).Where(s => s.Int("landOrProj") == 0);//非内置角色
            var sysRoleIdList = sysRoleList.Select(s => s.String("roleId"));
            List<BsonDocument> dataScopeList = _dataOp.FindAllByKeyValList("DataScope", "roleId", sysRoleIdList).ToList();
            var tempAreas = dataScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Area).ToList();
            var tempCitys = dataScopeList.Where(s => s.String("dataTableName") == AreaRoleType.City).ToList();
            var tempLands = dataScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Land).ToList();
            var tempProjs = dataScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Project).ToList();

            //区域权限
            foreach (var area in tempAreas)
            {
                string roleId = area.String("roleId");
                this.scopeList.Add(area);
                var citys = CalcChildScope("LandCity", "areaId", area.String("dataId"));//区域下城市
                foreach (var city in citys)
                {
                    tempCitys.Add(new BsonDocument { { "dataTableName", AreaRoleType.City }, { "roleId", roleId }, { "dataId", city.String("cityId") } });
                }
            }

            //城市权限
            var existLandId = _dataOp.FindAll("Land").Select(s => s.String("landId"));
            foreach (var city in tempCitys)
            {
                this.scopeList.Add(city);

                var lands = CalcChildScope("Land", "cityId", city.String("dataId"));
                lands = lands.Where(s => existLandId.Contains(s.String("landId"))).ToList();
                string roleId = city.String("roleId");
                foreach (var land in lands)
                {
                    tempLands.Add(new BsonDocument { { "dataTableName", AreaRoleType.Land }, { "roleId", roleId }, { "dataId", land.String("landId") } });
                }
            }

            //地块权限
            var existProjId = _dataOp.FindAll("Project").Select(s => s.String("projId"));
            foreach (var land in tempLands)
            {
                string roleId = land.String("roleId");
                this.scopeList.Add(land);
                var projs = CalcChildScope("Project", "landId", land.String("dataId"));
                projs = projs.Where(s => existProjId.Contains(s.String("projId"))).ToList();
                foreach (var proj in projs)
                {
                    tempProjs.Add(new BsonDocument { { "dataTableName", AreaRoleType.Project }, { "roleId", roleId }, { "dataId", proj.String("projId") } });
                }
            }
            foreach (var proj in tempProjs)
            {
                this.scopeList.Add(proj);
            }
        }


        /// <summary>
        /// 通过权限码，获取有该项目权限的用户列表
        /// </summary>
        /// <param name="roleType"></param>
        /// <param name="dataId"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public List<BsonDocument> GetUserOfProjectRight(string roleType, string dataId, string code)
        {
            var roleRights = _dataOp.FindAllByKeyVal("SysRoleRight", "code", code);//获取拥有改权限的角色权限
            var roleIdList = roleRights.Select(s => s.String("roleId"));
            var roleList = _dataOp.FindAllByKeyValList("SysRole", "roleId", roleIdList).ToList();//获取角色

            var sysRoleList = roleList.Where(s => s.Int("landOrProj") == 0);//非内置角色
            var initRoleList = roleList.Where(s => s.Int("landOrProj") != 0);//内置角色
            var initRoleIds = initRoleList.Select(s => s.String("roleId"));
            var sysRoleIds = sysRoleList.Select(s => s.String("roleId"));


            List<BsonDocument> initScopeList = _dataOp.FindAllByKeyValList("DataScope", "roleId", initRoleIds).ToList();
            List<BsonDocument> sysScopeList = _dataOp.FindAllByKeyValList("DataScope", "roleId", sysRoleIds).ToList();



            var tempAreas = sysScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Area).ToList();
            var tempCitys = sysScopeList.Where(s => s.String("dataTableName") == AreaRoleType.City).ToList();
            var tempLands = sysScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Land).ToList();
            var tempProjs = sysScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Project).ToList();

            List<BsonDocument> tempScopeList = new List<BsonDocument>();

            //区域权限
            foreach (var area in tempAreas)
            {
                string roleId = area.String("roleId");
                tempScopeList.Add(area);
                var citys = CalcChildScope("LandCity", "areaId", area.String("dataId"));//区域下城市
                foreach (var city in citys)
                {
                    tempCitys.Add(new BsonDocument { { "dataTableName", AreaRoleType.City }, { "roleId", roleId }, { "dataId", city.String("cityId") } });
                }
            }

            //城市权限
            var existLandId = _dataOp.FindAll("Land").Select(s => s.String("landId"));
            foreach (var city in tempCitys)
            {
                tempScopeList.Add(city);

                var lands = CalcChildScope("Land", "cityId", city.String("dataId"));
                lands = lands.Where(s => existLandId.Contains(s.String("landId"))).ToList();
                string roleId = city.String("roleId");
                foreach (var land in lands)
                {
                    tempLands.Add(new BsonDocument { { "dataTableName", AreaRoleType.Land }, { "roleId", roleId }, { "dataId", land.String("landId") } });
                }
            }

            //地块权限
            var existProjId = _dataOp.FindAll("Project").Select(s => s.String("projId"));
            foreach (var land in tempLands)
            {
                string roleId = land.String("roleId");
                tempScopeList.Add(land);
                var projs = CalcChildScope("Project", "landId", land.String("dataId"));
                projs = projs.Where(s => existProjId.Contains(s.String("projId"))).ToList();
                foreach (var proj in projs)
                {
                    tempProjs.Add(new BsonDocument { { "dataTableName", AreaRoleType.Project }, { "roleId", roleId }, { "dataId", proj.String("projId") } });
                }
            }
            foreach (var proj in tempProjs)
            {
                tempScopeList.Add(proj);
            }

            tempScopeList = tempScopeList.Union(initScopeList).Where(s => s.String("dataTableName") == roleType && s.String("dataId") == dataId).ToList();
            var temproleIdList = tempScopeList.Select(s => s.String("roleId")).ToList();





            return FindUserByRoleIds(temproleIdList);
        }

        /// <summary>
        /// 通过角色Id获取角色用户
        /// </summary>
        /// <param name="roleIdList">角色Id列表</param>
        /// <returns></returns>
        private List<BsonDocument> FindUserByRoleIds(IEnumerable<string> roleIdList)
        {
            //获取用户角色
            var roleIds = roleIdList.ToList();
            List<BsonDocument> roleUsers = _dataOp.FindAllByKeyValList("SysRoleUser", "roleId", roleIdList).ToList();
            var roleUserIds = roleUsers.Select(r => r.String("userId")).ToList();

            var orgIdList = _dataOp.FindAllByKeyValList("SysRoleOrg", "roleId", roleIdList).Select(o => o.String("orgId")).ToList();//部门角色
            var postIdList = _dataOp.FindAllByKeyValList("OrgPost", "orgId", orgIdList).Select(o => o.String("postId")).ToList();
            var userIds = _dataOp.FindAllByKeyValList("UserOrgPost", "postId", postIdList).Select(u => u.String("userId")).ToList();
            userIds = roleUserIds.Union(userIds).ToList();
            return _dataOp.FindAllByKeyValList("SysUser", "userId", userIds).ToList();


            //获取用户所在的所有岗位角色
            //var postIdList = _dataOp.FindAllByKeyVal("UserOrgPost", "userId", userId.ToString()).Select(u => u.String("postId"));
            //var rolePostIds = _dataOp.FindAllByKeyValList("SysCommPost", "comPostId", postIdList).Select(s => s.String("roleId"));//岗位角色

            //获取用户所在的所有部门角色
            //var orgIdList = _dataOp.FindAllByKeyValList("OrgPost", "postId", postIdList).Select(o => o.String("orgId"));

        }


        /// <summary>
        /// 获取内置项目权限
        /// </summary>
        private void GetInitProjectRight()
        {
            //获取项目角色
            var initRoleList = _dataOp.FindAllByKeyValList("SysRole", "roleId", roleIdList).Where(s => s.Int("landOrProj") != 0).ToList();//系统中的内置角色
            var initRoleIdList = initRoleList.Select(s => s.String("roleId")).ToList();
            List<BsonDocument> dataScopeList = _dataOp.FindAllByKeyValList("DataScope", "roleId", initRoleIdList).ToList();

            var tempLands = dataScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Land).ToList();
            var tempProjs = dataScopeList.Where(s => s.String("dataTableName") == AreaRoleType.Project).ToList();

            //系统中未删除的地块
            var existLandId = _dataOp.FindAll("Land").Select(s => s.String("landId"));

            var lands = tempLands.Where(s => existLandId.Contains(s.String("dataId"))).ToList();
            foreach (var land in lands)
            {
                this.scopeList.Add(new BsonDocument { { "dataTableName", AreaRoleType.Land }, { "roleId", land.String("roleId") }, { "dataId", land.String("dataId") } });
            }


            //地块权限
            var existProjId = _dataOp.FindAll("Project").Select(s => s.String("projId"));
            var projs = tempProjs.Where(s => existProjId.Contains(s.String("dataId"))).ToList();
            foreach (var proj in projs)
            {
                this.scopeList.Add(new BsonDocument { { "dataTableName", AreaRoleType.Project }, { "roleId", proj.String("roleId") }, { "dataId", proj.String("dataId") } });
            }
        }


        private List<BsonDocument> CalcChildScope(string table, string key, string pkvalue)
        {
            return _dataOp.FindAllByKeyVal(table, key, pkvalue).ToList();
        }

        /// <summary>
        /// 获取用户在系统中的所有角色Id
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        private List<string> GetUserRoles(int userId)
        {
            //获取用户角色
            List<BsonDocument> roleUsers = _dataOp.FindAllByKeyVal("SysRoleUser", "userId", userId.ToString()).ToList();
            var roleUserIds = roleUsers.Select(r => r.String("roleId"));

            //获取用户所在的所有岗位角色
            var postIdList = _dataOp.FindAllByKeyVal("UserOrgPost", "userId", userId.ToString()).Select(u => u.String("postId"));
            var rolePostIds = _dataOp.FindAllByKeyValList("SysCommPost", "comPostId", postIdList).Select(s => s.String("roleId"));//岗位角色

            //获取用户所在的所有部门角色
            var orgIdList = _dataOp.FindAllByKeyValList("OrgPost", "postId", postIdList).Select(o => o.String("orgId"));
            var roleOrgIds = _dataOp.FindAllByKeyValList("SysRoleOrg", "orgId", orgIdList).Select(o => o.String("roleId"));//部门角色

            return roleUserIds.Union(rolePostIds).Union(roleOrgIds).ToList();
        }

        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="code">权限码组成：模块_操作 e.g. MATERIALLIB_ADD 材料库新增权限</param>
        /// <returns></returns>
        public bool CheckProjectRight(string roleType, string dataId, params string[] codes)
        {
            if (isAdmin) return true;
            foreach (var code in codes)
            {
                var rightList = this.projectRights.Where(s => s.areaRoleType == roleType && s.dataId == dataId && s.code == code);
                if (rightList.Any())
                {
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// 验证系统权限
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        public bool CheckSysRight(params string[] codes)
        {
            if (isAdmin) return true;
            foreach (var code in codes)
            {
                var rightList = this.sysRoleRights.Where(s => s.code == code);
                if (rightList.Any())
                {
                    return true;
                }
            }
            return false;
        }

        public bool CheckStdLibRight(BsonDocument retObj, params string[] codes)
        {
            if (isAdmin) return true;
            string libId = retObj.String("libId");
            foreach (var code in codes)
            {
                var rightList = this.sysRoleRights.Where(s => s.code == code);
                if (rightList.Any())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断当前用户是否可以点击项目资料库按钮
        /// </summary>
        /// <returns></returns>
        public bool IsShowProjLibMenu()
        {
            return this.projectRights.Where(s => s.areaRoleType == AreaRoleType.Project && s.code == "POJECTDATALIB_VIEW").Any();
        }

        /// <summary>
        /// 判断用户是否有项目库的查看权限
        /// </summary>
        /// <returns></returns>
        public bool IsShowProjMenu()
        {
            var projViews = this.projectRights.Where(s => s.code == "PROJECTLIB_VIEW").ToList();
            return (this.projectRights.Where(s => s.areaRoleType == AreaRoleType.Area && s.code == "PROJECTLIB_VIEW").Any() ||
                    this.projectRights.Where(s => s.areaRoleType == AreaRoleType.City && s.code == "PROJECTLIB_VIEW").Any() ||
                    this.projectRights.Where(s => s.areaRoleType == AreaRoleType.Land && s.code == "PROJECTLIB_VIEW").Any() ||
                    this.projectRights.Where(s => s.areaRoleType == AreaRoleType.Project && s.code == "PROJECTLIB_VIEW").Any()
                );

        }

        public class RoleRight
        {
            public string code { get; set; }

            public string areaRoleType { get; set; }

            public string dataId { get; set; }

            public bool isProjectRight { get; set; }

            public string moduleId { get; set; }

            public string operatingId { get; set; }

            public string key { get; set; }
        }

        /// <summary>
        /// 区域模块
        /// </summary>
        private class ScopeComparer : IEqualityComparer<BsonDocument>
        {
            public bool Equals(BsonDocument x, BsonDocument y)
            {
                return x.String("dataId") == y.String("dataId") && x.String("dataTableName") == y.String("dataTableName") && x.String("roleId") == y.String("roleId");
            }

            public int GetHashCode(BsonDocument obj)
            {
                return obj.ToString().GetHashCode();
            }
        }

        private class SysRoleRightComparer : IEqualityComparer<RoleRight>
        {
            public bool Equals(RoleRight x, RoleRight y)
            {
                return x.moduleId == y.moduleId && x.code == y.code;
            }

            public int GetHashCode(RoleRight obj)
            {
                return obj.ToString().GetHashCode();
            }
        }
    }
}
