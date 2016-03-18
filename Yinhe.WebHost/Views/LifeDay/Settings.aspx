<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/BootStrap_flatui.Master"
    Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    生活日常
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <%
        var addition = SysAppConfig.Mission_PointAddition;//10%加成
        var maxAddition = SysAppConfig.Mission_MaxPointAddition;//最高加成70&
        var type = PageReq.GetParamInt("type");
        string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
        DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);

        var fixValue = 9;//修正值
        var killMonsterSecond =7;//杀掉怪物需要时间
        var maxLevel = 85;
        var curExp = 0;

        //List<Yinhe.ProcessingCenter.DataRule.StorageData> expData = new List<Yinhe.ProcessingCenter.DataRule.StorageData>();
        //for (var curLevel = 1; curLevel <= maxLevel; curLevel++)
        //{
        //    var threeS = (curLevel - 1) * (curLevel - 1) * (curLevel - 1);
        //    curExp = (threeS + fixValue) / 5 * ((curLevel - 1) * 2 + fixValue);
        //    expData.Add(new Yinhe.ProcessingCenter.DataRule.StorageData()
        //    {
        //        Document = new BsonDocument().Add("level", curLevel.ToString()).Add("levelExp", curExp.ToString()),
        //        Name = "PersonLevel",
        //        Type = Yinhe.ProcessingCenter.DataRule.StorageType.Insert
        //    });
        //}
        //if (expData.Count() > 0)
        //{
        //    dataOp.BatchSaveStorageData(expData);
        //}
        
        List<Yinhe.ProcessingCenter.DataRule.StorageData> expData = new List<Yinhe.ProcessingCenter.DataRule.StorageData>();
    
        //if (expData.Count() > 0)
        //{
        //    dataOp.BatchSaveStorageData(expData);
        //}
    %>
  

  <%if(type==1){ %>
    <%    
         var rand = new Random();
        for (var curLevel = 1; curLevel <= maxLevel; curLevel++)
          {
              var equipmentCode = rand.Next(1000000);
              
              %>
              <%=curLevel%>.<%=equipmentCode%>
                            <%if(equipmentCode >= 975001)
                                  {%><strong>ss</strong><%} %></br>
             
              <%
            
          } %>
          <%} %>
          <%if(type==2){
            var fixValueExp = 9;//修正值
            var missionFixValueExp = 1;
           for (var curLevel = 1; curLevel <= maxLevel; curLevel++)
             {
                var threeS = (curLevel - 1) * (curLevel - 1) * (curLevel - 1);
                curExp = (threeS + fixValue) / 5 * ((curLevel - 1) * 2 + fixValueExp);

                var threeB = (curLevel - 1) * (curLevel - 1) ;
                var curMissionExp = (threeB + missionFixValueExp) / 5 * ((curLevel - 1) * 2 + missionFixValueExp);
                if (curMissionExp == 0)
                {
                    curMissionExp = 10;
                }
                 %>
                <%=curLevel%>:<%= curExp%>_<%=curMissionExp%>_<%=curExp/curMissionExp%><br/>
        
          <%} %>
          <%} %>
          <%if (type == 3)
            {
                var pathDict=new Dictionary<string,string>();
                 pathDict.Add("iconarmor",@"/Content/LifeDay/sprite_item/iconarmor");
                 pathDict.Add("iconacc",@"/Content/LifeDay/sprite_item/iconacc");
                 pathDict.Add("iconweapon",@"/Content/LifeDay/sprite_item/iconweapon");
                 pathDict.Add("iconset",@"/Content/LifeDay/sprite_item/iconset");
                 pathDict.Add("coat",@"/Content/LifeDay/sprite_item_common/coat");
                 pathDict.Add("belt",@"/Content/LifeDay/sprite_item_common/belt");
                 pathDict.Add("bracelet",@"/Content/LifeDay/sprite_item_common/bracelet");
                 pathDict.Add("pants",@"/Content/LifeDay/sprite_item_common/pants");
                 pathDict.Add("shoes",@"/Content/LifeDay/sprite_item_common/shoes");
                 pathDict.Add("shoulder",@"/Content/LifeDay/sprite_item_common/shoulder");
                 pathDict.Add("support",@"/Content/LifeDay/sprite_item_common/support");
                 pathDict.Add("ring",@"/Content/LifeDay/sprite_item_common/ring");
                 pathDict.Add("necklace",@"/Content/LifeDay/sprite_item_common/necklace");
                 pathDict.Add("magicstone",@"/Content/LifeDay/sprite_item_common/magicstone");
                 pathDict.Add("sswd", @"/Content/LifeDay/sprite_item_weapon_swordman/sswd");
                 pathDict.Add("beamswd", @"/Content/LifeDay/sprite_item_weapon_swordman/beamswd");
                 pathDict.Add("club", @"/Content/LifeDay/sprite_item_weapon_swordman/club");
                 pathDict.Add("katana", @"/Content/LifeDay/sprite_item_weapon_swordman/katana");
                 pathDict.Add("lswd", @"/Content/LifeDay/sprite_item_weapon_swordman/lswd");
                //格斗
                 pathDict.Add("bglove", @"/Content/LifeDay/sprite_item_weapon_fighter/bglove");
                 pathDict.Add("claw", @"/Content/LifeDay/sprite_item_weapon_fighter/claw");
                 pathDict.Add("gauntlet", @"/Content/LifeDay/sprite_item_weapon_fighter/gauntlet");
                 pathDict.Add("knuckle", @"/Content/LifeDay/sprite_item_weapon_fighter/knuckle");
                 pathDict.Add("tonfa", @"/Content/LifeDay/sprite_item_weapon_fighter/tonfa");

                 //gunner
                 pathDict.Add("automatic", @"/Content/LifeDay/sprite_item_weapon_gunner/automatic");
                 pathDict.Add("bowgun", @"/Content/LifeDay/sprite_item_weapon_gunner/bowgun");
                 pathDict.Add("hcannon", @"/Content/LifeDay/sprite_item_weapon_gunner/hcannon");
                 pathDict.Add("musket", @"/Content/LifeDay/sprite_item_weapon_gunner/musket");
                 pathDict.Add("revolver", @"/Content/LifeDay/sprite_item_weapon_gunner/revolver");

                 //mage
                 var chartacter="mage";
                 pathDict.Add("broom", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/broom", chartacter));
                 pathDict.Add("pole", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/pole", chartacter));
                 pathDict.Add("rod", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/rod", chartacter));
                 pathDict.Add("spear", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/spear", chartacter));
                 pathDict.Add("staff", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/staff", chartacter));

                 //priest
                 chartacter = "priest";
                 pathDict.Add("axe", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/axe", chartacter));
                 pathDict.Add("cross", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/cross", chartacter));
                 pathDict.Add("rosary", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/rosary", chartacter));
                 pathDict.Add("scythe", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/scythe", chartacter));
                 pathDict.Add("totem", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/totem", chartacter));

                 //thief
                 chartacter = "thief";
                 pathDict.Add("chakraweapon", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/chakraweapon", chartacter));
                 pathDict.Add("dagger", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/dagger", chartacter));
                 pathDict.Add("twinswd", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/twinswd", chartacter));
                 pathDict.Add("wand", string.Format(@"/Content/LifeDay/sprite_item_weapon_{0}/wand", chartacter));
                
                
                 var ssLandMark = @"/Content/LifeDay/sprite_item/iconmark/0.png";
                var allItemList = dataOp.FindAll("Item").Where(c=>c.Int("rarity")==3).ToList();
                List<Yinhe.ProcessingCenter.DataRule.StorageData> equipData = new List<Yinhe.ProcessingCenter.DataRule.StorageData>();
                var equipPart = string.Empty;
                foreach (var item in allItemList)
                {
                    
                    var url = item.Text("url");
                    var urlSplitArr = url.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
                    if (urlSplitArr.Length >= 2)
                    {
                        var position = urlSplitArr[0].ToLower();
                        ///0001.png 
                        var pngPath = urlSplitArr[1].TrimStart('0');
                        if (pngPath.IndexOf(".") == 0)
                        {
                            pngPath = "0" + pngPath;
                        }
                        if (pathDict.ContainsKey(position))
                        {
                            var fixPath = string.Format("{0}/{1}", pathDict[position], pngPath);
                            equipData.Add(new Yinhe.ProcessingCenter.DataRule.StorageData()
                            {
                                Document = new BsonDocument().Add("equipCategory", position).Add("fixUrl", fixPath),
                                Name = "Item",
                                Query=Query.EQ("_id",BsonObjectId.Parse(item.Text("_id"))),
                                Type = Yinhe.ProcessingCenter.DataRule.StorageType.Update
                            });
                          
                        %>
             
                        <%=item.Text("name")%><%=position%><%=fixPath%>
                        
                        <p  style=" background-image:url(<%=fixPath%>); width:28px; height:28px;  ">
                         <img style=" float:left; width:28px; height:28px; " src="<%=ssLandMark %>"></img>
                         </p>
                        
                        <%}
                        else
                        { %>
                         <%=item.Text("name")%>:<%=url%>2
                        <%} %>
                        <%
                    }
                    else
                    { 
                         %>
                           <%=item.Text("name")%>:<%=url%>
                         <%
                    }
                }
                if (equipData.Count() > 0)
                {
                  //  dataOp.BatchSaveStorageData(equipData);
                }   
                %>

                
          <%} %>
</asp:Content>
