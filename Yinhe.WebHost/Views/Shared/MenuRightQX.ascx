<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<% 
    
    List<string> codeRighList = AuthManage._().AllUserRight.Select(t => t.Code).ToList();
    List<Item> codeItems = null;
    //暂时处理为如果是发布就不进行权限控制
    bool isCheckRight = SysAppConfig.IsPublish == false && AuthManage.UserType != UserTypeEnum.DebugUser;
    if (SysAppConfig.IsPublish == false && AuthManage.UserType!= UserTypeEnum.DebugUser)
    {
        codeItems = new List<Item>()
        {
            //产品研发平台
            new Item(){ id=0,name="",value="", type="我的桌面"},
            new Item(){ id=1,name="PRODUCTDEVPLAT_VIEW",value="PRODUCTDEVPLAT_VIEW,PRODUCTDEVPLAT_ADMIN",type="产品系列"},
            new Item(){ id=2,name="PROJECTLIB_VIEW",value=",PROJECTLIB_ADMIN",type="项目库"},
            new Item(){ id=3,name="DESIGNCHANGE_VIEW",value=",",type="设计变更管理"},
            new Item(){ id=4,name="MATERIALLIB_VIEW",value=",MATERIALLIB_ADMIN",type="部品材料库"},
            new Item(){ id=5,name="STRUCTURELIB_VIEW",value="STRUCTURELIB_VIEW,STRUCTURELIB_ADMIN",type="构造做法"},
            new Item(){ id=6,name="DESIGNPROLIB_VIEW",value=",DESIGNPROLIB_ADMIN",type="设计供应商"},
            
            
            new Item(){ id=7,name="SYSTEMSETTING_VIEW",value="USERMANAGE_VIEW,DEPJOBRMANAGE_VIEW,COMMONPOST_VIEW,FLOWCENTRE_VIEW,ROLEPOWER_VIEW,BUGTYPEPART_VIEW,MATERIALPURCHASE_VIEW,HOMEPAGE_VIEW,RESEARCHPRO_VIEW,PREPARATION_VIEW",type="系统设置"},
            //new Item(){ id=8,name="",value="",type="豪宅设备"},
            //new Item(){ id=9,name="",value="",type="广州豪宅数据"},
            //new Item(){ id=10,name="SYSTEMSETTING_VIEW",value="SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,",type="系统设置"}
        };
    }
    else {
        codeItems = new List<Item>()
        {
            //产品研发平台
            new Item(){ id=0,name="",value="", type="工作台"},
            new Item(){ id=1,name="",value=",,,,", type="产品系列管理"},
            new Item(){ id=2,name="",value=",,,", type="项目资料管理"},
            new Item(){ id=3,name="",value=",",type="设计变更管理"},
            new Item(){ id=4,name="",value=",,,",type="部品材料库"},
            new Item(){ id=5,name="",value=",,,,,,",type="构造做法"},
            new Item(){ id=6,name="" ,value=",,,,,,,",type="设计供应商"},
            
            
            //new Item(){ id=5,name="",value=",,,",type="研发基础数据库"},
            //new Item(){ id=6,name="",value=",,",type="土地库"},
            //new Item(){ id=7,name="",value=",,",type="客群库"},
            //new Item(){ id=7,name="",value="",type="高端住宅设备"},
            //new Item(){ id=8,name="",value="",type="广州豪宅销售情况"},
            new Item(){ id=7,name="",value=",,,,,,,,,,,,,,",type="系统设置"}
        };
    }

    //string codeJs = ",true";
    string codeJs = string.Empty;
    StringBuilder secJs = new StringBuilder();
    foreach (var item in codeItems)
    {
        string[] arrFirstCode = item.name.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        if (arrFirstCode.Length == 1)
        {
            codeJs += "," + codeRighList.Contains(item.name).ToString().ToLower();//判断是否有权限
        }
        else if (arrFirstCode.Length == 0)
        {
            codeJs += ",true";
        }
        else
        {
            bool hasContains = false;
            foreach (var fc in arrFirstCode)
            {
                hasContains = codeRighList.Contains(fc);
                if (hasContains == true)
                {
                    break;
                }
            }
            codeJs += "," + hasContains.ToString().ToLower();
        }
        secJs.AppendFormat("ArrMenuRight[{0}]=", item.id + 1);
        string secArrCode = string.Empty;
        string[] secArr = item.value.Split(new string[]{","},StringSplitOptions.None);
        foreach(var sec in secArr)
        {
            if (string.IsNullOrEmpty(sec) == true)
            {
                secArrCode +=",true";
            }else{
                secArrCode +=","+codeRighList.Contains(sec).ToString().ToLower();
            }
        }
        if(secArrCode.Length>0)
        {
            secArrCode = secArrCode.Remove(0,1);
        }
         secJs.AppendFormat("[{0}];",secArrCode);
    }
    if (codeJs.Length > 0)
    {
        codeJs = codeJs.Remove(0, 1);
    }
    
 %>
    <%--<script type="text/javascript">
//       var ArrMenuRight = new Array();
//       ArrMenuRight[0] = [<%=codeJs %>];
////      <%=secJs %>
//      //重新定义。 因 2012.7.5  1、加设计管理平台菜单 2、标准库少了设备与技术库  3、部品材料库少了一个供应商库 editby Mr 林

//      ArrMenuRight[3] = [true,true];
//      ArrMenuRight[4] = [true,true,true,true,true,true,true,true,true];
//       ArrMenuRight[5] = [true,true,true,true,true,true];
        //       ArrMenuRight[10] = [true,true,true,true,true,true];
        var ArrMenuRight = new Array();
        ArrMenuRight[0] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[1] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[2] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[3] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[4] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[5] = [true, true, true, true, true, true, true, true, true, true];

        ArrMenuRight[6] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[7] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[8] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[9] = [true, true, true, true, true, true, true, true, true, true];
        ArrMenuRight[10] = [true, true, true, true, true, true, true, true, true, true];
      
</script>
--%>
<script type="text/javascript">
  var isCheckRight = <%=isCheckRight.ToString().ToLower() %>;
       var ArrMenuRight = new Array();
      ArrMenuRight[0] = [<%=codeJs %>];
      <%=secJs %>
</script>