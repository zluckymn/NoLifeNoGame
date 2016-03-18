using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Yinhe.ProcessingCenter;
using MongoDB.Driver;
using MongoDB.Bson;
using Yinhe.ProcessingCenter.Common;
using MongoDB.Driver.Builders;

///<summary>
///系统权限处理
///</summary>
namespace Yinhe.ProcessingCenter.Permissions
{
    /// <summary>
    /// 项目权限数据范围
    /// </summary>
    public class ProjectRightScope : IRightScope
    {
        #region IRightScope 成员
        /// <summary>
        /// 获取角色对应数据范围列表
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public List<DataScopeEntity> GetDataScopes(int roleId)
        {
            DataOperation dataOp = new DataOperation();

            List<BsonDocument> dataScopes = dataOp.FindAllByKeyVal("DataScope", "roleId", roleId.ToString()).ToList();

            return this.ConvertToBiz(dataScopes);
        }

        /// <summary>
        /// 保存角色数据范围数据
        /// </summary>
        /// <param name="dataScopes"></param>
        /// <returns></returns>
        public InvokeResult Save(List<DataScopeEntity> dataScopes, int roleId)
        {
            DataOperation dataOp = new DataOperation();
            string deleteQuery = string.Empty;
            if (dataScopes.Count <= 0)
            {
                //如果为空删除所有数据范围
                deleteQuery = string.Format("db.DataScope.find({\"roleId\":\"{0}\")", roleId);
                return dataOp.Delete("DataScope", deleteQuery);
            }

            #region 构建删除查询语句
            List<string> dataIdList = dataScopes.Where(d => d.ScopeId != 0).Select(d => d.ScopeId.ToString()).ToList();

            var delQuery = Query.And(
                Query.EQ("roleId", roleId.ToString()),
                Query.NotIn("scopeId", TypeConvert.StringListToBsonValueList(dataIdList))
                );
            dataOp.Delete("", delQuery);

            #endregion

            #region 构建插入语句

            #endregion
            using (TransactionScope tran = new TransactionScope())
            {

            }
            //db.users.find({"roleId":"","dataTableName":"":"dataFeildName":"","数据Id":{"$nin":[10000,20000]}}) 
            throw new NotImplementedException();
        }

        public object GetUserDataScopeRight(int userId)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// 将数据对象转为业务对象
        /// </summary>
        /// <param name="dataScopes"></param>
        /// <returns></returns>
        private List<DataScopeEntity> ConvertToBiz(List<BsonDocument> dataScopes)
        {
            List<DataScopeEntity> dataScopeEntities = new List<DataScopeEntity>();
            foreach (var scope in dataScopes)
            {
                dataScopeEntities.Add(new DataScopeEntity()
                {
                    ScopeId = scope.Int("scopeId"),
                    RoleId = scope.Int("roleId"),
                    RoleCategoryId = scope.Int("roleCategoryId"),
                    DataTableName = scope.String("dataTableName"),
                    DataFeildName = scope.String("dataFeildName"),
                    DataId = scope.Int("dataId"),
                    CreateUserId = scope.Int("createUserId"),
                    UpdateUserId = scope.Int("updateUserId"),
                    CreateDate = scope.Date("createDate"),
                    UpdateDate = scope.Date("updateDate"),
                    Status = scope.Int("status"),
                    order = scope.Int("order"),
                    remark = scope.String("order")
                });
            }
            return dataScopeEntities;
        }

        /// <summary>
        /// 将实体对象转为bsondocument
        /// </summary>
        /// <param name="dataScopes"></param>
        /// <returns></returns>
        private List<BsonDocument> ConvertToBson(List<DataScopeEntity> dataScopes)
        {
            List<BsonDocument> bsonDocuments = new List<BsonDocument>();

            return bsonDocuments;
        }
    }
}
