using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;


using System.Xml.Linq;
using System.Xml;

using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Yinhe.ProcessingCenter;


namespace Yinhe.WebHost
{
    /// <summary>
    /// TodoList 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
     [System.Web.Script.Services.ScriptService]
    public class TodoList : System.Web.Services.WebService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="kw"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        [WebMethod]
        public XmlDocument GetTodoList(string loginName,string kw, int pageSize, int pageIndex)
        {
            DataOperation dataOp = new DataOperation();

            //通过登录名，获取用户的审批任务信息
            List<BsonDocument> auditList = dataOp.FindAllByQuery("OAMessage", 
                Query.And(Query.EQ("acceptName", loginName), Query.EQ("auditStatus", "0"))).
                OrderByDescending(x => x.String("createDate")).ToList();
            if (!string.IsNullOrEmpty(kw))
            {
                auditList = auditList.Where(s => s.String("title").Contains(kw)).ToList();
            }
            var sumbitUserIds = auditList.Select(x => x.String("subUserId"));
            var acceptUserIds = auditList.Select(x => x.String("acceptName"));
           
            var sumbitUserInfo = dataOp.FindAllByQuery("SysUser", Query.In("userId", TypeConvert.StringListToBsonValueList(sumbitUserIds)));
            var acceptUserInfo = dataOp.FindAllByQuery("SysUser", Query.In("loginName", TypeConvert.StringListToBsonValueList(acceptUserIds)));

            if (pageIndex < 1)
                pageIndex = 1;
            int totalPage = (int)Math.Ceiling(auditList.Count / (pageSize * 1.0));


            var domain = SysAppConfig.Domain;
            XDocument doc = new XDocument();
            var root = new XElement("WorkData",new XAttribute("name","产品管理系统"),new XAttribute("count",auditList.Count));
            int index = (pageIndex - 1) * pageSize;
            foreach (var item in auditList.Skip((pageIndex-1) * pageSize).Take(pageSize))
            {
                var url = item.String("url");
                url = Yinhe.ProcessingCenter.Common.Base64.EncodeBase64(System.Text.Encoding.GetEncoding("utf-8"), url);
                //url格式：/Account/Login_SSSSO?ReturnUrl=Home/Index

                url = string.Format("{0}{1}{2}", SysAppConfig.Domain, "/Account/Login_SSSSO?ReturnUrl=", url);
                 var curSumbitUser = sumbitUserInfo.FirstOrDefault(x => x.String("userId") == item.String("subUserId"));
                 var acceptUser = acceptUserInfo.FirstOrDefault(x => x.String("loginName") == item.String("acceptName"));
               
                 var element = new XElement("DataPojo",
                     new XElement("DataProperty", ++index, new XAttribute("propertyname", "待办事项编号")),
                     new XElement("DataProperty", item.String("title"), new XAttribute("propertyname", "标题")),
                     new XElement("DataProperty", item.String("title"), new XAttribute("propertyname", "标题描述")),
                     new XElement("DataProperty", url, new XAttribute("propertyname", "待办URL")),
                     new XElement("DataProperty", acceptUser.String("name"), new XAttribute("propertyname", "待办接收人名称")),
                     new XElement("DataProperty", curSumbitUser.String("name"), new XAttribute("propertyname", "待办发起人名称")),
                     new XElement("DataProperty", item.ShortDate("createDate"), new XAttribute("propertyname", "待办发起时间"))
                     );
                 root.Add(element);
            }
            doc.Add(root);
            var temp = new XmlDocument();
            temp.LoadXml(doc.ToString());
            return temp;
        }
    }

    public class TodoInfo 
    {
        /// <summary>
        /// 总的审批任务数量
        /// </summary>
        public int todoCount { get; set; }

        /// <summary>
        /// 分页数量
        /// </summary>
        public int totalPage { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int pageIndex { get; set; }
        public List<Todo> todoList { get; set; }
    }

    public class Todo
    {
        public string subject { get; set; }
        public string subUser { get; set; }
        public string acceptUser { get; set; }
        public string url { get; set; }
        public string createDate { get; set; }
    }

   
}
