using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Yinhe.ProcessingCenter.DataRule;
using MongoDB.Bson;
using Yinhe.ProcessingCenter;
using MongoDB.Driver.Builders;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 材料库处理类
    /// </summary>
    public class MaterialBll
    {
       #region 构造函数
        /// <summary>
        /// 类私有变量
        /// </summary>
        private readonly DataOperation _ctx = null;

        /// <summary>
        /// 封闭当前默认构造函数
        /// </summary>
        public MaterialBll()
        {
            _ctx = new DataOperation();
        }

        public MaterialBll(DataOperation ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static MaterialBll _()
        {
            return new MaterialBll();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <returns></returns>
        public static MaterialBll _(DataOperation ctx)
        {
            return new MaterialBll(ctx);
        }

        public static readonly List<object> procurementMethod = new List<object> { 
            new{id="集团采购",name="集团采购"},
            new{id="区域采购",name="区域采购"},
        };
        #endregion

        /// <summary>
        /// 更新材料库的编码
        /// </summary>
        public void RefreshCode(string cityId)
        {
            var tbName = "Material_Material";
            var mats = _ctx.FindAll(tbName);
            foreach (var material in mats)
            {
                var codeBson = new BsonDocument { { "codeOrder", "0" }, { "matNum","" } };
                _ctx.Update(tbName, Query.EQ("matId", material.String("matId")), codeBson);
            }
            foreach (var material in mats)
            {
                string oldCode = material.String("matNum");
                var codeBson = new BsonDocument();
                if (material.Int("cityId") == 0)
                {
                    material.TryAdd("cityId", cityId);
                    codeBson.TryAdd("cityId", cityId);
                }

                material.TryAdd("matNum",string.Empty);
                
                var code = CalcMaterialCode(material, material);
                codeBson.TryAdd("matNum", code);
                codeBson.TryAdd("codeOrder", material.String("codeOrder"));
                _ctx.Update(tbName, Query.EQ("matId", material.String("matId")), codeBson);
            }
        }

        /// <summary>
        /// 计算材料的编码
        /// </summary>
        /// <param name="material">表单提交的材料信息</param>
        /// <param name="old">数据库保存材料信息</param>
        /// <returns></returns>
        public string CalcMaterialCode(BsonDocument material, BsonDocument old)
        {
            if (material.String("procurementMethod") == "集团采购")
            {
                material.TryAdd("cityId", "0");
            }
            if (ShouldCalc(material, old))
            {
                var cityId = material.String("cityId");
                var city = _ctx.FindOneByQuery("MatCity", Query.EQ("cityId", cityId));

                var baseCatId = material.String("baseCatId");
                var baseCat = _ctx.FindOneByQuery("Material_BaseCat", Query.EQ("baseCatId", baseCatId));

                var secondCat = _ctx.FindOneByQuery("Material_Category", Query.EQ("categoryId", baseCat.String("categoryId")));
                var firstCat = _ctx.FindOneByQuery("Material_Category", Query.EQ("categoryId", secondCat.String("nodePid")));

                if (baseCat == null || firstCat == null || secondCat == null)
                    return string.Empty;

                var firstCode = CalcPinyinStep(firstCat.String("name"), 2, 2);
                var secondCode =  CalcPinyin(secondCat.String("name"), 2);
                var baseCode = CalcPinyin(baseCat.String("name"), 4);
                var cityCode = city.String("code");
                if (material.String("procurementMethod") == "集团采购")
                {
                    cityCode = "JT";
                    material.TryAdd("cityId", "0");
                }

                string order = string.Empty;
                string code = string.Format("{0}-{1}-{2}-{3}{4}", firstCode, secondCode, baseCode, cityCode,order);
                var matId = material.String("matId");
                var allMats = _ctx.FindAll("Material_Material");
                var mats = allMats.Where(s => s.String("matNum").StartsWith(code) && s.Int("matId") != material.Int("matId"));
                if (mats.Any())
                {
                    var count = mats.Count();
                    var maxOrder = mats.Select(s => s.Int("codeOrder")).Max();
                    if (count <= maxOrder)
                    {
                        count = maxOrder + 1;
                    }
                    else
                    {
                        count += 1;
                    }
                   
                    order = count.ToString();
                }
                else
                {
                    order = "1";
                }
                material.TryAdd("codeOrder", order);
                code = string.Format("{0}-{1}-{2}-{3}{4}", firstCode, secondCode, baseCode, cityCode, order.PadLeft(3,'0'));
               return code;
            }
            else
            {
                return old.String("matNum");
            }
        }
        /// <summary>
        /// 计算材料的编码
        /// </summary>
        /// <param name="material">表单提交的材料信息</param>
        /// <param name="old">数据库保存材料信息</param>
        /// <returns></returns>
        public string CalcMaterialCode(BsonDocument material, BsonDocument old,string cityTbName)
        {
            if (material.String("procurementMethod") == "集团采购")
            {
                material.TryAdd("cityId", "0");
            }
            if (ShouldCalc(material, old))
            {
                var cityId = material.String("cityId");
                var city = _ctx.FindOneByQuery(cityTbName, Query.EQ("cityId", cityId));

                var baseCatId = material.String("baseCatId");
                var baseCat = _ctx.FindOneByQuery("Material_BaseCat", Query.EQ("baseCatId", baseCatId));

                var secondCat = _ctx.FindOneByQuery("Material_Category", Query.EQ("categoryId", baseCat.String("categoryId")));
                var firstCat = _ctx.FindOneByQuery("Material_Category", Query.EQ("categoryId", secondCat.String("nodePid")));

                if (baseCat == null || firstCat == null || secondCat == null)
                    return string.Empty;

                var firstCode = CalcPinyinStep(firstCat.String("name"), 2, 2);
                var secondCode = CalcPinyin(secondCat.String("name"), 2);
                var baseCode = CalcPinyin(baseCat.String("name"), 4);
                var cityCode = city.String("code");
                if (material.String("procurementMethod") == "集团采购")
                {
                    cityCode = "JT";
                    material.TryAdd("cityId", "0");
                }

                string order = string.Empty;
                string code = string.Format("{0}-{1}-{2}-{3}{4}", firstCode, secondCode, baseCode, cityCode, order);
                var matId = material.String("matId");
                var allMats = _ctx.FindAll("Material_Material");
                var mats = allMats.Where(s => s.String("matNum").StartsWith(code) && s.Int("matId") != material.Int("matId"));
                if (mats.Any())
                {
                    var count = mats.Count();
                    var maxOrder = mats.Select(s => s.Int("codeOrder")).Max();
                    if (count <= maxOrder)
                    {
                        count = maxOrder + 1;
                    }
                    else
                    {
                        count += 1;
                    }

                    order = count.ToString();
                }
                else
                {
                    order = "1";
                }
                material.TryAdd("codeOrder", order);
                code = string.Format("{0}-{1}-{2}-{3}{4}", firstCode, secondCode, baseCode, cityCode, order.PadLeft(3, '0'));
                return code;
            }
            else
            {
                return old.String("matNum");
            }
        }
        /// <summary>
        /// 计算材料的编码
        /// </summary>
        /// <param name="material">表单提交的材料信息</param>
        /// <param name="old">数据库保存材料信息</param>
        /// <returns></returns>
        public string CalcMaterialCodeQX(BsonDocument material, BsonDocument old)
        {
            if (ShouldCalcQX(material, old))
            {
                var baseCatId = material.String("baseCatId");
                var baseCat = _ctx.FindOneByQuery("XH_Material_BaseCat", Query.EQ("baseCatId", baseCatId));

                var secondCat = _ctx.FindOneByQuery("XH_Material_Category", Query.EQ("categoryId", baseCat.String("categoryId")));
                var firstCat = _ctx.FindOneByQuery("XH_Material_Category", Query.EQ("categoryId", secondCat.String("nodePid")));

                if (baseCat == null || firstCat == null || secondCat == null)
                    return string.Empty;

                var firstCode = CalcPinyinStep(firstCat.String("name"), 2, 2);
                var secondCode = CalcPinyin(secondCat.String("name"), 2);
                var baseCode = CalcPinyin(baseCat.String("name"), 4);


                string order = string.Empty;
                string code = string.Format("{0}-{1}-{2}", firstCode, secondCode, baseCode);
                var matId = material.String("matId");
                var allMats = _ctx.FindAll("XH_Material_Material");
                var mats = allMats.Where(s => s.String("materialNumber").StartsWith(code) && s.Int("matId") != material.Int("matId"));
                if (mats.Any())
                {
                    var count = mats.Count();
                    var maxOrder = mats.Select(s => s.Int("codeOrder")).Max();
                    if (count <= maxOrder)
                    {
                        count = maxOrder + 1;
                    }
                    else
                    {
                        count += 1;
                    }

                    order = count.ToString();
                }
                else
                {
                    order = "1";
                }
                material.TryAdd("codeOrder", order);
                code = string.Format("{0}-{1}-{2}-{3}", firstCode, secondCode, baseCode,  order.PadLeft(3, '0'));
                return code;
            }
            else
            {
                return old.String("materialNumber");
            }
        }



        /// <summary>
        /// 修改材料类目时，修改其下所有编码
        /// </summary>
        /// <param name="catName"></param>
        /// <returns></returns>
        public void ChangeCategory(BsonDocument category)
        {
            int nodeLevel = category.Int("nodeLevel");
            
            if (nodeLevel == 1){
                //一级类目
                var cates = _ctx.FindAllByQuery("Material_Category",Query.EQ("nodePid",category.String("categoryId")));//获取二级类目
                var catIds = cates.Select(s=>s.String("categoryId"));
                var baseCats = _ctx.FindAllByQuery("Material_BaseCat",Query.In("categoryId",TypeConvert.StringListToBsonValueList(catIds)));
                var baseCatIds = baseCats.Select(s=>s.String("baseCatId"));
                var mats = _ctx.FindAllByQuery("Material_Material",Query.In("baseCatId",TypeConvert.StringListToBsonValueList(baseCatIds)));//一级类目下的所有材料

                var firstCode = CalcPinyinStep(category.String("name"), 2, 2);//一级编码
                foreach(var mat in mats)
                {
                    var matNum = mat.String("matNum");
                    var codes = matNum.SplitParam("-");
                    if(codes.Count()== 4)
                    {
                        codes[0] = firstCode;
                        matNum = string.Join("-",codes);
                        _ctx.Update("Material_Material", Query.EQ("matId", mat.String("matId")), new BsonDocument { { "matNum", matNum } });
                    }
                }

            }
            else if(nodeLevel == 2) { 
                //二级类目
                var baseCats = _ctx.FindAllByQuery("Material_BaseCat",Query.EQ("categoryId",category.String("categoryId")));//所有基类
                var baseCatIds = baseCats.Select(s=>s.String("baseCatId"));
                var mats = _ctx.FindAllByQuery("Material_Material",Query.In("baseCatId",TypeConvert.StringListToBsonValueList(baseCatIds)));//一级类目下的所有材料

                var secondCode = CalcPinyin(category.String("name"), 2);//二级编码

                foreach (var mat in mats)
                {
                    var matNum = mat.String("matNum");
                    var codes = matNum.SplitParam("-");
                    if (codes.Count() == 4)
                    {
                        codes[1] = secondCode;
                        matNum = string.Join("-", codes);
                        _ctx.Update("Material_Material", Query.EQ("matId", mat.String("matId")), new BsonDocument { { "matNum", matNum } });
                    }
                }
            }
        }

        /// <summary>
        /// 修改基类名称时，改变其下材料编码
        /// </summary>
        /// <param name="baseCat"></param>
        public void ChangeBaseCat(BsonDocument baseCat)
        {
           //基类下所有材料
            var mats = _ctx.FindAllByQuery("Material_Material", Query.EQ("baseCatId", baseCat.String("baseCatId")));

            var baseCode = CalcPinyin(baseCat.String("name"), 4);//基类编码
            foreach (var mat in mats)
            {
                var matNum = mat.String("matNum");
                var codes = matNum.SplitParam("-");
                if (codes.Count() == 4)
                {
                    codes[2] = baseCode;
                    matNum = string.Join("-", codes);
                    _ctx.Update("Material_Material", Query.EQ("matId", mat.String("matId")), new BsonDocument { { "matNum", matNum } });
                }
            }
        }

        

        /// <summary>
        /// 判断是否要计算编码
        /// </summary>
        /// <param name="newM"></param>
        /// <param name="old"></param>
        /// <returns></returns>
        private bool ShouldCalc(BsonDocument newM, BsonDocument old)
        {
            if (newM.String("baseCatId") == old.String("baseCatId") && //基类相同
                newM.String("cityId") == old.String("cityId") &&//城市相同
                newM.String("procurementMethod") == old.String("procurementMethod") &&//采购方式相同
                newM.String("matNum") == old.String("matNum") && !string.IsNullOrEmpty(newM.String("matNum"))
                )
                return false;
            else
                return true;
        }

        /// <summary>
        /// 判断是否要计算编码
        /// </summary>
        /// <param name="newM"></param>
        /// <param name="old"></param>
        /// <returns></returns>
        private bool ShouldCalcQX(BsonDocument newM, BsonDocument old)
        {
            if (newM.String("baseCatId") == old.String("baseCatId") && //基类相同
                newM.String("materialNumber") == old.String("materialNumber") && !string.IsNullOrEmpty(newM.String("materialNumber"))
                )
                return false;
            else
                return true;
        }



        /// <summary>
        /// 计算拼音
        /// </summary>
        /// <param name="name"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        private string CalcPinyin(string name,int counter)
        {
            int index = 0;
            string code = string.Empty;
            foreach (var c in name)
            {
                if (index++ == counter)
                {
                    break;
                }
                code += CNToPinyin.GetChineseSpell(c.ToString());
            }
            return code;
        }

        /// <summary>
        /// 按一定步长获取输入中的拼音缩写
        /// </summary>
        /// <param name="name"></param>
        /// <param name="step">步长</param>
        /// <returns></returns>
        private string CalcPinyinStep(string name, int step,int counter)
        {
            string code = string.Empty;
            if (step < name.Length)
            {
                int index = 0;
                for (int i = 0; i < name.Length; i+=step)
                {
                    if (index++ < counter)
                    {
                        code += CNToPinyin.GetChineseSpell(name[i].ToString());
                    }
                }
                   
            }
            else//所有名称小于步长，则显示所有名称的首个拼音
            {
                foreach (var c in name)
                {
                    code += CNToPinyin.GetChineseSpell(c.ToString());
                }
            }
            return code;
        }

        /// <summary>
        /// 获取拼音的第一个字母
        /// </summary>
        /// <param name="code">拼音字符串</param>
        /// <returns></returns>
        //private string GetFirstChart(string code)
        //{
        //    if (!string.IsNullOrEmpty(code))
        //    {
        //        return code.ToUpper()[0].ToString();
        //    }
        //    return string.Empty;
        //}
    }
}
