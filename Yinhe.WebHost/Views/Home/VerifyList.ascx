<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<% 
    //int currentUserId = this.CurrentUserId;
    string loginName = PageReq.GetString("loginName");
    string Ticket = PageReq.GetString("Ticket");
    List<BsonDocument> auditList = dataOp.FindAllByQuery("OAMessage", Query.And(Query.EQ("acceptName", loginName), Query.EQ("auditStatus", "0"))).OrderByDescending(x => x.String("createDate")).ToList();
    List<string> sumbitUserId = auditList.Select(x => x.String("subUserId")).Distinct().ToList();
    List<BsonDocument> sumbitUserInfo = new List<BsonDocument>();
    if (sumbitUserId.Count() > 0)
    {
        sumbitUserInfo = dataOp.FindAllByQuery("SysUser", Query.In("userId", TypeConvert.StringListToBsonValueList(sumbitUserId))).ToList();
    }
    var domain = SysAppConfig.Domain;
%>

<style>
  #mytable {   
    padding: 0;
    margin: 0;   
    border-collapse:collapse;
}

td {
    border: 1px solid #C1DAD7;   
    background: #fff;
    font-size:11px;
    padding: 6px 6px 6px 12px;
    color: #4f6b72;
}

td.alt {
    background: #F5FAFA;
    color: #797268;
}
</style>
<div>
    <table id="mytable">
        <col style="width: 40px" />
        <col />
        <col style="width: 120px" />
        <col style="width: 120px" />
        <col style="width: 120px" />
        <thead>
            <tr>
                <td>
                    序号
                </td>
                <td>
                    名称
                </td>
                <td>
                    提交时间
                </td>
                <td>
                    提交人
                </td>
                <td>
                    操作
                </td>
            </tr>
        </thead>
        <tbody>
            <% int index = 0;
               foreach (var tempMes in auditList)
               {
                   index++;
                   string url = string.Format("{0}{1}", domain, tempMes.String("url"));
                   var curSumbitUser = sumbitUserInfo.Where(x => x.String("userId") == tempMes.String("subUserId")).FirstOrDefault();
            %>
            <tr>
                <td>
                    <%=index %>
                </td>
                <td>
                   <a href="<%=url %>"> <%=tempMes.String("title")%></a>
                </td>
                <td>
                    <%=tempMes.ShortDate("createDate")%>
                </td>
                <td>
                    <%=curSumbitUser.String("name")%>
                </td>
                <td>
                    <a href="<%=url %>">点击审核</a>
                </td>
            </tr>
            <%} %>
        </tbody>
    </table>
</div>
