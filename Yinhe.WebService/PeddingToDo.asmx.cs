using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Yinhe.ProcessingCenter;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Web.Services.Protocols;
using System.Web.Services.Description;

namespace Yinhe.WebHost.AsynServices
{
    /// <summary>
    /// PeddingToDo 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://10.0.0.53:8080/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [SoapDocumentService(RoutingStyle = SoapServiceRoutingStyle.RequestElement)] 
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class PeddingToDo : System.Web.Services.WebService
    {

        [WebMethod]
        //[SoapRpcMethod(Use = SoapBindingUse.Literal, Action = "http://tempuri.org/HelloWorld", RequestNamespace = "http://tempuri.org/", ResponseNamespace = "http://tempuri.org/")] 
        public string HelloWorld()
        {
            return "Hello World";
        }


        [WebMethod]
       // [SoapRpcMethod(Use=SoapBindingUse.Literal,Action= "http://tempuri.org/HelloWorld", RequestNamespace = "http://tempuri.org/", ResponseNamespace = "http://tempuri.org/")] 
        public List<Pedding> GetPeddingToDoList(string uid)
        {

            DataOperation dataOp = new DataOperation();
            List<Pedding> list = new List<Pedding>();

            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", uid);
            if (user != null)
            {
                string userId = user.Text("userId");
                List<BsonDocument> taskMangerList = dataOp.FindAllByKeyVal("XH_DesignManage_TaskManager", "userId", userId).ToList();
                //获取计划负责人列表
                var planManagerList = dataOp.FindAllByKeyVal("XH_DesignManage_PlanManager", "userId", userId).ToList();
                var taskIds = taskMangerList.Select(c => c.Text("taskId"));
                var planIds = planManagerList.Select(c => c.Text("planId"));
                //获取想
                var AllAssociatTaskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIds.ToList());
                var AllAssociatPlanList = dataOp.FindAllByKeyValList("XH_DesignManage_Plan", "taskId", planIds.ToList());

                //获取未完成的我涉及的任务列表
                var unCompleteTaskList = AllAssociatTaskList.Where(c => c.Int("status") != (int)TaskStatus.Completed).ToList();

                //获取我涉及的关键任务列表
                var keyTaskList = AllAssociatTaskList.Where(c => c.Int("levelId") == 1).Select(c => c.Text("taskId")).ToList();

                //获取我设计的待阅任务列表
                var allInstance = dataOp.FindAll("BusFlowInstance").ToList();
                var queryStr=string.Format("givenUserId={0}&status=0",userId);
                var circulateTaskList = dataOp.FindAllByQueryStr("BusinessFlowCirculation", queryStr);

                var flowInstanceHelper = new Yinhe.ProcessingCenter.BusinessFlow.FlowInstanceHelper();
                var waitMyApprovalInstance = flowInstanceHelper.GetUserWaitForApprovaleFlow(user.Int("userId"));
                var waitMyLaunchFlow = flowInstanceHelper.GetUserWaitForStartFlow(user.Int("userId"));
                var myBusFlowInstance = flowInstanceHelper.GetUserAssociatedFlowInstance(user.Int("userId"));

                List<string> allTaskIdList = new List<string>();
                allTaskIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("referFieldValue")));
                allTaskIdList.AddRange(waitMyLaunchFlow.Select(t => t.String("taskId")));
                allTaskIdList.AddRange(myBusFlowInstance.Select(t => t.String("referFieldValue")));
                var allTask = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", allTaskIdList.Distinct().ToList()).ToList();

                List<string> allFlowIdList = new List<string>();
                allFlowIdList.AddRange(waitMyApprovalInstance.Select(t => t.String("flowId")));
                allFlowIdList.AddRange(waitMyLaunchFlow.Select(t => t.String("flowId")));
                allFlowIdList.AddRange(myBusFlowInstance.Select(t => t.String("flowId")));
                var allFlow = dataOp.FindAllByKeyValList("BusFlow", "flowId", allFlowIdList.Distinct().ToList()).ToList();

