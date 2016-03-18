using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Diagnostics;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.Common;
using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Driver.Builders;
using Yinhe.ProcessingCenter.LifeDay;


namespace Yinhe.WebHost.Controllers
{
    public class LifeDayController : Yinhe.ProcessingCenter.ControllerBase //Controller
    {
        public ActionResult NoLifeNoGame()
        {
            return View();
        }
        public ActionResult MissionDetail()
        {
            return View();
        }
        public ActionResult Settings()
        {
            return View();
        }
        public ActionResult PersonItemList()
        {
            return View();
        }
        public ActionResult MissionQuickAdd()
        {
            return View();
        }
        public ActionResult MissionQuickGroupDetail()
        {
            return View();
        }
        
        
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult MissionComplete()
        {
            
           var addition= SysAppConfig.Mission_PointAddition;//10%加成
           var  maxAddition = SysAppConfig.Mission_MaxPointAddition;//最高加成70&
           var itemDropSeed = SysAppConfig.Mission_ItemDropSeed;//6000000
          
           //var addionMessage = new StringBuilder();//加成信息
           var itemMessage = new StringBuilder();//物品获得信息
           var rareItemMessage = new StringBuilder();//稀有物品获得信息
           string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
           DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
           var lifeDayHelper = new LifeDayHelper(dataOp);

            //缓存
             List<BsonDocument> allEquipment = null ;
             allEquipment = CacheHelper.GetCache("MissionItemList") as List<BsonDocument>;
             if (allEquipment == null)
             {
                 allEquipment = dataOp.FindAll("Item").ToList();
                 CacheHelper.SetCache("MissionItemList", allEquipment,null,DateTime.Now.AddMinutes(30));
             }

            var missionIdList = PageReq.GetFormList("missionIds");//_id
            var weixin = PageReq.GetForm("weixin");
            var userId = PageReq.GetForm("userId");
            InvokeResult result = new InvokeResult();
            var curUser = dataOp.FindAllByQuery("SysUser", Query.EQ("weixin", weixin)).FirstOrDefault();
            if (curUser == null)
            {
                result.Message = "当前用户不存在，请刷新重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var missionBsonValueList = TypeConvert.ToObjectIdList(missionIdList.ToList());

            var allMissionList = dataOp.FindAllByQuery("MissionLibrary", Query.In("_id", missionBsonValueList)).ToList();

            var allMissionRewardList = dataOp.FindAllByQuery("MissionReward", Query.In("missionId", missionBsonValueList)).ToList();
            var allMissionRequirementList = dataOp.FindAllByQuery("MissionRequirement", Query.In("missionId", missionBsonValueList)).ToList();
            var curUserItemList = dataOp.FindAllByQuery("PersonItem", Query.EQ("weixin", weixin)).ToList();//人员拥有物品
            var missionStorageDataList = new List<StorageData>();
            var curUserPoint = curUser.Int("point");
            var curUserExp = curUser.Int("exp");
            foreach (var mission in allMissionList)
            {
                var storageDataList = new List<StorageData>();
                if ((curUserPoint + mission.Int("completeRewardPoint") )<= 0)//点数不够,无法完成任务
                {
                    continue;
                }

                var canCompleteMission = true;
                ///获取完成每个任务需要的物品
                var hitMissionRequirement = allMissionRequirementList.Where(c => c.Text("missionId") == mission.Text("_id")).ToList();
                if (hitMissionRequirement.Count() > 0)
                {
                    foreach (var requireItem in hitMissionRequirement)
                    {
                        var curUserItem = curUserItemList.Where(c => c.Text("itemId") == requireItem.Text("itemId")).FirstOrDefault();
                        if(curUserItem!=null&&curUserItem.Int("amount") >= requireItem.Int("amount"))
                        {
                            var actAmount=curUserItem.Int("amount")-requireItem.Int("amount");
                            //更新持有物品
                            storageDataList.Add(new StorageData()
                            {
                                Document = new BsonDocument().Add("amount", actAmount.ToString()),
                                Name = "PersonItem",
                                Query = Query.EQ("_id",ObjectId.Parse(curUserItem.Text("_id"))),
                                Type = StorageType.Update
                            });
                           
                        }
                        else { 
                             canCompleteMission = false;
                             break;
                        }
                    }
                }
                
                if (canCompleteMission)//任务可完成
                { 
                    //获取奖励
                    var missionReward = allMissionRewardList.Where(c => c.Text("missionId") == mission.Text("_id")).ToList();
                    
                    #region 任务状态更新
                    //系统任务不更新状态
                    if (mission.Int("type") != (int)MissionCategory.SystemhMission)
                    {
                        var curComboHit = mission.Int("comboHit") + 1;
                        var curCompleteCount = mission.Int("curCompleteCount") + 1;
                        storageDataList.Add(new StorageData()
                        {
                            Document = new BsonDocument().Add("status", "1").Add("comboHit", curComboHit.ToString()).Add("curCompleteCount", curCompleteCount.ToString()),
                            Name = "MissionLibrary",
                            Query = Query.EQ("missionId", mission.Text("missionId")),
                            Type = StorageType.Update
                        });
                    }
                    #endregion

                    #region 个人物品更新
                    foreach (var rewardItem in missionReward)
                    {

                        var curUserItem = curUserItemList.Where(c => c.Text("itemId") == rewardItem.Text("itemId")).FirstOrDefault();
                        if (curUserItem != null)
                        {
                            var newAmount = curUserItem.Int("amount") + rewardItem.Int("amount");
                            storageDataList.Add(new StorageData()
                            {
                                Document = new BsonDocument().Add("amount", newAmount.ToString()),
                                Name = "PersonItem",
                                Query = Query.EQ("itemId", curUserItem.Text("itemId")),
                                Type = StorageType.Update
                            });
                        }
                        else
                        {
                            var insertItem = new BsonDocument();
                            insertItem.Add("amount", rewardItem.Text("amount"));
                            insertItem.Add("name", rewardItem.Text("name"));
                            insertItem.Add("itemId", rewardItem.Text("itemId"));
                            insertItem.Add("weixin", curUser.Text("weixin"));
                            storageDataList.Add(new StorageData()
                            {
                                Document = insertItem,
                                Name = "PersonItem",
                                Type = StorageType.Insert
                            });

                        }
                    }
                    #endregion

                    #region 更新个人point值,每次连击增加10%
                    //var curMissPoint = mission.Int("completeRewardPoint");
                    var curMissPoint = lifeDayHelper.GetMissionCompointPoint(mission);
                    curUserPoint += curMissPoint;
                    #endregion

                    #region 更新个人point值,每次连击增加10%
                    //var curMissPoint = mission.Int("completeRewardPoint");
                    var curMissExp = lifeDayHelper.GetMissionCompleteExp(curUser,mission);
                    curUserExp += curMissExp;
                    #endregion

                    #region 更新当日个人成就
                    storageDataList.Add(new StorageData()
                    {
                        Document = new BsonDocument().Add("name", mission.Text("name"))
                        .Add("remark", mission.Text("remark")).Add("completeRewardPoint", curMissPoint)
                        .Add("completeDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
                        .Add("weixin", weixin).Add("missionId", mission.Text("_id"))
                        .Add("missionType", mission.Text("missionType"))
                        .Add("type", mission.Text("type")),
                        Name = "PersonAchievement",
                        Type = StorageType.Insert
                    });
                    #endregion

                    #region 爆率物品更新
                    var itemDropCount=mission.Int("itemDropCount");
                    var rareityRand = new Random();//品质随机
                    var curUserLevel = curUser.Int("level");
                  
                   
                    for(var index=1;index<=itemDropCount;index++)
                    {
                          // 普通任务可以获取更高级的物品
                           var equipmentContent = lifeDayHelper.EquipmentDropQuality(curUser.Int("level")+10, rareityRand);
                           if (equipmentContent == null) continue;
                           var    color= equipmentContent.color;
                           var    rareItemClass =equipmentContent.rareDropItemClass;
                           var hitEquipment = equipmentContent.equipmenItem;
                           if (hitEquipment == null) continue;
                           if (string.IsNullOrEmpty(itemMessage.ToString()) && string.IsNullOrEmpty(rareItemMessage.ToString()))
                           {
                               itemMessage.Append("恭喜获得");
                           }
                           if (!string.IsNullOrEmpty(rareItemClass))
                           {
                               //稀有物品获得
                               rareItemMessage.AppendFormat("<span class='{0}'><a href='javascript:;' style='color:{1}'>{2}</a></span>,", rareItemClass, color, hitEquipment.Text("name"));
                           }
                           else
                           {
                               itemMessage.AppendFormat("<a href='javascript:;' style='color:{0}'>{1}</a>,", color, hitEquipment.Text("name"));
                           }

                           var insertItem = new BsonDocument();
                           insertItem.Add("amount", "1");
                           insertItem.Add("name", hitEquipment.Text("name"));
                           insertItem.Add("itemId", hitEquipment.Text("_id"));
                           insertItem.Add("rarity", hitEquipment.Text("rarity"));
                           insertItem.Add("weixin", curUser.Text("weixin"));
                           storageDataList.Add(new StorageData()
                           {
                               Document = insertItem,
                               Name = "PersonItem",
                               Type = StorageType.Insert
                           });
                          
                       }
                   
                   
                    #endregion
                }

                missionStorageDataList.AddRange(storageDataList);
            }
            #region 更新个人Exp值
            if (curUser.Int("exp") != curUserExp)
            {
                if (curUserExp > curUser.Int("exp"))
                itemMessage.AppendFormat(" +{0}EXP", curUserExp - curUser.Int("exp"));
                var updateBoson = new BsonDocument();
                var curUserLevel = curUser.Int("level");
                var nextUserLevel = ++curUserLevel;
                var nexLevel = dataOp.FindOneByKeyVal("PersonLevel", "level", (nextUserLevel).ToString());
                if (nexLevel != null)
                {
                    var nexLevelExp = nexLevel.Int("levelExp");
                    while (curUserExp >= nexLevelExp)//升级
                    {
                        updateBoson.Set("level", (nextUserLevel).ToString());
                        curUserExp -= nexLevelExp;//减少经验
                        nextUserLevel++;
                        nexLevel = dataOp.FindOneByKeyVal("PersonLevel", "level", (nextUserLevel).ToString());
                        if (nexLevel != null)
                        {
                            nexLevelExp = nexLevel.Int("levelExp");
                        }
                        else//无法继续升级
                        {
                            curUserExp = nexLevelExp;//最高级exp
                            break;
                        }
                    }
                }
                if (nextUserLevel < 0)
                {
                    nextUserLevel = 0;
                }
                updateBoson.Add("exp", curUserExp.ToString());
                missionStorageDataList.Add(new StorageData()
                {
                    Document = updateBoson,
                    Name = "SysUser",
                    Query = Query.EQ("userId", curUser.Text("userId")),
                    Type = StorageType.Update
                });
            }


            #endregion

            #region 更新point值
            if (curUser.Int("point") != curUserPoint)
            {
                if (curUserPoint > curUser.Int("point"))
                itemMessage.AppendFormat(",+{0}P", curUserPoint - curUser.Int("point"));
                 missionStorageDataList.Add(new StorageData()
                {
                    Document = new BsonDocument().Add("point", curUserPoint),
                    Name = "SysUser",
                    Query = Query.EQ("userId", curUser.Text("userId")),
                    Type = StorageType.Update
                });
            }
            #endregion
            if (missionStorageDataList.Count() > 0)
            {
                result = dataOp.BatchSaveStorageData(missionStorageDataList);
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "无法完成任务！请重试";
            }
            if (!string.IsNullOrEmpty(itemMessage.ToString()) && result.Status == Status.Successful)
            {
                result.Message =string.Format("<div>{0}喵~</div>",itemMessage.ToString());
            }
            if (!string.IsNullOrEmpty(rareItemMessage.ToString()))
            {
                //result.Message +=rareItemMessage.ToString().TrimEnd(',');
                result.FileInfo = rareItemMessage.ToString().TrimEnd(',') ;//是否爆特殊物品
            }
            
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }


        /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult MissionSave()
        {
       
           InvokeResult result = new InvokeResult();

            string missionTemplateId = PageReq.GetForm("missionTemplateId");
            string name = PageReq.GetForm("name");
            string remark = PageReq.GetForm("remark");
            string missionType = PageReq.GetForm("missionType");
            string completeRewardPoint = PageReq.GetForm("completeRewardPoint");
            string type = PageReq.GetForm("type");
            string failInfluencePoint = PageReq.GetForm("failInfluencePoint");
            string limitedTime = PageReq.GetForm("limitedTime");
            string invalidDate = PageReq.GetForm("invalidDate");
            string difficulty = PageReq.GetForm("difficulty");
            string weixin = PageReq.GetForm("weixin");
            string userId = PageReq.GetForm("userId");
            string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
            DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
            if (string.IsNullOrEmpty(name))
            {
                result.Status = Status.Failed;
                result.Message = "输入参数有误请重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            
            #region 数据验证
            int point = 0;
            if (!int.TryParse(completeRewardPoint, out point))
            {
                result.Status = Status.Failed;
                result.Message = "亲您的金额输入有误，请重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var pointValid=true;
            switch(missionType)//字段类型0每日 1周 2副本 3深渊
            {
                case "0":
                    if (point > 10000)
                    {
                        pointValid = false;
                    }
                    break;
                case "1":
                if (point > 10000)
                    {
                        pointValid = false;
                    }
                  break;
                case "2":
                     if (point > 10000)
                    {
                        pointValid = false;
                    }
                  break;
                   
                case "3":
                  if (point > 10000)
                  {
                      pointValid = false;
                  }
                    break;
            
            }
            if (type!="1"&&point > 10000)//欲望任务可以无限大，其他不能超
            {
                result.Status = Status.Failed;
                result.Message = "亲该任务的金额略夸张啊，能不能好好玩游戏了";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            #endregion

            BsonDocument dataBson = new BsonDocument();
            dataBson.Add("name", name);
            dataBson.Add("missionType", missionType);
            dataBson.Add("remark", remark);
            dataBson.Add("completeRewardPoint", completeRewardPoint);
            dataBson.Add("type", type);
            dataBson.Add("failInfluencePoint", failInfluencePoint);
            dataBson.Add("limitedTime", limitedTime);
            dataBson.Add("invalidDate", invalidDate);
            dataBson.Add("difficulty", difficulty);
            dataBson.Add("weixin", weixin);
            dataBson.Add("userId", userId);
         

            if (!string.IsNullOrEmpty(missionTemplateId))
            {
                var CurObj = dataOp.FindOneByQuery("MissionLibrary", Query.EQ("_id", ObjectId.Parse(missionTemplateId)));

                if (CurObj != null)
                {

                    result = dataOp.Update("MissionLibrary", Query.EQ("_id", ObjectId.Parse(missionTemplateId)), dataBson);

                }
            }
            else
            {
                result = dataOp.Insert("MissionLibrary", dataBson);
            }
      
            
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

         /// <summary>
        /// 保存提交上来的数据
        /// </summary>
        /// <param name="saveForm"></param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [ValidateInput(false)]
        public ActionResult QuickMissionSave()
        {
       
           InvokeResult result = new InvokeResult();

            string missionTemplateId = PageReq.GetForm("missionTemplateId");
          
            string weixin = PageReq.GetForm("weixin");
            string missionType = PageReq.GetForm("missionType");
            string type = PageReq.GetForm("type");
            string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
            DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
            if (string.IsNullOrEmpty(missionTemplateId))
            {
                result.Status = Status.Failed;
                result.Message = "输入参数有误请重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var curMissionTemplate = dataOp.FindOneByQuery("MissionTemplate", Query.EQ("_id", ObjectId.Parse(missionTemplateId)));
            if (curMissionTemplate == null)
            {
                result.Status = Status.Failed;
                result.Message = "输入参数有误请重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            var query=Query.Or(Query.EQ("missionTemplateId", missionTemplateId),Query.EQ("name",curMissionTemplate.Text("name")));
            var curUserMission = dataOp.FindOneByQuery("MissionLibrary", Query.And(Query.EQ("weixin", weixin), query));
            LifeDayHelper helper=new LifeDayHelper(dataOp);
            if (curUserMission == null)//添加
            {

                result=helper.InitialMissionTemplate(weixin,missionTemplateId);
            }
            else //删除
            {
                result=dataOp.Delete("MissionLibrary", Query.EQ("_id", ObjectId.Parse(curUserMission.Text("_id"))));
            }
 
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        


        
        /// <summary>
        /// 快速物品整理
        /// </summary>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QucikPersonItemSell()
        {
            
          
         
           var itemMessage = new StringBuilder();//物品获得信息
           var rareItemMessage = new StringBuilder();//稀有物品获得信息
           string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
           DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
           var lifeDayHelper = new LifeDayHelper(dataOp);

            //缓存
             List<BsonDocument> allEquipment = null ;
             allEquipment = CacheHelper.GetCache("MissionItemList") as List<BsonDocument>;
             if (allEquipment == null)
             {
                 allEquipment = dataOp.FindAll("Item").ToList();
                 CacheHelper.SetCache("MissionItemList", allEquipment,null,DateTime.Now.AddMinutes(30));
             }

            var rarityList = PageReq.GetFormList("rarityIds");//获取品级类型
            var weixin = PageReq.GetForm("weixin");
            
            InvokeResult result = new InvokeResult();
            var curUser = dataOp.FindAllByQuery("SysUser", Query.EQ("weixin", weixin)).FirstOrDefault();
            if (curUser == null)
            {
                result.Message = "当前用户不存在，请刷新重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var curUserItemList = dataOp.FindAllByQuery("PersonItem", Query.And(Query.EQ("weixin", weixin))).Where(c => rarityList.Contains(c.Text("rarity"))).ToList();//人员拥有物品
            var missionStorageDataList = new List<StorageData>();

            var hitItemEquipmentList = allEquipment.Where(c => curUserItemList.Select(d=>d.Text("itemId")).Contains(c.Text("_id"))).ToList();
            if (hitItemEquipmentList.Count() > 0)
            {
                var changePoint = 0;
                //hitItemEquipmentList.Select(c => c.Int("price")).Sum(c => c);
                foreach (var hitItem in hitItemEquipmentList)
                {
                    changePoint += lifeDayHelper.GetEquipmentSellPrice(hitItem);
                }
                if (changePoint > 0)
                {
                     
                    var curUserPoint = curUser.Int("point") + changePoint;
                    var updateBoson = new BsonDocument();
                    updateBoson.Add("point", curUserPoint.ToString());
                    missionStorageDataList.Add(new StorageData()
                    {
                        Document = updateBoson,
                        Name = "SysUser",
                        Query = Query.EQ("userId", curUser.Text("userId")),
                        Type = StorageType.Update
                    });
                }
                var delPersonItemList=from c in hitItemEquipmentList
                                  select new StorageData()
                                    {
                                        
                                        Name = "PersonItem",
                                        Query = Query.EQ("itemId", c.Text("_id")),
                                        Type = StorageType.Delete
                                    };
                missionStorageDataList.AddRange(delPersonItemList);
            }
            if (missionStorageDataList.Count() > 0)
            {
                result = dataOp.BatchSaveStorageData(missionStorageDataList);
            }
         
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PersonItemSell()
        {
            
          
           //var addionMessage = new StringBuilder();//加成信息
           var itemMessage = new StringBuilder();//物品获得信息
           var rareItemMessage = new StringBuilder();//稀有物品获得信息
           string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
           DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
           var lifeDayHelper = new LifeDayHelper(dataOp);

            //缓存
             List<BsonDocument> allEquipment = null ;
             allEquipment = CacheHelper.GetCache("MissionItemList") as List<BsonDocument>;
             if (allEquipment == null)
             {
                 allEquipment = dataOp.FindAll("Item").ToList();
                 CacheHelper.SetCache("MissionItemList", allEquipment,null,DateTime.Now.AddMinutes(30));
             }

             var missionIdList = PageReq.GetFormList("personItemIds");//_id
            var weixin = PageReq.GetForm("weixin");
            
            InvokeResult result = new InvokeResult();
            var curUser = dataOp.FindAllByQuery("SysUser", Query.EQ("weixin", weixin)).FirstOrDefault();
            if (curUser == null)
            {
                result.Message = "当前用户不存在，请刷新重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }

            var curUserItemList = dataOp.FindAllByQuery("PersonItem", Query.And(Query.EQ("weixin", weixin))).Where(c => missionIdList.Contains(c.Text("_id"))).ToList();//人员拥有物品
            var missionStorageDataList = new List<StorageData>();

            var hitItemEquipmentList = allEquipment.Where(c => curUserItemList.Select(d=>d.Text("itemId")).Contains(c.Text("_id"))).ToList();
            if (hitItemEquipmentList.Count() > 0)
            {
                var changePoint = 0;
                //hitItemEquipmentList.Select(c => c.Int("price")).Sum(c => c);
                foreach (var hitItem in hitItemEquipmentList)
                {
                    changePoint += lifeDayHelper.GetEquipmentSellPrice(hitItem);
                }
                if (changePoint > 0)
                {
                    var curUserPoint = curUser.Int("point") + changePoint;
                    var updateBoson = new BsonDocument();
                    updateBoson.Add("point", curUserPoint.ToString());
                    missionStorageDataList.Add(new StorageData()
                    {
                        Document = updateBoson,
                        Name = "SysUser",
                        Query = Query.EQ("userId", curUser.Text("userId")),
                        Type = StorageType.Update
                    });
                }
                var delPersonItemList=from c in hitItemEquipmentList
                                  select new StorageData()
                                    {
                                        
                                        Name = "PersonItem",
                                        Query = Query.EQ("itemId", c.Text("_id")),
                                        Type = StorageType.Delete
                                    };
                missionStorageDataList.AddRange(delPersonItemList);
            }
            if (missionStorageDataList.Count() > 0)
            {
                result = dataOp.BatchSaveStorageData(missionStorageDataList);
            }
         
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult HellChallenge()
        {
            
            
           var itemMessage = new StringBuilder();//物品获得信息
           var rareItemMessage = new StringBuilder();//稀有物品获得信息
           string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
           DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
           var lifeDayHelper = new LifeDayHelper(dataOp);

            //缓存
             List<BsonDocument> allEquipment = null ;
             allEquipment = CacheHelper.GetCache("MissionItemList") as List<BsonDocument>;
             if (allEquipment == null)
             {
                 allEquipment = dataOp.FindAll("Item").ToList();
                 CacheHelper.SetCache("MissionItemList", allEquipment,null,DateTime.Now.AddMinutes(30));
             }

            
            var weixin = PageReq.GetForm("weixin");
            var userId = PageReq.GetForm("userId");
            InvokeResult result = new InvokeResult();
            var curUser = dataOp.FindAllByQuery("SysUser", Query.EQ("weixin", weixin)).FirstOrDefault();
            if (curUser == null)
            {
                result.Message = "当前用户不存在，请刷新重试";
                return Json(TypeConvert.InvokeResultToPageJson(result));
            }
            
            var missionStorageDataList = new List<StorageData>();
            var curUserPoint = curUser.Int("point");
            var curUserExp = curUser.Int("exp");
            var hellChallengeCount = SysAppConfig.Mission_HellChallengeCount;
            var validHellChallengeCount = hellChallengeCount - curUser.Int("execHellChallengeCount");
            var storageDataList = new List<StorageData>();
            if (validHellChallengeCount>0)//可挑战
            {
                    curUserPoint += SysAppConfig.Mission_HellChallengePoint;
                    curUserExp += lifeDayHelper.GetMissionCompleteExp(curUser, new BsonDocument().Add("missionType", "3"));
                    //获取奖励
                    #region 爆率物品更新
                    var itemDropCount=SysAppConfig.Mission_HellItemDropCount;
                    if(itemDropCount<=0)
                    {
                      itemDropCount=1;
                    }
                    if(itemDropCount>8)
                    {
                     itemDropCount=8;
                    }
                    var rareityRand = new Random();//品质随机
                    var curUserLevel = curUser.Int("level");
                   
                    for(var index=1;index<=itemDropCount;index++)
                    {
                        var equipmentContent = lifeDayHelper.EquipmentDropQuality(curUser.Int("level"), rareityRand);//独立计算降低出货
                           if (equipmentContent == null) continue;
                           var    color= equipmentContent.color;
                           var    rareItemClass =equipmentContent.rareDropItemClass;
                           var hitEquipment = equipmentContent.equipmenItem;
                           if (hitEquipment == null) continue;
                           if (string.IsNullOrEmpty(itemMessage.ToString()) && string.IsNullOrEmpty(rareItemMessage.ToString()))
                           {
                               itemMessage.Append("恭喜获得");
                           }
                           if (!string.IsNullOrEmpty(rareItemClass))
                           {
                               //稀有物品获得
                               rareItemMessage.AppendFormat("<span class='{0}'><a href='javascript:;' style='color:{1}'>{2}</a></span>,", rareItemClass, color, hitEquipment.Text("name"));
                           }
                           else
                           {
                               itemMessage.AppendFormat("<a href='javascript:;' style='color:{0}'>{1}</a>,", color, hitEquipment.Text("name"));
                           }

                           var insertItem = new BsonDocument();
                           insertItem.Add("amount", "1");
                           insertItem.Add("name", hitEquipment.Text("name"));
                           insertItem.Add("itemId", hitEquipment.Text("_id"));
                           insertItem.Add("rarity", hitEquipment.Text("rarity"));
                           insertItem.Add("weixin", curUser.Text("weixin"));
                           storageDataList.Add(new StorageData()
                           {
                               Document = insertItem,
                               Name = "PersonItem",
                               Type = StorageType.Insert
                           });
                          
                       }
                    #endregion

                    #region 更新个人Exp值
                    if (curUser.Int("exp") != curUserExp)
                    {
                         //curUserExp = lifeDayHelper.GetLevleAddionalExp(curUser.Int("level"), curUserExp);
                        
                        if (curUserExp > curUser.Int("exp"))
                            itemMessage.AppendFormat(" +{0}EXP", curUserExp - curUser.Int("exp"));
                        var updateBoson = new BsonDocument();
                    
                        var nextUserLevel = ++curUserLevel;
                        var nexLevel = dataOp.FindOneByKeyVal("PersonLevel", "level", (nextUserLevel).ToString());
                        if (nexLevel != null)
                        {
                            var nexLevelExp = nexLevel.Int("levelExp");
                            while (curUserExp >= nexLevelExp)//升级
                            {
                                updateBoson.Set("level", (nextUserLevel).ToString());
                                curUserExp -= nexLevelExp;//减少经验
                                nextUserLevel++;
                                nexLevel = dataOp.FindOneByKeyVal("PersonLevel", "level", (nextUserLevel).ToString());
                                if (nexLevel != null)
                                {
                                    nexLevelExp = nexLevel.Int("levelExp");
                                }
                                else//无法继续升级
                                {
                                    curUserExp = nexLevelExp;//最高级exp
                                    break;
                                }
                            }
                        }
                        if (nextUserLevel < 0)
                        {
                            nextUserLevel = 0;
                        }
                        updateBoson.Add("exp", curUserExp.ToString());
                        missionStorageDataList.Add(new StorageData()
                        {
                            Document = updateBoson,
                            Name = "SysUser",
                            Query = Query.EQ("userId", curUser.Text("userId")),
                            Type = StorageType.Update
                        });
                    }


                    #endregion

                    #region 更新point值
                    if (curUser.Int("point") != curUserPoint)
                    {
                        if (curUserPoint > curUser.Int("point"))
                            itemMessage.AppendFormat(",+{0}P", curUserPoint - curUser.Int("point"));
                        missionStorageDataList.Add(new StorageData()
                        {
                            Document = new BsonDocument().Add("point", curUserPoint),
                            Name = "SysUser",
                            Query = Query.EQ("userId", curUser.Text("userId")),
                            Type = StorageType.Update
                        });
                    }
                    #endregion

                #region 更新helllCount
                    missionStorageDataList.Add(new StorageData()
                    {
                        Document = new BsonDocument().Add("execHellChallengeCount", curUser.Int("execHellChallengeCount") + 1),
                        Name = "SysUser",
                        Query = Query.EQ("userId", curUser.Text("userId")),
                        Type = StorageType.Update
                    });
                #endregion
            }

            missionStorageDataList.AddRange(storageDataList);
           
          
            if (missionStorageDataList.Count() > 0)
            {
                result = dataOp.BatchSaveStorageData(missionStorageDataList);
            }
            else
            {
                result.Status = Status.Failed;
                result.Message = "无法挑战！请重试";
            }
            if (!string.IsNullOrEmpty(itemMessage.ToString()) && result.Status == Status.Successful)
            {
                result.Message =string.Format("<div>{0}喵~</div>",itemMessage.ToString());
            }
            if (!string.IsNullOrEmpty(rareItemMessage.ToString()))
            {
                //result.Message +=rareItemMessage.ToString().TrimEnd(',');
                result.FileInfo = rareItemMessage.ToString().TrimEnd(',') ;//是否爆特殊物品
            }
         
            return Json(TypeConvert.InvokeResultToPageJson(result));
        }
    }
}
