<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.DataRule" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="System.IO" %>
<style type="text/css">
    .red{background-color:#FF4500;color:White}
    .gray{background-color:#E8E8E8}
    .green{background-color:#458b00;color:White}
    .tableborder tr td{ padding:5px}
</style>
<% 
    //获取表规则里所有的表名
    var allRules = TableRule.GetAllTables();
    var allTempRules=allRules.Select(i=>new {
        tbName=i.Name,
        key=i.GetPrimaryKey()
    }).ToList();
    var ruleTbNames = allTempRules.Select(i => i.tbName).ToList();
    

    DataOperation dataOp = new DataOperation();
    var pkCounter = dataOp.FindAll("TablePKCounter").ToList();
    //获取主键表里所有的表名
    var counterTbNames = pkCounter.Select(i => i.Text("tbName")).ToList();
    
    //数据库里实际存储的表名
    List<string> filterNames = new List<string>() { "system.indexes", "system.users", "SysAssoDataLog", "SysBehaviorLog", "TablePKCounter" };
    MongoOperation mongoOp = new MongoOperation();
    var factTbNames = mongoOp.GetDataBase().GetCollectionNames().Except(filterNames).ToList();

    //var allTbNames = ruleTbNames.Concat(counterTbNames).Concat(factTbNames).Distinct().OrderBy(i=>i).ToList();
    var allTbNames = counterTbNames.Concat(factTbNames).Distinct().OrderBy(i => i).ToList();
     %>
<a class="btn_04" onclick="syncPK(this);" style="cursor:pointer">同步主键<span></span></a>
<p>当主键表中所存的的值小于对应表中实际最大主键值时<br />
修改主键表中的值为该最大值</p>

<table class="tableborder">
    <thead>
        <tr>
        <th>表名</th>
        <th>表规则</th>
        <th>主键表</th>
        <th>实际数据库</th>
        <th>重复主键数</th>
    </tr>
    </thead>
    
    <% 
        foreach (var tbName in allTbNames)
        {
            var key = string.Empty;
            var ruleStatus = string.Empty;
            var counterStatus = string.Empty;
            var factStatus = string.Empty;
            var curMax=0;
            var factMax=0;
            string className = string.Empty;
            string keyClass = string.Empty;
            var dumpKeys = new List<int>();
            string dumpKeyStatus = string.Empty;
            var rule = allTempRules.Where(i => i.tbName == tbName).FirstOrDefault();
            if (rule != null)
            {
                key = rule.key ?? string.Empty;
                ruleStatus = key;
            }
            var counter = pkCounter.Where(i => i.Text("tbName") == tbName).FirstOrDefault();
            if (!counter.IsNullOrEmpty())
            {
                curMax = counter.Int("count");
                counterStatus = curMax.ToString();
            }
            if (factTbNames.Contains(tbName))
            {
                var allDatas = dataOp.FindAll(tbName).SetFields(key).ToList();

                if (!counter.IsNullOrEmpty() && allDatas.Count > 0)
                {
                    factMax = allDatas.Max(i => i.Int(key));
                    factStatus = factMax.ToString();
                    if (curMax < factMax)
                    {
                        className = "red";
                    }
                }
                if (!string.IsNullOrWhiteSpace(key))
                {
                    dumpKeys = allDatas.GroupBy(i => i.Int(key))
                    .Where(i => i.Count() > 1).Select(i => i.Key).ToList();
                    if (dumpKeys.Count > 0)
                    {
                        dumpKeyStatus = dumpKeys.Count.ToString();
                        keyClass = "green";
                    }
                }
            }
         %>
         <tr>
            <td><%=tbName %></td>
            <td><%=ruleStatus %></td>
            <td class="<%=className %>"><%=counterStatus%></td>
            <td><%=factStatus %></td>
            <td class="<%=keyClass %>"><%=dumpKeyStatus %></td>
         </tr>
         <% if (dumpKeys.Count > 0){ %>
         <tr><td colspan="5">重复主键值： <%=string.Join(",",dumpKeys) %></td></tr>
         <% } %>
    <% } %>
</table>

<script type="text/javascript">
    var canOpr = true;
    function syncPK(obj) {
        if (!canOpr) {
            return false;
        }
        canOpr = false;
        var $p = $(obj).parent();
        $p.mask("loading...");
        $.post("/Home/SyncPK", {}, function () {
            $p.load('/Home/PKCounterStatistics?r=' + new Date().getTime(), function () {
                $p.unmask();
                canOpr = true;
            });
        });
    }
</script>
