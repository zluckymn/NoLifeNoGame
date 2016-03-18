<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<% 
    
    List<string> codeRighList = AuthManage._().AllUserRight.Select(t => t.Code).ToList();
    List<Item> codeItems = null;
    //暂时处理为如果是发布就不进行权限控制
    bool isCheckRight = SysAppConfig.IsPublish == false && AuthManage.UserType != UserTypeEnum.DebugUser;
    if (SysAppConfig.IsPublish == false && AuthManage.UserType != UserTypeEnum.DebugUser)
    {
        codeItems = new List<Item>()
        {
            //产品研发平台
            new Item(){ id=0,name="",value=",,WorkFlowCenter_VIEW,DesignChange_VIEW,MONTHLYPLAN_VIEW,MonthlyPlanReport_VIEW", type="工作平台"},
            new Item(){ id=1,name="EngManage_VIEW",value="EngManage_VIEW", type="设计管理平台"},
            new Item(){ id=2,name="StandardLibraryIndex_VIEW,StandardResultUnit_VIEW,StandardResultLandscape_VIEW,StandardResultDecoration_VIEW,StandardResultFacade_VIEW,StandardResultMonomer_VIEW",value="StandardResultUnit_VIEW,StandardResultPlanning_VIEW,StandardResultFacade_VIEW,StandardResultLandscape_VIEW,StandardResultDecoration_VIEW,StandardResultSalesOffice_VIEW",type="设计资料库"},
            new Item(){ id=3,name="MaterialStorage_VIEW",value="MaterialStorage_VIEW",type="部品材料库"},
            new Item(){ id=4,name="OutSideProjectIndex_VIEW,OutSideProjecMaterial_VIEW",value="OutSideProjectIndex_VIEW,OutSideProjecMaterial_VIEW",type="外部项目库"},
            new Item(){ id=5,name="Designsupplier_VIEW",value="Designsupplier_VIEW",type="设计单位管理"},
            new Item(){ id=6,name="DesignKnowledge_VIEW",value="DesignKnowledge_VIEW",type="设计知识库"},
            new Item(){ id=7,name="Setting_VIEW,UserManage_FULLCONTROLL,OrgManage_FULLCONTROLL,WorkFlowManage_FULLCONTROLL,SystemSettingsPage_FULLCONTROLL,CorpExperience_FULLCONTROLL,PatternManage_FULLCONTROLL,NoticeManage_FULLCONTROLL",value="UserManage_FULLCONTROLL,OrgManage_FULLCONTROLL,,WorkFlowManage_FULLCONTROLL,SystemSettingsPage_FULLCONTROLL,CorpExperience_FULLCONTROLL,PatternManage_FULLCONTROLL,NoticeManage_FULLCONTROLL",type="设计变更管理"},
            new Item(){ id=8,name="QuestionAnswer_VIEW",value="QuestionAnswer_VIEW",type="问答系统"},
            new Item(){ id=9,name="Setting_VIEW",value="UserManage_VIEW,OrgManage_VIEW,GeneralPosition_VIEW,WorkFlowManage_VIEW,SystemSettingsPage_VIEW,CorpExperience_VIEW,PatternManage_VIEW,SubwayMapEditor_VIEW,OnlineTemplate_VIEW,DesignsupplierEvaluateType_VIEW,WorkFlowReport_VIEW,WorkLetterReport_VIEW,PaymentManage_VIEW,WorkFlowApproval_VIEW",type="系统设置"}
        };
    }
    else
    {
        codeItems = new List<Item>()
        {
            //产品研发平台 name:1级导航code value：2级导航code
            new Item(){ id=0,name="",value=",,,,,", type="首页"},
             new Item(){ id=1,name="",value="", type="设计管理平台"},
            new Item(){ id=2,name="",value=",,,,,",type="设计资料库"},
            new Item(){ id=3,name="",value=",,",type="部品材料库"},
            new Item(){ id=4,name="",value=",,",type="外部项目库"},
            new Item(){ id=5,name="",value=",,,,,",type="设计单位管理"},
              new Item(){ id=6,name="",value=",,,,,,,,,",type="设计知识库"},
            new Item(){ id=7,name="",value=",,,,,,,",type="设计变更管理"},
            new Item(){ id=8,name="",value=",,,,,,",type="问答系统"},
            new Item(){ id=9,name="",value=",,,,,,,,,,,,,",type="系统设置"}
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
        secJs.AppendFormat("ArrMenuRight[{0}]=", item.id + 1);
        string secArrCode = string.Empty;
        string[] secArr = item.value.Split(new string[] { "," }, StringSplitOptions.None);
        foreach (var sec in secArr)
        {
            if (string.IsNullOrEmpty(sec) == true)
            {
                secArrCode += ",true";
            }
            else
            {
                secArrCode += "," + codeRighList.Contains(sec).ToString().ToLower();
            }
        }
        if (secArrCode.Length > 0)
        {
            secArrCode = secArrCode.Remove(0, 1);
        }
        secJs.AppendFormat("[{0}];", secArrCode);
    }
    if (codeJs.Length > 0)
    {
      codeJs = codeJs.Remove(0, 1);
    }
%>

<script type="text/javascript">
  var isCheckRight = <%=isCheckRight.ToString().ToLower() %>;
       var ArrMenuRight = new Array();
      ArrMenuRight[0] = [<%=codeJs %>];
      <%=secJs %>
</script>
