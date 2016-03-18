using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Web;
using System.Reflection;
using System.IO;

namespace Yinhe.ProcessingCenter.DataRule
{
    /// <summary>
    /// 表规则
    /// </summary>
    public class TableRule
    {
        #region 属性
        /// <summary>
        /// 名称,表与表间的唯一标示
        /// </summary>
        public string Name = "";

        /// <summary>
        /// 表中的字段列表
        /// </summary>
        public List<ColumnRule> ColumnRules = new List<ColumnRule>();

        /// <summary>
        /// 表中的级联操作
        /// </summary>
        public List<CascadeRule> CascadeRules = new List<CascadeRule>();

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark = "";

        /// <summary>
        /// 是否记录到主表日志中
        /// </summary>
        public bool HasLog = false;

        private string _primaryKey;

        /// <summary>
        /// 获取主键的名称
        /// </summary>
        public string PrimaryKey
        {
            get
            {
                if (string.IsNullOrEmpty(_primaryKey))
                {
                    _primaryKey = ColumnRules.Single(s => s.IsPrimary).Name;
                }
                return _primaryKey;
            }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 默认构造函数,传入表名,读取数据规则
        /// </summary>
        /// <param name="tbName"></param>
        public TableRule(string tbName)
        {
            XElement tableElement = null;       //表规则
            List<XElement> ruleList = new List<XElement>();     //已缓存的表规则列表

            object ruleCache = CacheHelper.GetCache("DataRules");        //获取数据规则缓存

            #region 读取缓存
            if (ruleCache != null)     //有缓存文件,则读取缓存
            {
                ruleList = ruleCache as List<XElement>;         //赋值表规则列表

                tableElement = ruleList.FirstOrDefault(t => t.Attribute("Name").Value.ToString() == tbName.Trim());
            }
            #endregion

            #region 读取本项目
            if (tableElement == null) //如果不在缓存文件中,获取当前项目节点的规则xml文件
            {
                string xmlPath = AppDomain.CurrentDomain.BaseDirectory + "/DataRules.xml";  //获取本项目节点的数据规则

                if (System.IO.File.Exists(xmlPath)) //如果存在,则读取规则文件
                {
                    XElement ruleRoot = XElement.Load(xmlPath);

                    tableElement = (from i in ruleRoot.Elements("Table")
                                    where i.Attribute("Name").Value.ToString() == tbName.Trim()
                                    select i).FirstOrDefault();

                    if (tableElement != null)       //如果存在,则添加到缓存
                    {
                        ruleList.Add(tableElement);
                        CacheHelper.SetCache("DataRules", ruleList, null, DateTime.Now.AddMinutes(30)); //重写缓存
                    }
                }
            }
            #endregion

            #region 读取插件dll
            if (tableElement == null)
            {
                List<Assembly> distinctAssemblies = AssemblyHelper.GetPluginAssemblies();

                foreach (Assembly plugin in distinctAssemblies)   //遍历插件
                {
                    AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute)(plugin.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]);

                    string xmlUrl = titleAttr.Title + ".DataRules.xml";
                    Stream xmlStream = plugin.GetManifestResourceStream(xmlUrl);

                    if (xmlStream != null)
                    {
                        StreamReader xmlReader = new StreamReader(xmlStream);

                        string xmlContent = xmlReader.ReadToEnd();

                        XElement ruleRoot = XElement.Parse(xmlContent);

                        tableElement = (from i in ruleRoot.Elements("Table")
                                        where i.Attribute("Name").Value.ToString() == tbName.Trim()
                                        select i).FirstOrDefault();
                    }

                    if (tableElement != null)       //如果存在,则添加到缓存,并跳出缓存
                    {
                        ruleList.Add(tableElement);
                        CacheHelper.SetCache("DataRules", ruleList, null, DateTime.Now.AddMinutes(30)); //重写缓存
                        break;
                    }
                }
            }
            #endregion

            if (tableElement != null)
            {
                this.SetTableRule(tableElement);
            }
            else
            {
                this.Name = tbName;
            }
        }

        /// <summary>
        /// 初始化内容构造
        /// </summary>
        /// <param name="entityElement"></param>
        public TableRule(XElement entityElement)
        {
            this.SetTableRule(entityElement);
        }

        #endregion

