using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yinhe.ProcessingCenter;
using MongoDB.Driver;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.Common;
namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 角色验证处理
    /// </summary>
    public class AuthManage
    {
        /// <summary>
        /// 用于设置Html按钮的标签属性
        /// </summary>
        public static string CHECKRIGHTAG = "checkright";
        public static string USERTAG = "userId";
        private const string CacheKey = "USERRIGHT";

        private const int OverDay = 1;
        private int type = 0;
        public static AuthManage _()
        {
            return new AuthManage();
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
        /// 用户类型
        /// </summary>
        public static UserTypeEnum UserType
        {
            get
            {
                if (string.IsNullOrEmpty(PageReq.GetSession("UserType").ToString()))
                {
                    return UserTypeEnum.SystemUser;
                }
                else
                {
                    int userType = 0;
                    int.TryParse(PageReq.GetSession("UserType").ToString(), out userType);
                    return (UserTypeEnum)Enum.Parse(typeof(UserTypeEnum), userType.ToString());
                }
            }
        }



        #region 获取用户所有功能权限
        /// <summary>
        /// 获取用户所有系统功能权限
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<UserRight> GetUserFunctionRight(int userId)
        {
            List<UserRight> userRightList = new List<UserRight>();
            string userRightKey = string.Format("{0}_{1}", CacheKey, userId);
            //if (CacheHelper.GetCache(userRightKey) != null)
            //{
            //    userRightList = (List<UserRight>)CacheHelper.GetCache(userRightKey);
            //}
            //else
            //{
            //    userRightList = this.FindUserFunctionRight(userId);
            //    CacheHelper.SetCache(userRightKey, userRightList, null, DateTime.Now.AddDays(OverDay));
            //}
            userRightList = this.FindUserFunctionRight(userId);
            this._AllUserRight = userRightList;
            return userRightList;
        }


        /// <summary>
        /// 重写获取用户所有系统功能权限
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<UserRight> NewGetUserFunctionRight(int userId)
        {
            List<UserRight> userRightList = new List<UserRight>();

            userRightList = this.FindUserFunctionRightProj(userId);
            this._AllUserRight = userRightList;
            return userRightList;
        }

        #region 释放缓存
        public void ReleaseCache(int userId)
        {
            string userRightKey = string.Format("{0}_{1}", CacheKey, userId);
            CacheHelper.RemoveCache(userRightKey);
        }
        #endregion

        private List<UserRight> FindUserFunctionRightProj(int userId)
        {
            List<UserRight> userRighs = new List<UserRight>();
            DataOperation dataOp = new DataOperation();
            List<string> roleIdList = new List<string>();
            //获取用户角色
            List<BsonDocument> roleUsers = dataOp.FindAllByKeyVal("SysRoleUser", "userId", userId.ToString()).ToList();
            roleIdList.AddRange(roleUsers.Select(r => r.String("roleId")).Distinct().ToList());
            //获取用户的部门岗位
            List<string> postIdList = dataOp.FindAllByKeyVal("UserOrgPost", "userId", userId.ToString()).Select(u => u.String("postId")).Distinct().ToList();
            //获取用户所在部门
            List<string> orgIdList = dataOp.FindAllByKeyValList("OrgPost", "postId", postIdList).Select(o => o.String("orgId")).Distinct().ToList();
            roleIdList.AddRange(dataOp.FindAllByKeyValList("SysRoleOrg", "orgId", orgIdList).Select(o => o.String("roleId")).ToList());
            roleIdList = roleIdList.Distinct().ToList();

            //List<BsonDocument> dataObj = dataOp.FindAll().ToList();
            //获取DataScope
            List<BsonDocument> dataScopeList = dataOp.FindAllByKeyValList("DataScope", "roleId", roleIdList).ToList();
            List<string> dataScopes = new List<string>();
            foreach (var dataScope in dataScopeList)//所有拥有权限的区域
            {
                //这里拥有多种权限范围
                string data = dataScope.String("dataTableName") + "|" + dataScope.String("dataId");
                dataScopes.Add(data);
            }
            //获取角色权限
            List<BsonDocument> roleRights = dataOp.FindAllByKeyValList("SysRoleRight", "roleId", roleIdList).ToList();
            foreach (var roleRight in roleRights)
            {
                UserRight userRight = new UserRight();
                userRight.RoleId = roleRight.Int("roleId");
                userRight.ModuleId = roleRight.Int("modulId");
                userRight.OperatingId = roleRight.Int("operatingId");
                userRight.DataObjId = roleRight.Int("dataObjId");
                //userRight.DataId = dataScopeList.Where(s => s.Int("roleId") == roleRight.Int("roleId")).FirstOrDefault().Int("dataId");
                userRight.Code = roleRight.String("code");
                userRight.isDataRight = 1;

                userRight.RoleRightArea = dataScopes;
                userRighs.Add(userRight);
            }

            //获取
            return userRighs;
        }


        /// <summary>
        /// 获取用户的所有功能权限
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        private List<UserRight> FindUserFunctionRight(int userId)
        {
            List<UserRight> userRighs = new List<UserRight>();
            DataOperation dataOp = new DataOperation();
            List<string> roleIdList = new List<string>();
            //获取用户角色
            List<BsonDocument> roleUsers = dataOp.FindAllByKeyVal("SysRoleUser", "userId", userId.ToString()).ToList();
            roleIdList.AddRange(roleUsers.Select(r => r.String("roleId")).Distinct().ToList());
            //获取用户的部门岗位
            List<string> postIdList = dataOp.FindAllByKeyVal("UserOrgPost", "userId", userId.ToString()).Select(u => u.String("postId")).Distinct().ToList();
            //获取用户所在部门
            List<string> orgIdList = dataOp.FindAllByKeyValList("OrgPost", "postId", postIdList).Select(o => o.String("orgId")).Distinct().ToList();
            roleIdList.AddRange(dataOp.FindAllByKeyValList("SysRoleOrg", "orgId", orgIdList).Select(o => o.String("roleId")).ToList());
            roleIdList = roleIdList.Distinct().ToList();
            List<BsonDocument> roleRights = dataOp.FindAllByKeyValList("SysRoleRight", "roleId", roleIdList).ToList();
            foreach (var roleRight in roleRights)
            {
                UserRight userRight = new UserRight();
                userRight.RoleId = roleRight.Int("roleId");
                userRight.ModuleId = roleRight.Int("modulId");
                userRight.OperatingId = roleRight.Int("operatingId");
                userRight.DataObjId = roleRight.Int("dataObjId");
                userRight.DataId = roleRight.Int("dataId");
                userRight.Code = roleRight.String("code");
                userRighs.Add(userRight);
            }
            return userRighs;
        }
        #endregion

        #region 检查用户是否拥有指定权限代码
        /// <summary>
        /// 检查是否拥有响应权限
        /// </summary>
        /// <param name="rightCode">
        /// 多个权限 MODULECODE_RIGHTCODE|1|数据对象Id_数据实例Id,MODULECODE_RIGHTCODE
        ///MODULECODE_RIGHTCODE|userId_1
        ///MODULECODE_RIGHTCODE|数据对象Id_数据实例Id
        ///MODULECODE_RIGHTCODE|userId_1|数据对象Id_数据实例Id
        /// MODULECODE_RIGHTCODE --权限代码
        ///userId_1 --标示具有指定权限代码并且数据的创建人是本人
        ///数据对象Id_数据实例Id --指定该权限项验证的是指定数据对象，数据实例的权限（比如某个项目的权限）
        /// </param>
        /// <returns></returns>
        public bool CheckRight(string rightCode)
        {
            type = 0;
            if (AuthManage.UserType == UserTypeEnum.DebugUser) { return true; }
            if (this.CurrentUserId == -1) { return true; }
            if (SysAppConfig.IsPlugIn == true) { return true; }
            //用户权限代码列表
            List<UserRight> sysUserRights = this.AllUserRight.Where(u => u.DataId.HasValue == false || u.DataId.Value == 0).ToList();
            //List<UserRight> sysUserRights = this.AllUserRight.ToList();
            List<string> userRightCodes = sysUserRights.Select(r => r.Code).Distinct().ToList();
            string[] arrCode = rightCode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            bool hasRight = false;
            foreach (var c in arrCode)
            {
                //格式， 1|MODULECODE_RIGHTCODE
                string[] arrc = c.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                switch (arrc.Length)
                {
                    case 1:
                        //只有系统权限项判断
                        if (userRightCodes.Contains(arrc[0]) == true)
                        {
                            hasRight = true;
                        }
                        break;
                    case 2:
                        //系统权限项+创建用户 或者 数据权限+指定数据对象+指定对象实例Id
                        string[] arrSecond = arrc[1].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        if (arrSecond.Length == 2)
                        {
                            if (arrSecond[0] == AuthManage.USERTAG)
                            {
                                //创建用户
                                hasRight = userRightCodes.Contains(arrc[0]) == true && arrSecond[1] == this.CurrentUserId.ToString();
                            }
                            else
                            {
                                hasRight = this.CheckDataRight(int.Parse(arrSecond[0]), int.Parse(arrSecond[1]), arrc[0], null);
                            }
                        }
                        break;
                    case 3:
                        //数据权限（项目）,并且配合创建人
                        //创建人
                        string[] arrUser = arrc[1].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        //数据对象-数据实例
                        string[] arrData = arrc[2].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        if (arrUser.Length == 2 && arrData.Length == 2)
                        {
                            hasRight = this.CheckDataRight(int.Parse(arrData[0]), int.Parse(arrData[1]), arrc[0], int.Parse(arrUser[1]));
                        }
                        break;
                }
                if (hasRight == true)
                {
                    break;
                }
            }
            return hasRight;
        }

        public bool CheckRight(string rightCode, String dataTable, string id)//项目权限，第一个参数为rightCode,dataTable为区域标识
        {
            bool hasRight = false;
            if (!string.IsNullOrEmpty(dataTable))
                type = 1;
            if (AuthManage.UserType == UserTypeEnum.DebugUser) { return true; }
            if (this.CurrentUserId == -1) { return true; }
            //List<UserRight> sysUserRights = this._AllUserRight.Where(s => s.isDataRight == 1).ToList();
            List<UserRight> sysUserRights = this.AllUserRight.ToList();
            List<string> userRightCodes = sysUserRights.Select(r => r.Code).Distinct().ToList();//获取权限码
            UserRight model = sysUserRights.FirstOrDefault();
            List<string> area = model!=null?model.RoleRightArea:new List<string>();
            string[] arrCode = rightCode.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            string flagArea = dataTable + "|" + id;

            foreach (var c in arrCode)
            {
                //格式， 1|MODULECODE_RIGHTCODE
                string[] arrc = c.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                switch (arrc.Length)
                {
                    case 1:
                        //只有系统权限项判断
                        if (userRightCodes.Contains(arrc[0]) == true)
                        {
                            hasRight = true;
                        }
                        break;
                    case 2:
                        //系统权限项+创建用户 或者 数据权限+指定数据对象+指定对象实例Id
                        string[] arrSecond = arrc[1].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        if (arrSecond.Length == 2)
                        {
                            if (arrSecond[0] == AuthManage.USERTAG)
                            {
                                //创建用户
                                hasRight = userRightCodes.Contains(arrc[0]) == true && arrSecond[1] == this.CurrentUserId.ToString();
                            }
                            else
                            {
                                hasRight = this.CheckDataRight(int.Parse(arrSecond[0]), int.Parse(arrSecond[1]), arrc[0], null);
                            }
                        }
                        break;
                    case 3:
                        //数据权限（项目）,并且配合创建人
                        //创建人
                        string[] arrUser = arrc[1].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        //数据对象-数据实例
                        string[] arrData = arrc[2].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                        if (area.Contains(flagArea))
                        {

                            hasRight = this.CheckDataRight(int.Parse(arrData[0]), int.Parse(arrData[1]), arrc[0], int.Parse(arrUser[0]), 1);

                        }
                        break;
                }
                if (hasRight == true)
                {
                    break;
                }
            }
            return hasRight;
        }


        public bool CheckRight(String dataTable, string id)
        {
            bool hasRight = false;
            if (!string.IsNullOrEmpty(dataTable))
                type = 1;
            if (AuthManage.UserType == UserTypeEnum.DebugUser) { return true; }
            if (this.CurrentUserId == -1) { return true; }
            //List<UserRight> sysUserRights = this._AllUserRight.Where(s => s.isDataRight == 1).ToList();
            List<UserRight> sysUserRights = this.AllUserRight.ToList();
            List<string> userRightCodes = sysUserRights.Select(r => r.Code).Distinct().ToList();//获取权限码
            List<string> area = sysUserRights.Count() > 0 ? sysUserRights.FirstOrDefault() != null ? sysUserRights.FirstOrDefault().RoleRightArea : new List<string>() : new List<string>();


            string flagArea = dataTable + "|" + id;


            if (area.Contains(flagArea))
            {

                //hasRight = this.CheckDataRight(int.Parse(arrData[0]), int.Parse(arrData[1]), arrc[0], int.Parse(arrUser[0]), 1);
                hasRight = true;
            }


            return hasRight;
        }

        /// <summary>
        /// 权限项代码
        /// </summary>
        private Dictionary<string, List<string>> _DicDataRigh;
        /// <summary>
        /// 权限项代码
        /// </summary>
        private Dictionary<string, List<string>> DicDataRigh
        {
            get
            {
                if (this._DicDataRigh == null)
                {
                    this._DicDataRigh = new Dictionary<string, List<string>>();
                }
                return this._DicDataRigh;
            }
        }
        /// <summary>
        /// 用户权限代码列表
        /// </summary>
        private List<UserRight> _AllUserRight;
        /// <summary>
        /// 用户权限代码列表
        /// </summary>
        public List<UserRight> AllUserRight
        {
            get
            {
                if (_AllUserRight == null)
                {
                    if (type == 0)
                        this._AllUserRight = AuthManage._().GetUserFunctionRight(this.CurrentUserId);
                    else
                    {
                        this._AllUserRight = AuthManage._().NewGetUserFunctionRight(this.CurrentUserId);
                    }
                    if (this._AllUserRight == null)
                    {
                        this._AllUserRight = new List<UserRight>();
                    }
                }
                return this._AllUserRight;
            }
        }


        /// <summary>
        /// 检查数据权限代码
        /// </summary>
        /// <param name="dataObjId"></param>
        /// <param name="dataId"></param>
        /// <param name="code"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private bool CheckDataRight(int dataObjId, int dataId, string code, int? userId)
        {
            string dicKey = string.Format("{0}_{1}", dataObjId, dataId);
            List<string> dataUserRightCodes = new List<string>();
            if (this.DicDataRigh.ContainsKey(dicKey) == false)
            {
                dataUserRightCodes = this.AllUserRight
                    .Where(u => u.DataObjId == dataObjId && u.DataId == dataId)
                    .Select(u => u.Code)
                    .Distinct()
                    .ToList();
                this.DicDataRigh.Add(dicKey, dataUserRightCodes);
            }
            else
            {
                dataUserRightCodes = this.DicDataRigh[dicKey];
            }
            bool checkResult = checkResult = dataUserRightCodes.Contains(code);
            if (userId.HasValue == true)
            {
                checkResult = checkResult && userId.Value == this.CurrentUserId;
            }
            return checkResult;
        }


        private bool CheckDataRight(int dataObjId, int dataId, string code, int? userId, int flag)
        {
            string dicKey = string.Format("{0}_{1}", dataObjId, dataId);
            List<string> dataUserRightCodes = new List<string>();
            if (this.DicDataRigh.ContainsKey(dicKey) == false)
            {
                dataUserRightCodes = this.AllUserRight.Select(a => a.Code).Distinct().ToList();
                this.DicDataRigh.Add(dicKey, dataUserRightCodes);
            }
            else
            {
                dataUserRightCodes = this.DicDataRigh[dicKey];
            }
            bool checkResult = dataUserRightCodes.Contains(code);
            //if (userId.HasValue == true)
            //{
            //    checkResult = checkResult && userId.Value == this.CurrentUserId;
            //}
            return checkResult;
        }
        #endregion

        #region 成果库权限代码转换
        private static string[] ArrModule = new string[] { "", "UNITLIB", "DECORATIONLIB", "FACADELIB", "DEMAREALIB", "LANDSCAPELIB", "CRAFTSLIB", "PARTSLIB", "DEVICELIB" };
        private static string[] ArrDataObj = new string[] { "", "UNIT", "DECORATION", "FACADE", "DEMAREA", "LANDSCAPE", "CRAFTS", "PARTS", "PARTSLIB", "DEVICE" };
        private static string[] ArrRightCode = new string[] { "", "VIEW", "DOWNLOAD", "ADD", "UPDATE", "UPDATEALL", "ADMIN" };
        /// <summary>
        /// 获取标准成果库权限代码
        /// </summary>
        /// <param name="libId">库Id</param>
        /// <param name="code"></param>
        /// <param name="createUserId"></param>
        /// <returns></returns>
        public static string GetCode(int libId, RightCodeEnum codeEnum, int? createUserId)
        {
            if (ArrModule.Length < libId) { return ""; }
            string createUserTag = string.Empty;
            if (createUserId.HasValue == true)
            {
                createUserTag = string.Format("|{0}_{1}", AuthManage.USERTAG, createUserId.Value);
            }
            return string.Format("\"{0}_{1}_{2}{3}\"", ArrModule[libId], ArrRightCode[((int)codeEnum)], ArrDataObj[libId], createUserTag);
        }
        /// <summary>
        /// 获取标准成果库权限代码标签
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        public static string GetCodeTag(params string[] codes)
        {
            if (codes.Length <= 0) { return ""; }
            string codeTag = string.Format("{0}=", AuthManage.CHECKRIGHTAG);
            int index = 0;
            int len = codes.Length;
            foreach (var code in codes)
            {
                codeTag += code;
                index++;
                if (index < len)
                {
                    codeTag += ",";
                }
            }
            return codeTag;
        }


        /// <summary>
        /// 获取标准成果库权限代码
        /// </summary>
        /// <param name="libId">库Id</param>
        /// <param name="code"></param>
        /// <param name="createUserId"></param>
        /// <returns></returns>
        public static string GetCode(StandarLibEnum standarLibEnum, RightCodeEnum codeEnum, int? createUserId)
        {
            int libId = (int)standarLibEnum;
            return AuthManage.GetCode(libId, codeEnum, createUserId);
        }

        #endregion

    }


}
