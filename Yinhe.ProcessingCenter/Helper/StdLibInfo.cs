using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 标准库内置数据处理类
    /// </summary>
    public class StdLibInfo
    {
        public static readonly Dictionary<int, StdInfos> stdInfoDict = new Dictionary<int, StdInfos>{
                 {1,new StdInfos{editUrl="/StandardResult/UnitEdit/",viewUrl="/StandardResult/UnitView/",code="",libId=1,isUnit=true,isOne=false}},//户型库 fileObjId=46 
                 {2,new StdInfos{editUrl="/StandardResult/LandscapeEdit/",viewUrl="/StandardResult/LandscapeView/",code="",libId=2,isUnit=true,isOne=false}},//景观库 fileObjId=21
                 {3,new StdInfos{editUrl="/StandardResult/IndoorEdit/",viewUrl="/StandardResult/IndoorView/",code="",libId=3,isUnit=true,isOne=false}},//室内库 fileObjId=48
                 {4,new StdInfos{editUrl="/StandardResult/FacadeEdit/",viewUrl="/StandardResult/FacadeView/",code="",libId=4,isUnit=true,isOne=false}},//立面库 fileObjId=19
                 {5,new StdInfos{editUrl="/StandardResult/MonomerEdit/",viewUrl="/StandardResult/MonomerView/",code="",libId=5,isUnit=true,isOne=false}},//单体库 FileObjId=50
                 {6,new StdInfos{editUrl="/StandardResult/PEMEdit/",viewUrl="/StandardResult/PEMView/",code="",libId=6,isUnit=false,isOne=false}},//工艺工法库 FileObjId=22
                 {8,new StdInfos{editUrl="/StandardResult/ResidentialFloorPlanEdit/",viewUrl="/StandardResult/ResidentialFloorPlanView/",code="",libId=8,isUnit=true,isOne=false}},//住宅楼层平面  
                 {9,new StdInfos{editUrl="/StandardResult/ApartmentPlanEdit/",viewUrl="/StandardResult/ApartmentPlanView/",code="",libId=9,isUnit=true,isOne=false}},//住宅房型  
                 {10,new StdInfos{editUrl="/StandardResult/GardenHouseFloorPlanEdit/",viewUrl="/StandardResult/GardenHouseFloorPlanView/",code="",libId=10,isUnit=true,isOne=false}},//花园洋房楼层平面  
                 {11,new StdInfos{editUrl="/StandardResult/GardenHouseRoomEdit/",viewUrl="/StandardResult/GardenHouseRoomView/",code="",libId=11,isUnit=true,isOne=false}},//花园洋房房型  
                 {12,new StdInfos{editUrl="/StandardResult/VillaAtomEdit",viewUrl="/StandardResult/VillaAtomView/",code="",libId=12,isUnit=true,isOne=false}},//别墅单元户型  
                 {13,new StdInfos{editUrl="/StandardResult/VillaUnitEdit/",viewUrl="/StandardResult/VillaUnitView/",code="",libId=13,isUnit=true,isOne=false}},//别墅户型  
                 {14,new StdInfos{editUrl="/StandardResult/CoreTubeEdit/",viewUrl="/StandardResult/CoreTubeView/",code="",libId=14,isUnit=true,isOne=false}},//核心筒  
                 {15,new StdInfos{editUrl="/StandardResult/NoMonomerEdit/",viewUrl="/StandardResult/NoMonomerView/",code="",libId=15,isUnit=false,isOne=true}}, //非复制类单体
                 {16,new StdInfos{editUrl="/StandardResult/MonomerEdit/",viewUrl="/StandardResult/MonomerView/",code="",libId=16,isUnit=false,isOne=true}}, //复制类单体
                 {17,new StdInfos{editUrl="/StandardResult/DemonstrationAreaEdit/",viewUrl="/StandardResult/DemonstrationAreaView/",code="",libId=17,isUnit=false,isOne=true}},//示范区
                 {18,new StdInfos{editUrl="/StandardResult/LandscapeEdit/",viewUrl="/StandardResult/LandscapeView/",code="",libId=18,isUnit=true,isOne=false}},//硬景景观
                 {19,new StdInfos{editUrl="/StandardResult/SoftLandscapeEdit/",viewUrl="/StandardResult/SoftLandscapeView/",code="",libId=19,isUnit=true,isOne=false}},//软景景观
                 {20,new StdInfos{editUrl="/StandardResult/GuidePostsEdit/",viewUrl="/StandardResult/GuidePostsView/",code="",libId=20,isUnit=true,isOne=false}},//控制指标
                 {21,new StdInfos{editUrl="/StandardResult/MainSceneEdit/",viewUrl="/StandardResult/MainSceneView/",code="",libId=21,isUnit=true,isOne=false}},//主要场景  
                 {22,new StdInfos{editUrl="/StandardResult/HardLandscapeEdit/",viewUrl="/StandardResult/HardLandscapeView/",code="",libId=22,isUnit=true,isOne=false}},//硬景填写  
                 {23,new StdInfos{editUrl="/StandardResult/AfforestEdit/",viewUrl="/StandardResult/AfforestView/",code="",libId=23,isUnit=true,isOne=false}},//绿化  
                 {24,new StdInfos{editUrl="/StandardResult/SalesOfficesEdit/",viewUrl="/StandardResult/SalesOfficesView/",code="",libId=24,isUnit=true,isOne=false}},//售楼处  
                 {25,new StdInfos{editUrl="/StandardResult/PublicPartsEdit/",viewUrl="/StandardResult/PublicPartsView/",code="",libId=25,isUnit=true,isOne=false}},//公共部位
                 {26,new StdInfos{editUrl="/StandardResult/ModelHouseEdit/",viewUrl="/StandardResult/ModelHouseView/",code="",libId=26,isUnit=true,isOne=false}},//展示样板房
                 {27,new StdInfos{editUrl="/StandardResult/LampEdit/",viewUrl="/StandardResult/LampView/",code="",libId=27,isUnit=true,isOne=false}},//灯具
                 {28,new StdInfos{editUrl="/StandardResult/PublicPartsLobbyEdit/",viewUrl="/StandardResult/PublicPartsLobbyView/",code="",libId=28,isUnit=true,isOne=false}},//公共部位-大堂
                 {29,new StdInfos{editUrl="/StandardResult/PublicPartsLiftEdit/",viewUrl="/StandardResult/PublicPartsLiftView/",code="",libId=29,isUnit=true,isOne=false}},//公共部位电梯
                 {30,new StdInfos{editUrl="/StandardResult/PublicPartsBasementEdit/",viewUrl="/StandardResult/PublicPartsBasementView/",code="",libId=30,isUnit=true,isOne=false}},//公共部位-地下室
                 {31,new StdInfos{editUrl="/StandardResult/PublicPartsAisleEdit/",viewUrl="/StandardResult/PublicPartsAisleView/",code="",libId=31,isUnit=true,isOne=false}},//公共部位-走道
                 {32,new StdInfos{editUrl="/StandardResult/FineDecorationEdit/",viewUrl="/StandardResult/FineDecorationView/",code="",libId=32,isUnit=true,isOne=false}},//精装修
    };
    }
     public class StdInfos {
            public int libId { get; set; }
            public string editUrl { get; set; }
            public string viewUrl { get; set; }
            public bool isUnit { get; set; }
            public bool isOne { get; set; }
            public string code { get; set; }
        }
}
