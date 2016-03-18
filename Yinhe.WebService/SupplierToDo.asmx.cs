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

namespace Yinhe.WebService
{
    /// <summary>
    /// SupplierToDo 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class SupplierToDo : System.Web.Services.WebService
    {

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [WebMethod]
        // [SoapRpcMethod(Use=SoapBindingUse.Literal,Action= "http://tempuri.org/HelloWorld", RequestNamespace = "http://tempuri.org/", ResponseNamespace = "http://tempuri.org/")] 
        public List<Supplier> GetSupplierToDoList(string uid)
        {

            DataOperation dataOp = new DataOperation();
            List<Supplier> list = new List<Supplier>();

            BsonDocument user = dataOp.FindOneByKeyVal("SysUser", "loginName", uid);
            if (user != null)
            {
                string userId = user.Text("userId");
                //取供应商审批流程的方法待改进
                //找到供应商审批流程下的每个动作对应的审批人
                //List<BsonDocument> busStepList = dataOp.FindAllByQuery("InstanceActionUser", Query.And(Query.EQ("flowId", "117"), Query.EQ("userId", userId))).ToList();
                var supplierFlowList = dataOp.FindAllByQuery("BusFlow", Query.EQ("isDesignSupplier", "1"));//找出流程
                var supplierFlowIds = supplierFlowList.Select(c => c.String("flowId")).Distinct().ToList();
                List<BsonDocument> busStepList = dataOp.FindAllByKeyValList("InstanceActionUser", "flowId", supplierFlowIds).Where(c => c.Int("status")==0&&c.String("userId") == userId).ToList();
                var instanceIds = busStepList.Select(c => c.String("flowInstanceId")).Distinct().ToList(); //查找当前用户关联的流程实例
                List<BsonDocument> instanctList = dataOp.FindAllByKeyValList("BusFlowInstance", "flowInstanceId", instanceIds).Where(c => c.Text("instanceStatus")=="0").ToList();
                foreach (var ins in instanctList)
                {
                    //判断用户是否是流程当前步骤的审批人
                    var temObj = busStepList.Where(c => c.Text("flowInstanceId") == ins.Text("flowInstanceId") && c.Text("stepId") == ins.Text("stepId")).FirstOrDefault();
                    if (temObj != null)
                    {
                        var supplier = dataOp.FindOneByQuery("XH_Supplier_Supplier", Query.EQ("supplierId", ins.Text("referFieldValue"))); //查找供应商
                        if (supplier != null)
                        {
                            Supplier ped = new Supplier();
                            ped.title = "供应商：" + supplier.Text("name") + "的入库审批";
                            ped.url = string.Format("/DesignManage/SuplierWorkFlowInfo/?supplierId={0}&supplierApproveId={1}&Isapproval=%u540C%u610F", supplier.Int("supplierId"),ins.Text("flowId"));
                            list.Add(ped);
                        }
                    }
                }

            }
            return list;
        }
    }
    public class Supplier
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
    }
}
