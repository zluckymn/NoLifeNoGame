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
            new Item(){ id=1,name="PRODUCTDEVPLAT_VIEW_PRODUCTSERIES",value="PRODUCTDEVPLAT_VIEW_PRODUCTSERIES,PRODUCTDEVPLAT_ADMIN_PRODUCTSERIES", type="产品研发平台"},
            new Item(){ id=2,name="PROJECTLIB_VIEW_PROJECT,PROJSTRUE_VIEW_PROJSTRUE",value="PROJECTLIB_VIEW_PROJECT,PROJSTRUE_VIEW_PROJSTRUE",type="设计管理平台"},
            new Item(){ id=3,name="UNITLIB_VIEW_UNIT,DECORATIONLIB_VIEW_DECORATION,FACADELIB_VIEW_FACADE,DEMAREALIB_VIEW_DEMAREA,LANDSCAPELIB_VIEW_LANDSCAPE,CRAFTSLIB_VIEW_CRAFTS,PARTSLIB_VIEW_PARTS,DEVICELIB_VIEW_DEVICE"
             ,value="UNITLIB_VIEW_UNIT,DECORATIONLIB_VIEW_DECORATION,FACADELIB_VIEW_FACADE,DEMAREALIB_VIEW_DEMAREA,LANDSCAPELIB_VIEW_LANDSCAPE,CRAFTSLIB_VIEW_CRAFTS,PARTSLIB_VIEW_PARTS,DEVICELIB_VIEW_DEVICE",type="标准化成果库"},
            new Item(){ id=4,name="MATERIALLIB_VIEW_MATERIAL",value="MATERIALCAT_VIEW_MATCAT,MATERIALBASE_VIEW_MATBASE,MATERIALBRAND_VIEW_MATBRAND,MATERIALPRO_VIEW_MATPROVIDER",type="部品材料库"},
            new Item(){ id=5,name="DESIGNPROLIB_VIEW_DESINGPRO",value="DESIGNPROLIB_VIEW_DESINGPRO,DESIGNPROLIB_ADMIN_DESIGNPROCOM",type="供应商管理平台"},
            new Item(){ id=6,name="STANDARFILELIB_VIEW_STANDARFILE,DISDESIGNLIB_VIEW_DISDESIGN,BENCHMARKLIB_VIEW_BENCHCASE",value="STANDARFILELIB_VIEW_STANDARFILE,DISDESIGNLIB_VIEW_DISDESIGN,BENCHMARKLIB_VIEW_BENCHCASE",type="研发基础数据库"},
            new Item(){ id=7,name="LANDLIB_VIEW_LAND",value="LANDLIB_VIEW_LAND,LANDLIB_ADMIN_LAND",type="土地库"},
            new Item(){ id=8,name="CUSTLIB_VIEW_CUST",value="CUSTLIB_VIEW_CUST,CUSTLIB_ADMIN_CUST",type="客群库"},
            new Item(){ id=9,name="SYSTEMSETTING_VIEW",value="SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW,SYSTEMSETTING_VIEW",type="系统设置"}
        };
    }
    else {
        codeItems = new List<Item>()
        {
            //产品研发平台
            new Item(){ id=1,name="",value=",", type="产品研发平台"},
            new Item(){ id=2,name="",value="",type="设计管理平台"},
            new Item(){ id=3,name=""
             ,value=",,,,,,,",type="标准化成果库"},
            new Item(){ id=4,name="",value=",,,",type="部品材料库"},
            new Item(){ id=5,name="",value=",",type="供应商管理平台"},
            new Item(){ id=6,name="",value=",,,",type="研发基础数据库"},
            new Item(){ id=7,name="",value=",,",type="土地库"},
            new Item(){ id=8,name="",value=",,",type="客群库"},
            new Item(){ id=9,name="",value=",,,,,",type="系统设置"}
        };
    }

    string codeJs = ",true";
    StringBuilder secJs = new StringBuilder();
    foreach (var item in codeItems)
    {
        string[] arrFirstCode = item.name.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        if (arrFirstCode.Length == 1)
        {
            codeJs += "," + codeRighList.Contains(item.name).ToString().ToLower();
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
        secJs.AppendFormat("ArrMenuRight[{0}]=",item.id+1);
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