        #region 方法
        /// <summary>
        /// 根据XML获取对应表实体
        /// </summary>
        /// <param name="entityElement"></param>
        /// <returns></returns>
        public void SetTableRule(XElement entityElement)
        {
            #region 属性
            this.Name = entityElement.Attribute("Name") != null ? entityElement.Attribute("Name").Value : "";
            this.Remark = entityElement.Attribute("Remark") != null ? entityElement.Attribute("Remark").Value : "";
            this.HasLog = (entityElement.Attribute("HasLog") != null && entityElement.Attribute("HasLog").Value == "true") ? true : false;
            #endregion

            #region 字段
            List<ColumnRule> columns = new List<ColumnRule>();

            foreach (var tempElement in entityElement.Elements("Column"))
            {
                ColumnRule tempColumn = new ColumnRule(tempElement);

                columns.Add(tempColumn);
            }

            this.ColumnRules = columns;

            ColumnRule createUser = new ColumnRule(XElement.Parse("<Column Name=\"createUserId\" Remark=\"创建用户\" />"));
            ColumnRule updateUser = new ColumnRule(XElement.Parse("<Column Name=\"updateUserId\" Remark=\"更新用户\" />"));

            this.ColumnRules.Add(createUser);
            this.ColumnRules.Add(updateUser);
            #endregion

            #region 级联
            List<CascadeRule> cascades = new List<CascadeRule>();

            foreach (var tempElement in entityElement.Elements("Cascade"))
            {
                CascadeRule tempCascade = new CascadeRule(tempElement);

                cascades.Add(tempCascade);
            }

            this.CascadeRules = cascades;
            #endregion
        }

        /// <summary>
        /// 查找当前站点中的所有表规则
        /// </summary>
        /// <returns></returns>
        public static List<TableRule> GetAllTables()
        {
            List<TableRule> allRuleList = new List<TableRule>();

            #region 读取本项目
            string xmlPath = AppDomain.CurrentDomain.BaseDirectory + "/DataRules.xml";  //获取本项目节点的数据规则

            if (System.IO.File.Exists(xmlPath)) //如果存在,则读取规则文件
            {
                XElement ruleRoot = XElement.Load(xmlPath);

                foreach (var tableElement in ruleRoot.Elements("Table"))
                {
                    TableRule tempRule = new TableRule(tableElement);

                    allRuleList.Add(tempRule);
                }

            }

            #endregion

            #region 读取插件dll
            List<Assembly> distinctAssemblies = AssemblyHelper.GetPluginAssemblies();

            foreach (Assembly plugin in distinctAssemblies)   //遍历插件
            {
                AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute)(plugin.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]);

                string xmlUrl = titleAttr.Title + ".DataRules.xml";
                Stream xmlStream = plugin.GetManifestResourceStream(xmlUrl);

                if (xmlStream != null)
                {
                    StreamReader xmlReader = new StreamReader(xmlStream);

                    string xmlContent = xmlReader.ReadToEnd();

                    XElement ruleRoot = XElement.Parse(xmlContent);

                    foreach (var tableElement in ruleRoot.Elements("Table"))
                    {
                        TableRule tempRule = new TableRule(tableElement);

                        allRuleList.Add(tempRule);
                    }
                }
            }
            #endregion

            return allRuleList;
        }

        /// <summary>
        /// 获取当前表的所有子表,即有外键到到当前表的表
        /// </summary>
        /// <param name="tbName"></param>
        /// <returns></returns>
        public static List<TableRule> GetAllForeignTables(string tbName)
        {
            List<TableRule> foreignTables = new List<TableRule>();   //与当前要删除表有外键关联的表

            foreach (var tempTable in TableRule.GetAllTables())      //循环所有表结构
            {
                //判断表内字段,有存在SourceTable为当前要删除记录的表的,则记录
                if (tempTable.ColumnRules.Where(t => t.SourceTable == tbName).Count() > 0)
                {
                    foreignTables.Add(tempTable);
                }
            }

            return foreignTables;
        }

        /// <summary>
        /// 获取主键名称
        /// </summary>
        /// <returns></returns>
        public string GetPrimaryKey()
        {
            ColumnRule tempColumn = this.ColumnRules.Where(t => t.IsPrimary == true).FirstOrDefault();


            return tempColumn != null ? tempColumn.Name : "";
        }

        #endregion
    }
}
