using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Yinhe.ProcessingCenter.SynAD
{
    /// <summary>
    /// 中海弘扬AD路径解析
    /// </summary>
    public class PathAnalyseZHHY
    {
        private CommonLog _log = new CommonLog();

        private string connString = "Data Source=192.0.0.169;Initial Catalog=HumanResource;User ID=yinhe;Password=1234!@#$;";

        public List<ADDepartment> GetDepList()
        {
            List<ADDepartment> list = new List<ADDepartment>();
            SQLGetDataList bll = new SQLGetDataList();
            try
            {
                var dataList = bll.GetBsonDocumentDataList(connString, "select * from OADep where [IsActive] = 1");

                foreach (var item in dataList)
                {
                    ADDepartment dep = new ADDepartment();
                    dep.DepId =item.Int("DepID");
                    dep.Name = item.Text("DepName");
                    dep.Guid = item.Text("DepID");
                    dep.Code = item.Text("DepCode");
                    dep.ParentGuid = item.Text("ParentDepID");
                    dep.Level = item.Int("DepLevel");
                    list.Add(dep);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            return list;
        }

        public List<ADUser> GetUserList()
        {
            List<ADUser> list = new List<ADUser>();
            SQLGetDataList bll = new SQLGetDataList();
            try
            {
                var userList = bll.GetBsonDocumentDataList(connString, "select * from OAUserInfo where [IsActive] = 1");
                var relList = bll.GetBsonDocumentDataList(connString, "select * from OADepUsers");
                foreach (var item in userList)
                {
                    ADUser user = new ADUser();
                    user.UserId = item.Int("UserID");
                    user.Name = item.Text("UserNameCn");
                    user.LoginName = item.Text("UserNameEn");
                    user.EmailAddr =  item.Text("Email");
                    user.MobieNumber = item.Text("Mobile");
                    user.PhoneNumber =  item.Text("OfficeTel");
                    user.Guid = item.Text("UserID");
                    user.Remark = item.Text("UserAD");
                    var rel = relList.Where(t => t.Int("UserID") == item.Int("UserID")).ToList();
                    if (rel.Count > 0)
                    {
                        user.DepartMentGuids = new List<string>();
                        foreach (var entity in rel)
                        {
                            user.DepartMentGuids.Add(entity.Text("DepID"));
                        }
                    }
                    list.Add(user);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            return list;
        }
    }
}
