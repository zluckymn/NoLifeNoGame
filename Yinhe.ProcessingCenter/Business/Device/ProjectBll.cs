using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter.Business.Device
{
    /// <summary>
    /// 豪宅项目处理类
    /// </summary>
    public class ProjectBll
    {
        #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private DataOperation dataOp = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public ProjectBll()
        {
            dataOp = new DataOperation();
        }

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        private ProjectBll(DataOperation ctx)
        {
            dataOp = ctx;
        }


        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProjectBll _()
        {
            return new ProjectBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static ProjectBll _(DataOperation ctx)
        {
            return new ProjectBll(ctx);
        }
        #endregion

        #region 获取所有的项目地区 +List<string> GetProjectAreas()
        /// <summary>
        /// 获取所有的项目地区
        /// </summary>
        /// <returns></returns>
        public List<string> GetProjectAreas()
        {
            List<string> areas = new List<string>();
            List<BsonDocument> projects = dataOp.FindAll("Device_project").ToList();
            foreach (BsonDocument project in projects)
            {
                string area = project.String("area", "").Trim();
                if (!areas.Contains(area))
                {
                    areas.Add(area);
                }
            }
            return areas;
        }
        #endregion

        #region 获取所有楼房类型 +List<string> GetProjectBuildingTypes()
        /// <summary>
        /// 获取所有楼房类型
        /// </summary>
        /// <returns></returns>
        public List<string> GetProjectBuildingTypes()
        {
            List<string> bTypes = new List<string>();
            List<BsonDocument> projects = dataOp.FindAll("Device_project").ToList();
            foreach (BsonDocument project in projects)
            {
                string bType = project.String("buildingType", "").Trim();
                if (!bTypes.Contains(bType))
                {
                    bTypes.Add(bType);
                }
            }
            return bTypes;
        }
        #endregion

        public List<BsonDocument> OrderProjByBrand(List<BsonDocument> projList,string catId)
        {
            var orderedProjList = new List<BsonDocument>();
            var attributes = dataOp.FindAllByQuery("Device_attribute", Query.And(
                    Query.EQ("catId", catId),
                    Query.In("projId", projList.Select(c => c.GetValue("projId")))
                )).ToList();
            var brandIds = dataOp.FindAll("Device_brand").Select(c=>c.Int("brandId")).ToList();
            var brandStatistics = (from a in attributes
                                   where brandIds.Contains(a.Int("brandId"))
                                   group a by a.Int("brandId")
                                       into g
                                       select g).OrderByDescending(g => g.Count()).Select(c => c.Key).ToList();
            //插入有品牌的的项目
            foreach (int brandId in brandStatistics) 
            {
                var projIds= attributes.Where(c => c.Int("brandId")==brandId).Select(c=>c.Int("projId")).ToList();
                var projs=projList.Where(c=>projIds.Contains(c.Int("projId")));
                orderedProjList.AddRange(projs);
            }
            //插入无品牌，或者品牌有Id，单品牌表里不存在的项目
            var restProjIds = attributes.Where(c => !brandStatistics.Contains(c.Int("brandId"))).Select(c => c.Int("projId")).ToList();
            var restProjs=projList.Where(c=>restProjIds.Contains(c.Int("projId")));
            orderedProjList.AddRange(restProjs);
            return orderedProjList;
        }

    }
}