                List<string> allProjIdList = new List<string>();
                allProjIdList.AddRange(allTask.Select(t => t.String("projId")));
                allProjIdList.AddRange(AllAssociatTaskList.Select(t => t.String("projId")));
                allProjIdList.AddRange(AllAssociatPlanList.Select(t => t.String("projId")));
                var allProject = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "projId", allProjIdList.Distinct().ToList()).ToList();

                List<BsonDocument> flowList = new List<BsonDocument>();
                List<int> fIds = new List<int>();
                flowList.AddRange(waitMyApprovalInstance);
                flowList.AddRange(waitMyLaunchFlow);



                foreach (var item in unCompleteTaskList)
                {
                    Pedding ped = new Pedding();
                    ped.type = 1;
                    var project = allProject.Where(t => t.Int("projId") == item.Int("projId")).FirstOrDefault();
                    ped.title = string.Format("{0}、{1}、{2}-{3}", project.Text("name"), item.Text("name"), item.ShortDate("curStartData"), item.ShortDate("curEndData"));
                    ped.url = string.Format("/DesignManage/ProjTaskInfo/{0}",item.Int("taskId"));
                    list.Add(ped);
                    //if (!string.IsNullOrEmpty(item.ShortDate("curStartData")) && (DateTime.Now - item.Date("curStartData")).Days <= 2)
                    //{
                    //    list.Add(ped);
                    //} 
                }

                foreach (var item in flowList)
                {
                    var referFieldValue = item.Int("referFieldValue");
                    var taskNode = allTask.Where(c => c.Int("taskId") == referFieldValue).FirstOrDefault();
                    var flowObj = allFlow.Where(c => c.Int("flowId") == item.Int("flowId")).FirstOrDefault();
                    if (!fIds.Contains(item.Int("flowId")))
                        fIds.Add(item.Int("flowId"));
                    Pedding ped = new Pedding();
                    ped.type = 1;
                    var project = allProject.Where(t => t.Int("projId") == taskNode.Int("projId")).FirstOrDefault();
                    ped.title = string.Format("{0}、{1}、{2}", project.Text("name"), taskNode.Text("name"), item.CreateDate());
                    ped.url = string.Format("/DesignManage/TaskWorkFlowInfo/{0}", taskNode.Int("taskId"));
                    if (taskNode.Int("taskId")!=0)
                    list.Add(ped);
                }

                foreach (var item in myBusFlowInstance)
                {
                   
                    var referFieldValue = item.Int("referFieldValue");
                    var taskNode = allTask.Where(c => c.Int("taskId") == referFieldValue).FirstOrDefault();
                    var flowObj = allFlow.Where(c => c.Int("flowId") == item.Int("flowId")).FirstOrDefault();
                    bool flag = flowInstanceHelper.CanExecute(item.Text("flowId"), item.Text("flowInstanceId"), user.Int("userId"));
                    Pedding ped = new Pedding();
                    ped.type=flag == true ? 1: 2;
                    var project = allProject.Where(t => t.Int("projId") == taskNode.Int("projId")).FirstOrDefault();
                    ped.title = string.Format("{0}、{1}、{2}", project.Text("name"), taskNode.Text("name"), item.CreateDate());
                    ped.url = string.Format("/DesignManage/TaskWorkFlowInfo/{0}", taskNode.Int("taskId"));
                    if (!fIds.Contains(item.Int("flowId")))
                        if (taskNode.Int("taskId") != 0)
                    list.Add(ped);
                }
                //代阅
                foreach (var circulate in circulateTaskList)
                {
                    var item = allInstance.Where(c => c.Int("flowInstanceId") == circulate.Int("flowInstanceId")).FirstOrDefault();
                    if (item == null) continue;
                    var referFieldValue = item.Int("referFieldValue");
                    var taskNode = allTask.Where(c => c.Int("taskId") == referFieldValue).FirstOrDefault();
                    var flowObj = allFlow.Where(c => c.Int("flowId") == item.Int("flowId")).FirstOrDefault();
                    //bool flag = flowInstanceHelper.CanExecute(item.Text("flowId"), item.Text("flowInstanceId"), user.Int("userId"));
                    Pedding ped = new Pedding();
                   // ped.type=flag == true ? 1: 2;
                    ped.type = 2;//不能编辑
                    var project = allProject.Where(t => t.Int("projId") == taskNode.Int("projId")).FirstOrDefault();
                    ped.title = string.Format("【代阅任务】{0}、{1}、{2}", project.Text("name"), flowObj.Text("name"), item.CreateDate());
                    ped.url = string.Format("/DesignManage/TaskWorkFlowInfo/{0}", taskNode.Int("taskId"));
                    if (!fIds.Contains(item.Int("flowId")))
                        if (taskNode.Int("taskId") != 0)
                    list.Add(ped);
                }
                
              
                
            }
            return list;
        }
    }

    public class Pedding
    {
       private string _title;

        public string title
        {
        get { return _title; }
        set { _title = value; }
        }
            private string _url;

        public string url
        {
        get { return _url; }
        set { _url = value; }
        }

        private int _type;

        public int type
        {
            get { return _type; }
            set { _type = value; }
        }


    }
}
